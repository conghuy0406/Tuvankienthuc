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
                "gemini-2.5-flash", 
                "gemini-2.5-pro", 
                "gemini-1.5-flash-latest",
                "gemini-2.0-flash",
                "gemini-2.0-flash-lite",
                 "gemini-1.5-flash-latest"
            };

            foreach (var model in models)
            {
                try
                {
                    string endpoint =
                        $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                    var payload = new
                    {
                        contents = new[]
                        {
                            new { parts = new[] { new { text = prompt } } }
                        }
                    };

                    var res = await client.PostAsync(
                        endpoint,
                        new StringContent(
                            JsonSerializer.Serialize(payload),
                            Encoding.UTF8,
                            "application/json"));

                    if (!res.IsSuccessStatusCode) continue;

                    var json = await res.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);

                    var text = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                        return text.Trim();
                }
                catch
                {
                    continue;
                }
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
        public async Task<List<KienThucTuDanhGiaVm>>
            LayDanhSachKienThucChoTuDanhGiaAsync(int maSV, int maMH)
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

            foreach (var kt in kts)
            {
                if (!string.IsNullOrWhiteSpace(kt.CauHoiAI)) continue;

                string prompt = $@"
Tạo 1 câu hỏi tự đánh giá NGẮN (1 dòng, tiếng Việt).
Môn: {mon?.TenMH}
Chủ đề: {kt.ChuDe?.TenCD}
Kiến thức: {kt.NoiDung}
Chỉ trả về 1 câu hỏi.";

                var q = await CallAIAsync(prompt);
                if (!string.IsNullOrWhiteSpace(q))
                {
                    kt.CauHoiAI = q;
                    _context.KienThucs.Update(kt);
                }
            }

            await _context.SaveChangesAsync();

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
        public string BuildTimelineJson(
            List<(KienThuc kt, float score)> ds,
            int daysLeft)
        {
            if (daysLeft <= 0) daysLeft = 1;

            var timeline = new List<object>();
            int index = 0;

            for (int day = 1; day <= daysLeft; day++)
            {
                var items = new List<object>();

                for (int i = 0; i < 2 && index < ds.Count; i++)
                {
                    var kt = ds[index++].kt;

                    int minutes =
                        kt.DoKho >= 7 ? 40 :
                        kt.DoKho >= 5 ? 30 : 25;

                    items.Add(new
                    {
                        MaKT = kt.MaKT,
                        noiDung = kt.NoiDung,
                        minutes
                    });
                }

                timeline.Add(new { day, items });
            }

            return JsonSerializer.Serialize(
                timeline,
                new JsonSerializerOptions { WriteIndented = true });
        }

        // =====================================================
        // 5. AI – GỘP STUDY PLAN + GỢI Ý BỔ SUNG (1 LẦN)
        // =====================================================
        public async Task<(string plan, Dictionary<int, string> goiY)>
            SinhNoiDungTuVanAsync(
                string monHoc,
                string goal,
                int daysLeft,
                string timelineJson,
                List<(KienThuc kt, float score)> ds)
        {
            var topKT = ds.Take(5).Select(x => new
            {
                x.kt.MaKT,
                x.kt.NoiDung
            });

            string prompt = $@"
Bạn là giảng viên hướng dẫn ôn tập đại học.

MÔN: {monHoc}
MỤC TIÊU: {goal}
SỐ NGÀY CÒN LẠI: {daysLeft}

TIMELINE:
{timelineJson}

KIẾN THỨC ƯU TIÊN:
{JsonSerializer.Serialize(topKT)}

YÊU CẦU:
1. Viết STUDY PLAN thực tế, dùng để ôn thi thật (200–300 từ).
2. Với mỗi kiến thức, ghi gợi ý ôn tập ngắn (2–3 dòng).

TRẢ VỀ DUY NHẤT JSON:
{{
  ""studyPlan"": ""...text..."",
  ""goiY"": {{
     ""1"": ""...gợi ý...""
  }}
}}
CHỈ JSON.";

            var raw = await CallAIAsync(prompt);
            if (string.IsNullOrWhiteSpace(raw))
                return ("Hãy học theo timeline đã đề xuất.", new());

            var json = ExtractJsonObject(raw) ?? raw;
            using var doc = JsonDocument.Parse(json);

            string plan =
                doc.RootElement.GetProperty("studyPlan").GetString() ?? "";

            var dict = new Dictionary<int, string>();
            foreach (var p in doc.RootElement.GetProperty("goiY").EnumerateObject())
                if (int.TryParse(p.Name, out int id))
                    dict[id] = p.Value.GetString() ?? "";

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
}
