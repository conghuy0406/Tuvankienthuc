using Microsoft.AspNetCore.Mvc;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    public class BaoCaoVanDeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BaoCaoVanDeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create(int? maDX)
        {
            ViewBag.MaDX = maDX;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int? maDX,
            string tieuDe,
            string noiDung,
            string loaiVanDe)
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var bc = new BaoCaoVanDe
            {
                MaUser = userId.Value,
                MaDX = maDX,
                TieuDe = tieuDe,
                NoiDung = noiDung,
                LoaiVanDe = loaiVanDe
            };

            _context.BaoCaoVanDes.Add(bc);
            await _context.SaveChangesAsync();

            return RedirectToAction("LichSuTuVan", "TuVan");
        }
    }

}
