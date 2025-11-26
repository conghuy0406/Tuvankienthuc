using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;
using Tuvankienthuc.Services;

namespace Tuvankienthuc.Controllers
{
    [Authorize(Roles = "SinhVien,Admin")]
    public class TuVanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TuVanService _svc;

        public TuVanController(ApplicationDbContext context, TuVanService svc)
        {
            _context = context;
            _svc = svc;
        }

        // ==========================================================================================
        // CHỌN MÔN
        // ==========================================================================================
        public async Task<IActionResult> Index()
        {
            ViewBag.MonHocs = await _context.MonHocs.ToListAsync();
            return View();
        }

        // ==========================================================================================
        // TỰ ĐÁNH GIÁ KIẾN THỨC
        // ==========================================================================================
        public async Task<IActionResult> TuDanhGia(int maMH)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return RedirectToAction("Login", "Auth");

            var mon = await _context.MonHocs.FindAsync(maMH);
            if (mon == null) return NotFound();

            ViewBag.MaMH = maMH;
            ViewBag.TenMonHoc = mon.TenMH;

            var list = await _svc.LayDanhSachKienThucChoTuDanhGiaAsync(maSV.Value, maMH);
            return View(list);
        }

        // cập nhật trạng thái tick
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(int maKT, bool daHieu)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return Unauthorized();

            var rec = await _context.KienThucSinhViens
                .FirstOrDefaultAsync(x => x.MaSV == maSV && x.MaKT == maKT);

            if (rec == null)
            {
                rec = new KienThucSinhVien
                {
                    MaSV = maSV.Value,
                    MaKT = maKT,
                    TrangThai = daHieu ? 2 : 1,
                    LanHocCuoi = DateTime.Now
                };

                _context.KienThucSinhViens.Add(rec);
            }
            else
            {
                rec.TrangThai = daHieu ? 2 : 1;
                rec.LanHocCuoi = DateTime.Now;
                _context.KienThucSinhViens.Update(rec);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // ==========================================================================================
        // TRẢ KẾT QUẢ TƯ VẤN
        // ==========================================================================================
        [HttpPost]
        public async Task<IActionResult> TuVanKetQua(int maMH, string goal, int? daysLeft)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return RedirectToAction("Login", "Auth");

            var mon = await _context.MonHocs.FindAsync(maMH);
            if (mon == null) return NotFound();

            var (listKT, svData) = await _svc.PhanTichHocTapAsync(maSV.Value, maMH);

            // thuật toán thông minh
            var (ds, modelUsed1) = _svc.DuDoanThongMinh(listKT, svData, daysLeft ?? 0);

            // sinh lời khuyên
            var (loiKhuyen, modelUsed2) =
                await _svc.TaoLoiKhuyenAIAsync(goal, mon.TenMH, ds, daysLeft ?? 0);

            string modelFinal = !string.IsNullOrWhiteSpace(modelUsed1) ? modelUsed1 : modelUsed2;

            var moTaKT = await _svc.TaoMoTaAITheoKienThucAsync(goal, mon.TenMH, ds);

            // push to view
            ViewBag.TenMonHoc = mon.TenMH;
            ViewBag.Goal = goal;
            ViewBag.DaysLeft = daysLeft ?? 0;
            ViewBag.AIModel = modelFinal;
            ViewBag.LoiKhuyen = loiKhuyen;
            ViewBag.MoTaKT = moTaKT;

            return View("KetQuaTuVan", ds);
        }
    }
}
