using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Services;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    public class ChatController : Controller
    {
        private readonly GeminiService _gemini;
        private readonly ApplicationDbContext _context;

        public ChatController(GeminiService gemini, ApplicationDbContext context)
        {
            _gemini = gemini;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        public class ChatRequest
        {
            public string Message { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] ChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Message))
                return BadRequest();

            int? maSV = HttpContext.Session.GetInt32("MaSV");

            string contextText = "";

            if (maSV != null)
            {
                var deXuat = await _context.DeXuats
                    .Where(dx => dx.MaSV == maSV)
                    .OrderByDescending(dx => dx.ThoiGian)
                    .FirstOrDefaultAsync();

                if (deXuat != null)
                {
                    var chiTiet = await _context.DeXuatChiTiets
                        .Include(ct => ct.KienThuc)
                        .Where(ct => ct.MaDX == deXuat.MaDX)
                        .OrderByDescending(ct => ct.Score)
                        .Take(5)
                        .ToListAsync();

                    if (chiTiet.Any())
                    {
                        contextText = "Thông tin học tập của sinh viên:\n";
                        foreach (var ct in chiTiet)
                        {
                            contextText +=
                                $"- {ct.KienThuc.NoiDung}, điểm: {ct.Score}, lý do: {ct.Reason}\n";
                        }
                    }
                }
            }

            var prompt = $@"
Bạn là AI tư vấn học tập cho người dùng
Trả lời ngắn gọn, dễ hiểu, đúng trọng tâm.

{contextText}

Câu hỏi:
{req.Message}
";

            var reply = await _gemini.AskAsync(prompt);

            if (maSV != null)
            {
                var log = new ChatLog
                {
                    MaSV = maSV.Value,
                    MaDX = null,
                    NoiDung = req.Message,
                    TraLoi = reply,
                    ThoiGian = DateTime.Now
                };

                _context.ChatLogs.Add(log);
                await _context.SaveChangesAsync();
            }

            return Json(new { reply });
        }
    }
}
