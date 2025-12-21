using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Services
{
    public class TuVanService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;

        public TuVanService(
            ApplicationDbContext context,
            IHttpClientFactory http,
            IConfiguration config)
        {
            _context = context;
            _http = http;
            _config = config;
        }

        // =====================================================
        // 0. GỌI GEMINI – MULTI MODEL, KHÔNG ĐỔI LOGIC
        // =====================================================
        private async Task<string?> CallAIAsync(string prompt)
        {
            string apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            var client = _http.CreateClient();

            string[] models =
                {
        // 1. Nhóm Quota Khủng (30 RPM) - Nếu API hỗ trợ Gemma
        "gemma-2-27b-it",
        "gemma-2-9b-it",

        // 2. Nhóm Tốc độ cao (10-15 RPM)
        "gemini-2.5-flash-lite", // Có trong ảnh của bạn (10 RPM)
        "gemini-1.5-flash-8b",   // Bản siêu nhẹ cũ (thường 15 RPM)
        "gemini-1.5-flash",      // Bản ổn định cũ (thường 15 RPM)


        "gemini-2.5-flash",
        "gemini-1.5-pro"
    };

            foreach (var model in models)
            {
                int maxRetries = 3; // Thử lại tối đa 3 lần cho mỗi model
                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                        var payload = new
                        {
                            contents = new[] { new { parts = new[] { new { text = prompt } } } }
                        };

                        var res = await client.PostAsync(
                            endpoint,
                            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                        // Xử lý Rate Limit (429) hoặc Server Error (503)
                        if ((int)res.StatusCode == 429 || (int)res.StatusCode == 503)
                        {
                            // Backoff: Đợi 2s, 4s, 8s...
                            int waitTime = (int)Math.Pow(2, i + 1) * 1000;
                            Console.WriteLine($"⚠️ Model {model} bị lỗi {res.StatusCode}. Đợi {waitTime}ms...");
                            await Task.Delay(waitTime);
                            continue; // Thử lại vòng lặp for (retry)
                        }

                        if (res.IsSuccessStatusCode)
                        {
                            var json = await res.Content.ReadAsStringAsync();
                            using var doc = JsonDocument.Parse(json);

                            // Safe access JSON path
                            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                            {
                                var text = candidates[0]
                                    .GetProperty("content")
                                    .GetProperty("parts")[0]
                                    .GetProperty("text")
                                    .GetString();

                                return text?.Trim();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Lỗi gọi AI: {ex.Message}");
                        // Nếu lỗi mạng, đợi chút rồi thử lại
                        await Task.Delay(10000);
                    }
                }
                // Nếu hết 3 lần retry mà model này vẫn lỗi, vòng lặp foreach sẽ chuyển sang model tiếp theo
            }

            return null;
        }


        // =====================================================
        // 1. PHÂN TÍCH DỮ LIỆU
        // =====================================================
        public async Task<(List<KienThuc>, List<KienThucSinhVien>)>
            PhanTichHocTapAsync(int maSV, int maMH)
        {
            var kts = await _context.KienThucs
                .Include(x => x.ChuDe)
                .Where(x => x.ChuDe.MaMH == maMH)
                .ToListAsync();

            var sv = await _context.KienThucSinhViens
                .Where(x => x.MaSV == maSV)
                .ToListAsync();

            return (kts, sv);
        }

        // =====================================================
        // 2. SINH CÂU HỎI TỰ ĐÁNH GIÁ (AI – CHỈ KHI CHƯA CÓ)
        // =====================================================
        public async Task<List<KienThucTuDanhGiaVm>> LayDanhSachKienThucChoTuDanhGiaAsync(int maSV, int maMH)
        {
            var mon = await _context.MonHocs.FindAsync(maMH);

            var kts = await _context.KienThucs
                .Include(x => x.ChuDe)
                .Where(x => x.ChuDe.MaMH == maMH)
                .OrderBy(x => x.ChuDe.TenCD)
                .ThenBy(x => x.MaKT)
                .ToListAsync();

            var svData = await _context.KienThucSinhViens
                .Where(x => x.MaSV == maSV)
                .ToListAsync();

            int countCall = 0; // Đếm số lần gọi để delay

            foreach (var kt in kts)
            {
                if (!string.IsNullOrWhiteSpace(kt.CauHoiAI)) continue;

                string prompt = $@"
        Tạo 1 câu hỏi tự đánh giá NGẮN (1 dòng, tiếng Việt).
        Môn: {mon?.TenMH}
        Chủ đề: {kt.ChuDe?.TenCD}
        Kiến thức: {kt.NoiDung}
        Chỉ trả về 1 câu hỏi.";

                // Gọi AI
                var q = await CallAIAsync(prompt);

                if (!string.IsNullOrWhiteSpace(q))
                {
                    kt.CauHoiAI = q;
                    _context.KienThucs.Update(kt);

                    // Lưu database ngay để tránh mất dữ liệu nếu crash giữa chừng
                    await _context.SaveChangesAsync();
                }

                // --- QUAN TRỌNG: DELAY GIỮA CÁC LẦN GỌI ---
                countCall++;
                // Sau mỗi lần gọi, nghỉ 2 giây. Nếu API Free Tier, có thể cần tăng lên 4000 (4s)
                await Task.Delay(2000);
            }

            // Return kết quả
            return kts.Select(kt =>
            {
                int tt = svData.FirstOrDefault(s => s.MaKT == kt.MaKT)?.TrangThai ?? 0;
                return new KienThucTuDanhGiaVm
                {
                    KienThuc = kt,
                    TrangThai = tt
                };
            }).ToList();
        }

        // =====================================================
        // 3. SCORE – HEURISTIC LOCAL (KHÔNG AI)
        // =====================================================
        public List<(KienThuc kt, float score)>
            DuDoanThongMinh(
                List<KienThuc> kts,
                List<KienThucSinhVien> svData,
                int daysLeft,
                string goal)
        {
            if (daysLeft <= 0) daysLeft = 1;

            var result = new List<(KienThuc kt, float score)>();

            foreach (var kt in kts)
            {
                int tt = svData.FirstOrDefault(s => s.MaKT == kt.MaKT)?.TrangThai ?? 0;

                float understanding = tt switch
                {
                    2 => 1.0f,
                    1 => 0.5f,
                    _ => 0.0f
                };

                float doKho = Math.Clamp((float)kt.DoKho / 10f, 0f, 1f);
                float tienDe = Math.Clamp((float)kt.SoKienThucTruoc / 10f, 0f, 1f);

                float important = 0.5f + 0.3f * doKho + 0.2f * tienDe;

                if (kt.IsKienThucCoBan)
                    important += 0.3f;

                important += GoalWeight(goal);

                float timePressure = 1f + Math.Max(0, 30 - daysLeft) / 40f;

                float score = important * (1 - understanding) * timePressure;

                result.Add((kt, score));
            }

            return result.OrderByDescending(x => x.score).ToList();
        }

        // =====================================================
        // 4. TIMELINE – LOCAL
        // =====================================================
        public async Task<List<TimelineVm>> BuildTimelineAsync(
    List<(KienThuc kt, float score)> ds,
    int daysLeft)
        {
            if (daysLeft <= 0) daysLeft = 1;

            var maCDs = ds.Select(x => x.kt.MaCD).Distinct().ToList();

            var taiLieuTheoChuDe = await _context.TaiLieuChuDes
                .Include(x => x.TaiLieu)
                .Where(x => maCDs.Contains(x.MaCD))
                .OrderBy(x => x.OrderIndex)
                .GroupBy(x => x.MaCD)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(x => x.TaiLieu).ToList()
                );

            var timeline = new List<TimelineVm>();

            // 👉 Danh sách kiến thức dùng để ôn tập (top score)
            var reviewPool = ds
                .OrderByDescending(x => x.score)
                .Select(x => x.kt)
                .ToList();

            int learnIndex = 0;
            int reviewIndex = 0;

            for (int day = 1; day <= daysLeft; day++)
            {
                var dayVm = new TimelineVm { Day = day };
                int totalMinutes = 0;

                // =========================
                // 1️⃣ Ưu tiên học kiến thức mới
                // =========================
                while (learnIndex < ds.Count && totalMinutes < 90)
                {
                    var kt = ds[learnIndex].kt;

                    int minutes =
                        kt.DoKho >= 7 ? 40 :
                        kt.DoKho >= 5 ? 30 : 25;

                    if (totalMinutes + minutes > 90) break;

                    dayVm.Items.Add(BuildItem(kt, minutes, taiLieuTheoChuDe));
                    totalMinutes += minutes;
                    learnIndex++;
                }

                // =========================
                // 2️⃣ Nếu còn thời gian → ôn tập
                // =========================
                while (totalMinutes < 90 && reviewPool.Count > 0)
                {
                    var kt = reviewPool[reviewIndex % reviewPool.Count];
                    reviewIndex++;

                    int minutes = 30;
                    if (totalMinutes + minutes > 90) break;

                    dayVm.Items.Add(BuildItem(kt, minutes, taiLieuTheoChuDe, isReview: true));
                    totalMinutes += minutes;
                }

                timeline.Add(dayVm);
            }

            return timeline;
        }
        private TimelineItemVm BuildItem(
            KienThuc kt,
            int minutes,
            Dictionary<int, List<TaiLieu>> taiLieuTheoChuDe,
            bool isReview = false)
        {
            return new TimelineItemVm
            {
                MaKT = kt.MaKT,
                NoiDung = isReview
                    ? $"Ôn lại: {kt.NoiDung}"
                    : kt.NoiDung,
                ChuDe = kt.ChuDe?.TenCD,
                Minutes = minutes,
                TaiLieu = taiLieuTheoChuDe.ContainsKey(kt.MaCD)
                    ? taiLieuTheoChuDe[kt.MaCD].Select(tl => new TaiLieuVm
                    {
                        MaTL = tl.MaTL,
                        TieuDe = tl.TieuDe,
                        LoaiTL = tl.LoaiTL,
                        DuongDan = tl.DuongDan
                    }).ToList()
                    : new()
            };
        }





        // =====================================================
        // 5. AI – GỘP STUDY PLAN + GỢI Ý BỔ SUNG (1 LẦN)
        // =====================================================
        public async Task<(string plan, Dictionary<int, string> goiY)>
        SinhNoiDungTuVanTheoTimelineAsync(
            string monHoc,
            string goal,
            int daysLeft,
            List<TimelineVm> timeline)
        {
            var validMaKT = timeline
                .SelectMany(x => x.Items)
                .Select(x => x.MaKT)
                .ToHashSet();

            string timelineJson = JsonSerializer.Serialize(timeline);

            string prompt = $@"
Bạn là GIẢNG VIÊN đại học đang hướng dẫn sinh viên ôn thi.

DƯỚI ĐÂY là TIMELINE HỌC TẬP CỐ ĐỊNH.
Mỗi kiến thức đã có DANH SÁCH TÀI LIỆU ĐÍNH KÈM.

=== NGUYÊN TẮC BẮT BUỘC ===
1. KHÔNG thêm tài liệu mới
2. KHÔNG thay đổi timeline
3. CHỈ sử dụng tài liệu đã có
4. Viết HƯỚNG DẪN CỤ THỂ, KHÔNG CHUNG CHUNG

=== CÁCH VIẾT GỢI Ý ÔN TẬP (RẤT QUAN TRỌNG) ===
Với MỖI kiến thức, gợi ý phải gồm:
- Bước 1: Đọc/xem tài liệu nào (ghi đúng TÊN tài liệu)
- Bước 2: Tập trung vào nội dung gì trong tài liệu
- Bước 3: Làm gì sau khi học (ví dụ: viết SQL, vẽ ERD, so sánh khái niệm…)

❌ KHÔNG viết các câu chung chung như:
- ""Hãy tìm hiểu thêm""
- ""Nắm vững kiến thức""
- ""Tham khảo tài liệu""

=== STUDY PLAN ===
- Viết 1 đoạn tổng quan 180–250 từ
- Giải thích chiến lược học theo timeline
- Nhấn mạnh cách KẾT HỢP LÝ THUYẾT + THỰC HÀNH từ tài liệu

=== THÔNG TIN ===
MÔN: {monHoc}
MỤC TIÊU: {goal}
SỐ NGÀY: {daysLeft}

TIMELINE:
{timelineJson}

=== ĐỊNH DẠNG TRẢ VỀ (CHỈ JSON) ===
{{
  ""studyPlan"": ""...text..."",
  ""goiY"": {{
    ""MaKT"": ""Bước 1... Bước 2... Bước 3...""
  }}
}}

CHỈ TRẢ VỀ JSON, KHÔNG GIẢI THÍCH.
";


            var raw = await CallAIAsync(prompt);
            if (string.IsNullOrWhiteSpace(raw))
                return ("Hãy học theo timeline đã đề xuất.", new());

            var json = ExtractJsonObject(raw) ?? raw;
            using var doc = JsonDocument.Parse(json);

            string plan =
                doc.RootElement.GetProperty("studyPlan").GetString() ?? "";

            var dict = new Dictionary<int, string>();

            foreach (var p in doc.RootElement.GetProperty("goiY").EnumerateObject())
            {
                if (int.TryParse(p.Name, out int maKT) && validMaKT.Contains(maKT))
                    dict[maKT] = p.Value.GetString() ?? "";
            }

            return (plan, dict);
        }




        // =====================================================
        // GOAL WEIGHT – TIẾNG VIỆT
        // =====================================================
        private float GoalWeight(string goal) =>
            goal switch
            {
                "Ôn để đạt yêu cầu môn học" => 0.12f,
                "Ôn tập để đạt điểm cao" => 0.22f,
                "Nắm vững và hiểu sâu kiến thức" => 0.28f,
                "Hoàn thành bài tập / đồ án" => 0.18f,
                _ => 0.15f
            };

        private string? ExtractJsonObject(string raw)
        {
            int s = raw.IndexOf('{');
            int e = raw.LastIndexOf('}');
            return (s >= 0 && e > s) ? raw.Substring(s, e - s + 1) : null;
        }
    }

    // =====================================================
    // VIEWMODEL
    // =====================================================
    public class KienThucTuDanhGiaVm
    {
        public KienThuc KienThuc { get; set; } = null!;
        public int TrangThai { get; set; } // 0,1,2
    }
    public class TimelineVm
    {
        public int Day { get; set; }
        public List<TimelineItemVm> Items { get; set; } = new();
    }

    public class TimelineItemVm
    {
        public int MaKT { get; set; }
        public string NoiDung { get; set; } = "";
        public string? ChuDe { get; set; }
        public int Minutes { get; set; }
        public List<TaiLieuVm> TaiLieu { get; set; } = new();
    }

    public class TaiLieuVm
    {
        public int MaTL { get; set; }
        public string TieuDe { get; set; } = "";
        public string LoaiTL { get; set; } = "";
        public string DuongDan { get; set; } = "";
    }

}
