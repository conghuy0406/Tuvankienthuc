using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Filters;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    [RoleAuthorize("GiangVien", "Admin")]
    public class GiangVienController : Controller
    {
        private readonly ApplicationDbContext _context;

        public GiangVienController(ApplicationDbContext context)
        {
            _context = context;
        }
        // =======================
        // 1️⃣ TRANG DASHBOARD GIẢNG VIÊN
        // =======================
        public async Task<IActionResult> Index()
        {
            int? gvId = HttpContext.Session.GetInt32("UserId");
            if (gvId == null)
                return RedirectToAction("Login", "Auth");

            var monHocGiangDay = await _context.MonHocs
                .Where(m => m.GiangVienId == gvId)
                .ToListAsync();

            ViewBag.SoMon = monHocGiangDay.Count;

            return View(monHocGiangDay);
        }
        // =======================
        // CHỦ ĐỀ TRONG MÔN
        // =======================
        public async Task<IActionResult> ChuDe(int maMH)
        {
            int? gvId = HttpContext.Session.GetInt32("UserId");
            if (gvId == null)
                return RedirectToAction("Login", "Auth");

            var mon = await _context.MonHocs.FindAsync(maMH);
            if (mon == null || mon.GiangVienId != gvId)
                return Forbid();

            var chuDes = await _context.ChuDes
                .Where(cd => cd.MaMH == maMH)
                .ToListAsync();

            ViewBag.MonHoc = mon;
            return View(chuDes);
        }

        // GET: Thêm chủ đề
        [HttpGet]
        public IActionResult ThemChuDe(int maMH)
        {
            var cd = new ChuDe
            {
                MaMH = maMH
            };
            return View(cd);
        }

        // POST: Thêm chủ đề
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemChuDe(ChuDe cd)
        {
            if (!ModelState.IsValid)
            {
                return View(cd);
            }

            // MoTa không nullable => đảm bảo không null
            cd.MoTa ??= "";

            _context.ChuDes.Add(cd);
            await _context.SaveChangesAsync();

            return RedirectToAction("ChuDe", new { maMH = cd.MaMH });
        }

        // GET: Sửa chủ đề
        [HttpGet]
        public async Task<IActionResult> SuaChuDe(int id)
        {
            var cd = await _context.ChuDes.FindAsync(id);
            if (cd == null) return NotFound();
            return View(cd);
        }

        // POST: Sửa chủ đề
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaChuDe(ChuDe cd)
        {
            if (!ModelState.IsValid)
                return View(cd);

            cd.MoTa ??= "";

            _context.ChuDes.Update(cd);
            await _context.SaveChangesAsync();

            return RedirectToAction("ChuDe", new { maMH = cd.MaMH });
        }

        // Xóa chủ đề
        public async Task<IActionResult> XoaChuDe(int id)
        {
            var cd = await _context.ChuDes.FindAsync(id);
            if (cd != null)
            {
                int maMH = cd.MaMH;
                _context.ChuDes.Remove(cd);
                await _context.SaveChangesAsync();
                return RedirectToAction("ChuDe", new { maMH });
            }
            return RedirectToAction("Index");
        }

        // =======================
        // KIẾN THỨC TRONG CHỦ ĐỀ
        // =======================
        public async Task<IActionResult> KienThuc(int maCD)
        {
            int? gvId = HttpContext.Session.GetInt32("UserId");
            if (gvId == null)
                return RedirectToAction("Login", "Auth");

            var cd = await _context.ChuDes
                .Include(c => c.MonHoc)
                .FirstOrDefaultAsync(c => c.MaCD == maCD);

            if (cd == null || cd.MonHoc?.GiangVienId != gvId)
                return Forbid();

            var list = await _context.KienThucs
                .Where(k => k.MaCD == maCD)
                .ToListAsync();

            ViewBag.ChuDe = cd;
            return View(list);
        }

        // GET: Thêm kiến thức
        [HttpGet]
        public IActionResult ThemKienThuc(int maCD)
        {
            var kt = new KienThuc
            {
                MaCD = maCD,
                DoKho = 1,
                SoKienThucTruoc = 0,
                IsKienThucCoBan = false
            };
            return View(kt);
        }

        // POST: Thêm kiến thức
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ThemKienThuc(KienThuc kt)
        {
            if (!ModelState.IsValid)
                return View(kt);

            _context.KienThucs.Add(kt);
            await _context.SaveChangesAsync();

            return RedirectToAction("KienThuc", new { maCD = kt.MaCD });
        }

        // GET: Sửa kiến thức
        [HttpGet]
        public async Task<IActionResult> SuaKienThuc(int id)
        {
            var kt = await _context.KienThucs.FindAsync(id);
            if (kt == null) return NotFound();
            return View(kt);
        }

        // POST: Sửa kiến thức
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuaKienThuc(KienThuc kt)
        {
            if (!ModelState.IsValid)
                return View(kt);

            _context.KienThucs.Update(kt);
            await _context.SaveChangesAsync();

            return RedirectToAction("KienThuc", new { maCD = kt.MaCD });
        }

        // Xóa kiến thức
        public async Task<IActionResult> XoaKienThuc(int id)
        {
            var kt = await _context.KienThucs.FindAsync(id);
            if (kt != null)
            {
                int maCD = kt.MaCD;
                _context.KienThucs.Remove(kt);
                await _context.SaveChangesAsync();
                return RedirectToAction("KienThuc", new { maCD });
            }
            return RedirectToAction("Index");
        }
    }
}
