using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Services
{
    public class TuVanService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public TuVanService(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        // ==========================================================================================
        // HÀM GỌI GEMINI – DÙNG NHIỀU MODEL THEO THỨ TỰ ƯU TIÊN
        // Trả về: (nội dung sinh ra, tên model đã dùng) – nếu tất cả đều lỗi => (null, null)
        // ==========================================================================================
        private async Task<(string? text, string? modelUsed)> GoiGeminiAsync(string prompt)
        {
            try
            {
                string apiKey = _config["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                    return (null, null);

                var client = _httpClientFactory.CreateClient();

                // Danh sách model theo thứ tự ưu tiên bạn chọn
                string[] models =
                {
                    "gemini-2.5-flash",
                    "gemini-2.5-pro",
                    "gemini-1.5-flash-latest",
                    "gemini-2.0-flash",
                    "gemini-2.0-flash-lite"
                };

                foreach (var model in models)
                {
                    // Thử tối đa 2 lần cho mỗi model
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        try
                        {
                            string endpoint =
                                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                            var payload = new
                            {
                                contents = new[]
                                {
                                    new
                                    {
                                        parts = new[]
                                        {
                                            new { text = prompt }
                                        }
                                    }
                                }
                            };

                            var res = await client.PostAsync(
                                endpoint,
                                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                            var json = await res.Content.ReadAsStringAsync();

                            if (!res.IsSuccessStatusCode)
                            {
                                // Nếu là lỗi quá tải/quota -> thử lại / thử model khác
                                int code = (int)res.StatusCode;
                                if (code == 503 || code == 429 ||
                                    json.Contains("overloaded", StringComparison.OrdinalIgnoreCase) ||
                                    json.Contains("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                                    json.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
                                {
                                    // thử lại model hiện tại
                                    continue;
                                }

                                // Lỗi khác -> bỏ model này, chuyển sang model tiếp theo
                                break;
                            }

                            using var doc = JsonDocument.Parse(json);
                            string? text = doc.RootElement
                                .GetProperty("candidates")[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString();

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                // Thành công
                                return (text.Trim(), model);
                            }
                            else
                            {
                                // Nội dung rỗng -> thử lại
                                continue;
                            }
                        }
                        catch
                        {
                            // lỗi mạng tạm thời -> thử lại model này
                            continue;
                        }
                    }
                }

                // Tất cả model đều lỗi
                return (null, null);
            }
            catch
            {
                return (null, null);
            }
        }

        // ==========================================================================================
        // BƯỚC 1 – PHÂN TÍCH DỮ LIỆU
        // ==========================================================================================
        public async Task<(List<KienThuc>, List<KienThucSinhVien>)>
            PhanTichHocTapAsync(int maSV, int maMH)
        {
            var kt = await _context.KienThucs
                .Include(k => k.ChuDe)
                .Where(k => k.ChuDe.MaMH == maMH)
                .ToListAsync();

            var sv = await _context.KienThucSinhViens
                .Where(k => k.MaSV == maSV)
                .ToListAsync();

            return (kt, sv);
        }

        // ==========================================================================================
        // BƯỚC 2 – LẤY DANH SÁCH KIẾN THỨC + SINH CÂU HỎI AI (CÓ CACHE VÀO CSDL)
        // ==========================================================================================
        public async Task<List<KienThucTuDanhGiaVm>>
            LayDanhSachKienThucChoTuDanhGiaAsync(int maSV, int maMH)
        {
            var mon = await _context.MonHocs.FindAsync(maMH);

            var kienThucs = await _context.KienThucs
                .Include(k => k.ChuDe)
                .Where(k => k.ChuDe.MaMH == maMH)
                .OrderBy(k => k.ChuDe.TenCD)
                .ThenBy(k => k.MaKT)
                .ToListAsync();

            var svData = await _context.KienThucSinhViens
                .Where(k => k.MaSV == maSV)
                .ToListAsync();

            // Sinh câu hỏi AI nếu chưa có, và lưu lại vào CSDL
            foreach (var kt in kienThucs)
            {
                if (string.IsNullOrWhiteSpace(kt.CauHoiAI))
                {
                    string? q = await TaoCauHoiAI(kt, mon?.TenMH ?? "");
                    if (!string.IsNullOrWhiteSpace(q))
                    {
                        kt.CauHoiAI = q;
                        _context.KienThucs.Update(kt);
                    }
                }
            }

            await _context.SaveChangesAsync();

            var vm = new List<KienThucTuDanhGiaVm>();
            foreach (var kt in kienThucs)
            {
                int tt = svData.FirstOrDefault(s => s.MaKT == kt.MaKT)?.TrangThai ?? 0;
                vm.Add(new KienThucTuDanhGiaVm
                {
                    KienThuc = kt,
                    TrangThai = tt
                });
            }

            return vm;
        }

        private async Task<string?> TaoCauHoiAI(KienThuc kt, string monHoc)
        {
            string prompt = $@"
Tạo 1 câu hỏi tự đánh giá (ngắn gọn – 1 dòng, tiếng Việt) cho sinh viên:
- Môn: {monHoc}
- Chủ đề: {kt.ChuDe?.TenCD}
- Kiến thức: {kt.NoiDung}

Chỉ trả về đúng 1 câu hỏi.";

            var (text, model) = await GoiGeminiAsync(prompt);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        // ==========================================================================================
        // BƯỚC 3 – SẮP XẾP THỨ TỰ KIẾN THỨC (THUẬT TOÁN HEURISTIC THÔNG MINH)
        //  -> KHÔNG GỌI AI, CHỈ DÙNG RULE + DỮ LIỆU SV
        // ==========================================================================================
        public (List<(KienThuc kt, float score)> ds, string modelUsed)
            DuDoanThongMinh(List<KienThuc> kts, List<KienThucSinhVien> svData, int daysLeft)
        {
            if (daysLeft <= 0) daysLeft = 1;

            var result = new List<(KienThuc kt, float score)>();

            foreach (var kt in kts)
            {
                int trangThai = svData.FirstOrDefault(s => s.MaKT == kt.MaKT)?.TrangThai ?? 0;

                // Mức độ hiểu hiện tại
                float understanding = trangThai switch
                {
                    2 => 1.0f, // Đã hiểu
                    1 => 0.5f, // Cần ôn
                    _ => 0.0f  // Chưa học/chưa đánh dấu
                };

                // Độ quan trọng dựa trên độ khó + số kiến thức tiền đề
                float doKhoNorm = Math.Clamp((float)kt.DoKho / 10f, 0f, 1f);
                float tienDeNorm = Math.Clamp((float)kt.SoKienThucTruoc / 10f, 0f, 1f);

                float important =
                    0.5f +
                    0.3f * doKhoNorm +
                    0.2f * tienDeNorm;

                // Áp lực thời gian: càng ít ngày còn lại thì càng gấp
                float timePressure = 1f + Math.Max(0, 30 - daysLeft) / 40f;

                // Score cuối cùng
                float score = important * (1 - understanding) * timePressure;

                result.Add((kt, score));
            }

            // Sắp xếp giảm dần theo score
            var ordered = result.OrderByDescending(x => x.score).ToList();

            // ⚠️ modelUsed để trống, để Controller ưu tiên dùng model AI thật (từ lời khuyên)
            return (ordered, "");
        }

        // ==========================================================================================
        // BƯỚC 4 – SINH LỜI KHUYÊN AI (DÙNG DANH SÁCH MODEL Ở TRÊN)
        // ==========================================================================================
        public async Task<(string loiKhuyen, string modelUsed)>
            TaoLoiKhuyenAIAsync(string goal, string monHoc,
            List<(KienThuc kt, float score)> list, int daysLeft)
        {
            string prompt = $@"
Bạn là cố vấn học tập.
Môn học: {monHoc}
Mục tiêu: {goal}
Số ngày còn lại: {daysLeft}.

Các kiến thức ưu tiên: {string.Join(", ", list.Take(5).Select(x => x.kt.NoiDung))}

Hãy viết đoạn tư vấn khoảng 100 từ, tiếng Việt, rõ ràng và khích lệ sinh viên.";

            var (text, model) = await GoiGeminiAsync(prompt);

            if (string.IsNullOrWhiteSpace(text))
            {
                return ("Hiện AI đang bận, bạn hãy ưu tiên học theo thứ tự danh sách trên.", "fallback");
            }

            return (text.Trim(), model ?? "unknown");
        }

        // ==========================================================================================
        // BƯỚC 5 – GIẢI THÍCH VÌ SAO KIẾN THỨC QUAN TRỌNG (CHO TOP N KIẾN THỨC)
        // ==========================================================================================
        public async Task<Dictionary<int, string>>
            TaoMoTaAITheoKienThucAsync(string goal, string monHoc,
            List<(KienThuc kt, float score)> ds)
        {
            var dict = new Dictionary<int, string>();

            foreach (var item in ds.Take(3)) // chỉ lấy top 3 để tiết kiệm API
            {
                var kt = item.kt;
                string prompt = $@"
Giải thích ngắn (2–3 câu, tiếng Việt, dễ hiểu) vì sao kiến thức sau quan trọng
đối với mục tiêu '{goal}' trong môn '{monHoc}':

- {kt.NoiDung}

Chỉ trả về đoạn văn, không gạch đầu dòng.";

                var (text, model) = await GoiGeminiAsync(prompt);

                if (!string.IsNullOrWhiteSpace(text))
                    dict[kt.MaKT] = text.Trim();
            }

            return dict;
        }

        // ==========================================================================================
        // BƯỚC 6 – LƯU ĐỀ XUẤT + CHATLOG VÀO DB
        // ==========================================================================================
        public async Task<DeXuat> LuuKetQuaAsync(int maSV, int maMH,
            string goal, string reason,
            List<(KienThuc kt, float score)> ds)
        {
            var dx = new DeXuat
            {
                MaSV = maSV,
                MaMH = maMH,
                Goal = goal,
                Nguon = "AI",
                NoiDung = "Tư vấn kiến thức",
                ThoiGian = DateTime.Now
            };

            _context.DeXuats.Add(dx);
            await _context.SaveChangesAsync();

            int rank = 1;
            foreach (var item in ds.Take(5))
            {
                _context.DeXuatChiTiets.Add(new DeXuatChiTiet
                {
                    MaDX = dx.MaDX,
                    MaKT = item.kt.MaKT,
                    RankIndex = rank++,
                    Score = item.score,
                    Reason = reason
                });
            }

            await _context.SaveChangesAsync();
            return dx;
        }

        public async Task LuuChatLogAsync(int maSV, int? maDX,
            string cauHoi, string traLoi)
        {
            var log = new ChatLog
            {
                MaSV = maSV,
                MaDX = maDX,
                NoiDung = cauHoi,
                TraLoi = traLoi,
                ThoiGian = DateTime.Now
            };

            _context.ChatLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

    // ==========================================================================================
    // VIEWMODEL – KIẾN THỨC + TRẠNG THÁI
    // ==========================================================================================
    public class KienThucTuDanhGiaVm
    {
        public KienThuc KienThuc { get; set; }
        /// <summary>
        /// 0 = chưa học, 1 = cần ôn, 2 = đã hiểu
        /// </summary>
        public int TrangThai { get; set; }
    }
}
