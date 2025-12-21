using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    public class DeXuatDanhGiaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeXuatDanhGiaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==============================
        // POST: Sinh viên gửi đánh giá
        // ==============================
        [HttpPost]
        public async Task<IActionResult> Create(int maDX, int rating, string? nhanXet)
        {
            int? maUser = HttpContext.Session.GetInt32("UserId");
            if (maUser == null) return RedirectToAction("Login", "Auth");

            var dg = new DeXuatDanhGia
            {
                MaDX = maDX,
                MaUser = maUser.Value,
                Rating = rating,
                NhanXet = nhanXet,
                ThoiGian = DateTime.Now
            };

            _context.DeXuatDanhGias.Add(dg);
            await _context.SaveChangesAsync();

            return RedirectToAction("LichSuTuVan", "TuVan");
        }

    }
}
