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

        // ╔═════════════════════════════════════════════════╗
        //            0. HÀM GỌI GEMINI - CỐT LÕI
        // ╚═════════════════════════════════════════════════╝
        private async Task<(string? text, string? modelUsed)> GoiGeminiAsync(string prompt)
        {
            try
            {
                string apiKey = _config["Gemini:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                    return (null, null);

                var client = _httpClientFactory.CreateClient();

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
                                int code = (int)res.StatusCode;
                                if (code == 503 || code == 429 ||
                                    json.Contains("overloaded", StringComparison.OrdinalIgnoreCase) ||
                                    json.Contains("UNAVAILABLE", StringComparison.OrdinalIgnoreCase) ||
                                    json.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
                                    continue;

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
                                return (text.Trim(), model);

                            continue;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }

                return (null, null);
            }
            catch
            {
                return (null, null);
            }
        }

        // ╔═════════════════════════════════════════════════╗
        //                     1. PHÂN TÍCH DỮ LIỆU
        // ╚═════════════════════════════════════════════════╝
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

        // ╔═════════════════════════════════════════════════╗
        //           2. SINH CÂU HỎI TỰ ĐÁNH GIÁ (AI)
        // ╚═════════════════════════════════════════════════╝
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
                vm.Add(new KienThucTuDanhGiaVm { KienThuc = kt, TrangThai = tt });
            }

            return vm;
        }

        private async Task<string?> TaoCauHoiAI(KienThuc kt, string monHoc)
        {
            string prompt = $@"
Tạo 1 câu hỏi tự đánh giá (ngắn, 1 dòng – tiếng Việt) cho sinh viên:
- Môn: {monHoc}
- Chủ đề: {kt.ChuDe?.TenCD}
- Kiến thức: {kt.NoiDung}

Chỉ trả về đúng 1 câu hỏi.";

            var (text, model) = await GoiGeminiAsync(prompt);
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        // ╔═════════════════════════════════════════════════╗
        //                 3. TÍNH SCORE (HEURISTIC)
        // ╚═════════════════════════════════════════════════╝
        public (List<(KienThuc kt, float score)> ds, string modelUsed)
            DuDoanThongMinh(List<KienThuc> kts, List<KienThucSinhVien> svData, int daysLeft, string goal)
        {
            if (daysLeft <= 0) daysLeft = 1;

            var result = new List<(KienThuc kt, float score)>();

            foreach (var kt in kts)
            {
                int trangThai = svData.FirstOrDefault(s => s.MaKT == kt.MaKT)?.TrangThai ?? 0;

                float understanding = trangThai switch
                {
                    2 => 1.0f,
                    1 => 0.5f,
                    _ => 0.0f
                };

                float doKhoNorm = Math.Clamp((float)kt.DoKho / 10f, 0f, 1f);
                float tienDeNorm = Math.Clamp((float)kt.SoKienThucTruoc / 10f, 0f, 1f);

                float important = 0.5f + 0.3f * doKhoNorm + 0.2f * tienDeNorm;

                if (kt.IsKienThucCoBan)
                    important += 0.3f;

                important += (float)kt.SoKienThucTruoc / 50f;
                important += GoalWeight(goal);

                float timePressure = 1f + Math.Max(0, 30 - daysLeft) / 40f;

                float score = important * (1 - understanding) * timePressure;

                result.Add((kt, score));
            }

            return (result.OrderByDescending(x => x.score).ToList(), "");
        }

        // ╔═════════════════════════════════════════════════╗
        //                  4. AI XẾP THỨ TỰ HỌC
        // ╚═════════════════════════════════════════════════╝
        public async Task<List<(KienThuc kt, float score)>> SapXepLaiBangAIAsync(
            List<(KienThuc kt, float score)> ds, string monHoc)
        {
            try
            {
                var simpleList = ds.Select(x => new
                {
                    maKT = x.kt.MaKT,
                    noiDung = x.kt.NoiDung,
                    score = x.score,
                    doKho = x.kt.DoKhoAI ?? x.kt.DoKho,
                    prereq = x.kt.PrereqCountAI ?? x.kt.SoKienThucTruoc,
                    isCore = x.kt.IsCoreAI ?? x.kt.IsKienThucCoBan
                }).ToList();

                string jsonList = JsonSerializer.Serialize(simpleList, new JsonSerializerOptions { WriteIndented = true });

                string prompt = $@"
Bạn là cố vấn học tập giàu kinh nghiệm.

Đây là danh sách kiến thức của môn ""{monHoc}"":

{jsonList}

Hãy sắp xếp lại thứ tự học sao cho tối ưu:

- score cao ưu tiên trước
- kiến thức nền tảng (isCore) học sớm
- kiến thức có prereq lớn học sau
- độ khó cao học cuối cùng (nhưng vẫn tôn trọng score)

Trả về JSON:

{{ ""orderedIds"": [ danh sách MaKT ] }}

Chỉ JSON.";

                var (text, model) = await GoiGeminiAsync(prompt);
                if (string.IsNullOrWhiteSpace(text)) return ds;

                using var doc = JsonDocument.Parse(text);
                var ordered = doc.RootElement.GetProperty("orderedIds")
                                             .EnumerateArray()
                                             .Select(x => x.GetInt32())
                                             .ToList();

                var map = ds.ToDictionary(x => x.kt.MaKT, x => x);
                var finalList = new List<(KienThuc kt, float score)>();

                foreach (int id in ordered)
                    if (map.ContainsKey(id))
                        finalList.Add(map[id]);

                foreach (var item in ds)
                    if (!finalList.Any(x => x.kt.MaKT == item.kt.MaKT))
                        finalList.Add(item);

                return finalList;
            }
            catch
            {
                return ds;
            }
        }

        // ╔═════════════════════════════════════════════════╗
        //                5. TIMELINE JSON BUILDER
        // ╚═════════════════════════════════════════════════╝
        public string BuildTimelineJson(List<(KienThuc kt, float score)> ds, int daysLeft)
        {
            if (daysLeft <= 0) daysLeft = 1;

            var timeline = new List<object>();
            int index = 0;
            int total = ds.Count;

            for (int day = 1; day <= daysLeft; day++)
            {
                var items = new List<object>();

                // Mỗi ngày tối đa 2–3 kiến thức
                int maxItems = total > 10 ? 3 : 2;

                for (int i = 0; i < maxItems && index < total; i++)
                {
                    var kt = ds[index].kt;

                    int minutes =
                        (kt.DoKhoAI ?? kt.DoKho) >= 7 ? 40 :
                        (kt.DoKhoAI ?? kt.DoKho) >= 5 ? 30 :
                        (kt.DoKhoAI ?? kt.DoKho) >= 3 ? 25 : 20;

                    items.Add(new
                    {
                        NoiDung = kt.NoiDung,
                        MaKT = kt.MaKT,
                        minutes
                    });

                    index++;
                }

                // 👉 Dù items rỗng vẫn add, để đảm bảo đủ day 1..daysLeft
                timeline.Add(new
                {
                    day,
                    items
                });
            }

            return JsonSerializer.Serialize(timeline, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        // ╔═════════════════════════════════════════════════╗
        //                    6. STUDY PLAN AI
        // ╚═════════════════════════════════════════════════╝
        public async Task<(string plan, string model)> SinhStudyPlanAsync(
            string monHoc, string goal, int daysLeft, List<(KienThuc kt, float score)> ds)
        {
            try
            {
                string timelineJson = BuildTimelineJson(ds, daysLeft);

                string prompt = $@"
Bạn là cố vấn học tập chuyên nghiệp.

Môn: {monHoc}
Mục tiêu: ""{goal}""
Số ngày còn lại: {daysLeft}

Timeline hệ thống:

{timelineJson}

Hãy viết Study Plan chi tiết:
- Dạng từng ngày: “Ngày X: …”
- Liệt kê kiến thức + số phút
- Giải thích ngắn lý do thứ tự học
- Giọng văn rõ ràng, dễ hiểu, khích lệ
- Khoảng 300–400 từ

Chỉ trả về nội dung text.";

                var (text, modelUsed) = await GoiGeminiAsync(prompt);
                if (string.IsNullOrWhiteSpace(text))
                    return ("AI đang bận, hãy học theo timeline.", "fallback");

                return (text.Trim(), modelUsed ?? "unknown");
            }
            catch
            {
                return ("AI gặp lỗi, hãy học theo danh sách ưu tiên.", "fallback");
            }
        }

        // ╔═════════════════════════════════════════════════╗
        //            7. GIẢI THÍCH TOP KIẾN THỨC (AI)
        // ╚═════════════════════════════════════════════════╝
        public async Task<Dictionary<int, string>>
            TaoMoTaAITheoKienThucAsync(string goal, string monHoc, List<(KienThuc kt, float score)> ds)
        {
            var dict = new Dictionary<int, string>();

            foreach (var item in ds.Take(3))
            {
                var kt = item.kt;
                string prompt = $@"
Giải thích ngắn (2–3 câu) vì sao kiến thức sau quan trọng
đối với mục tiêu '{goal}' trong môn '{monHoc}':

- {kt.NoiDung}

Trả về đoạn văn.";

                var (text, _) = await GoiGeminiAsync(prompt);

                if (!string.IsNullOrWhiteSpace(text))
                    dict[kt.MaKT] = text.Trim();
            }

            return dict;
        }

        // ╔═════════════════════════════════════════════════╗
        //                 8. LƯU DỮ LIỆU TƯ VẤN
        // ╚═════════════════════════════════════════════════╝
        public async Task<DeXuat> LuuKetQuaAsync(int maSV, int maMH,
            string goal, string reason, List<(KienThuc kt, float score)> ds)
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

        public async Task LuuChatLogAsync(int maSV, int? maDX, string cauHoi, string traLoi)
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
        public async Task<(string json, string model)> TaoTimelineBangAIAsync(
            string monHoc,
            string goal,
            int daysLeft,
            List<(KienThuc kt, float score)> ds)
        {
            try
            {
                if (daysLeft <= 0) daysLeft = 1;

                // Chuẩn hóa dữ liệu gửi AI
                var simpleList = ds.Select(x => new
                {
                    noiDung = x.kt.NoiDung,
                    doKho = x.kt.DoKhoAI ?? x.kt.DoKho,
                    score = x.score
                }).ToList();

                string jsonInput = JsonSerializer.Serialize(simpleList, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // PROMPT mạnh - yêu cầu JSON đúng chuẩn
                string prompt = $@"
Bạn là hệ thống lập kế hoạch học tập.

Dữ liệu kiến thức:
{jsonInput}

YÊU CẦU:
- TRẢ VỀ DUY NHẤT JSON ARRAY.
- Phải có đủ day = 1..{daysLeft}.
- items có thể rỗng.
- Mỗi mục:
  {{
     ""day"": 1,
     ""items"": [
        {{ ""noiDung"": ""text"", ""minutes"": 20 }}
     ]
  }}
- minutes: số nguyên 15–45.
- Không được trả bất kỳ text nào ngoài JSON.

CHỈ TRẢ JSON ARRAY KHÔNG CHỮ KÈM THEO.
";

                var (text, modelUsed) = await GoiGeminiAsync(prompt);

                Console.WriteLine("===== RAW TIMELINE RESPONSE =====");
                Console.WriteLine(text);

                if (string.IsNullOrWhiteSpace(text))
                    return (BuildTimelineJson(ds, daysLeft), "fallback");

                // Lấy đúng phần JSON từ output
                string? extracted = ExtractJsonArray(text);
                if (extracted == null)
                    return (BuildTimelineJson(ds, daysLeft), "fallback");

                // Parse JSON general (không mapping cứng property)
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(extracted);
                }
                catch
                {
                    return (BuildTimelineJson(ds, daysLeft), "fallback");
                }

                var normalized = new List<object>();

                // Bảo đảm đủ day 1..daysLeft
                for (int d = 1; d <= daysLeft; d++)
                {
                    var element = doc.RootElement
                                     .EnumerateArray()
                                     .FirstOrDefault(x =>
                                         x.TryGetProperty("day", out var v) &&
                                         v.GetInt32() == d);

                    List<object> items = new();

                    if (element.ValueKind != JsonValueKind.Undefined &&
                        element.TryGetProperty("items", out var its))
                    {
                        foreach (var item in its.EnumerateArray())
                        {
                            string nd = NormalizeKey(item, "noiDung", "NoiDung");
                            int min = item.TryGetProperty("minutes", out var m) ? m.GetInt32() : 20;

                            items.Add(new
                            {
                                noiDung = nd,
                                minutes = min
                            });
                        }
                    }

                    normalized.Add(new
                    {
                        day = d,
                        items = items
                    });
                }

                string finalJson = JsonSerializer.Serialize(normalized, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return (finalJson, modelUsed ?? "gemini-2.5-flash");
            }
            catch
            {
                return (BuildTimelineJson(ds, daysLeft), "fallback");
            }
        }


        public async Task<List<(KienThuc kt, float score)>> TinhScoreBangAIAsync(
    List<KienThuc> kts, 
    List<KienThucSinhVien> svData,
    int daysLeft, 
    string goal)
{
    try
    {
        var input = kts.Select(kt => new 
        {
            MaKT = kt.MaKT,
            NoiDung = kt.NoiDung,
            DoKho = kt.DoKhoAI ?? kt.DoKho,
            Prereq = kt.PrereqCountAI ?? kt.SoKienThucTruoc,
            IsCore = kt.IsCoreAI ?? kt.IsKienThucCoBan,
            Understanding = svData.FirstOrDefault(s => s.MaKT == kt.MaKT)?.TrangThai switch
            {
                2 => 1.0,
                1 => 0.5,
                _ => 0.0
            }
        }).ToList();

        string jsonInput = JsonSerializer.Serialize(input, new JsonSerializerOptions { WriteIndented = true });

        string prompt = $@"
Bạn là chuyên gia lập lộ trình học.

Dữ liệu kiến thức:
{jsonInput}

Mục tiêu: {goal}
Số ngày còn lại: {daysLeft}

Hãy tính SCORE cho từng kiến thức theo quy tắc:
- Score 0–1 (càng cao càng ưu tiên học trước)
- Độ khó tăng score
- Tiền đề tăng score
- Kiến thức nền tảng ưu tiên
- Nếu Understanding=1.0 thì giảm score mạnh
- Áp lực thời gian ít ngày → score cao hơn

TRẢ VỀ DUY NHẤT JSON ARRAY:
[
  {{ ""MaKT"": 1, ""Score"": 0.92 }},
  {{ ""MaKT"": 2, ""Score"": 0.15 }}
]

KHÔNG VIẾT THÊM GIẢI THÍCH.
";

        var (text, model) = await GoiGeminiAsync(prompt);

        if (string.IsNullOrWhiteSpace(text))
            return null; // để fallback

        var parsed = JsonSerializer.Deserialize<List<AiScoreResult>>(text);

        if (parsed == null) return null;

        // Map lại vào KienThuc
        var map = parsed.ToDictionary(x => x.MaKT, x => x.Score);

                return kts.Select(k =>
            (kt: k, score: map.ContainsKey(k.MaKT) ? (float)map[k.MaKT] : 0f)
        ).ToList();
            }
    catch
    {
        return null; // fallback
    }
}

        // ╔═════════════════════════════════════════════════╗
        //     10. TÍNH SCORE TỔNG HỢP (AI → fallback)
        // ╚═════════════════════════════════════════════════╝
        public async Task<List<(KienThuc kt, float score)>> TinhScoreTongHopAsync(
            List<KienThuc> kts,
            List<KienThucSinhVien> svData,
            int daysLeft,
            string goal)
        {
            // 1) Gọi AI trước
            var ai = await TinhScoreBangAIAsync(kts, svData, daysLeft, goal);

            if (ai != null && ai.Count > 0 && ai.Any(x => x.score > 0))
            {
                Console.WriteLine(">>> SCORE TỪ AI");
                return ai.OrderByDescending(x => x.score).ToList();
            }

            // 2) Nếu AI fail → fallback heuristic
            Console.WriteLine(">>> FALLBACK SCORE (HEURISTIC)");
            var (fb, _) = DuDoanThongMinh(kts, svData, daysLeft, goal);
            return fb;
        }

        // ─────────────────────────────────────────────
        //  Extract JSON ARRAY từ output của Gemini
        // ─────────────────────────────────────────────
        private string? ExtractJsonArray(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            int start = raw.IndexOf('[');
            int end = raw.LastIndexOf(']');

            if (start >= 0 && end > start)
                return raw.Substring(start, end - start + 1);

            return null;
        }

        // ─────────────────────────────────────────────
        //  Chuẩn hóa key property (AI có thể trả noiDung hoặc NoiDung)
        // ─────────────────────────────────────────────
        private string NormalizeKey(JsonElement item, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (item.TryGetProperty(key, out var val))
                    return val.GetString() ?? "";
            }
            return "";
        }


        // ╔═════════════════════════════════════════════════╗
        //                9. HỖ TRỢ (GOAL + HOURS)
        // ╚═════════════════════════════════════════════════╝
        private float GoalWeight(string goal) =>
            goal switch
            {
                "Thi qua môn" => 0.2f,
                "Đạt điểm cao" => 0.4f,
                "Hiểu sâu kiến thức" => 0.5f,
                "Làm bài tập lớn" => 0.3f,
                _ => 0.2f
            };

        private float EstimateStudyHours(KienThuc kt)
        {
            float baseHour = 1.0f;
            float diff = (float)kt.DoKho / 5f;
            float prereq = (float)kt.SoKienThucTruoc / 5f;

            return baseHour + diff + prereq;
        }
        // Dùng để deserialize JSON timeline từ AI
        private class AiTimelineDay
        {
            public int day { get; set; }
            public List<AiTimelineItem> items { get; set; } = new();
        }

        private class AiTimelineItem
        {
            public string NoiDung { get; set; } = "";
            public int minutes { get; set; }
        }
        private class AiScoreResult
        {
            public int MaKT { get; set; }
            public double Score { get; set; }
        }

    }


    // ╔═══════════════════════════════════════════╗
    //            VIEWMODEL DÙNG CHO UI
    // ╚═══════════════════════════════════════════╝
    public class KienThucTuDanhGiaVm
    {
        public KienThuc KienThuc { get; set; }
        /// <summary>
        /// 0 = chưa học, 1 = cần ôn, 2 = đã hiểu
        /// </summary>
        public int TrangThai { get; set; }
    }

}
