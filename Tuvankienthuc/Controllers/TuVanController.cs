using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;
using Tuvankienthuc.Services;
using Tuvankienthuc.ViewModels;

namespace Tuvankienthuc.Controllers
{
    public class TuVanController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly TuVanService _svc;

        public TuVanController(ApplicationDbContext context, TuVanService svc)
        {
            _context = context;
            _svc = svc;
        }

        // =====================================================
        // 1. CHỌN MÔN HỌC
        // =====================================================
        [HttpGet]
        public IActionResult Index()
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null)
                return RedirectToAction("Login", "Auth");

            var mon = _context.MonHocs
                .Include(m => m.GiangVien)
                .OrderBy(m => m.TenMH)
                .ToList();

            return View(mon);
        }

        // =====================================================
        // 2. TỰ ĐÁNH GIÁ KIẾN THỨC
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> TuDanhGia(int maMH)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null)
                return RedirectToAction("Login", "Auth");

            var mon = await _context.MonHocs.FindAsync(maMH);
            if (mon == null)
                return NotFound();

            var ds =
                await _svc.LayDanhSachKienThucChoTuDanhGiaAsync(maSV.Value, maMH);

            ViewBag.MaMH = maMH;
            ViewBag.TenMonHoc = mon.TenMH;

            return View(ds);
        }

        // =====================================================
        // 3. AJAX CẬP NHẬT TRẠNG THÁI KIẾN THỨC
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(int maKT, bool daHieu)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null)
                return Json(false);

            var kt = await _context.KienThucSinhViens
                .FirstOrDefaultAsync(x => x.MaSV == maSV && x.MaKT == maKT);

            if (kt == null)
            {
                kt = new KienThucSinhVien
                {
                    MaSV = maSV.Value,
                    MaKT = maKT,
                    TrangThai = daHieu ? 2 : 0
                };
                _context.KienThucSinhViens.Add(kt);
            }
            else
            {
                kt.TrangThai = daHieu ? 2 : 0;
            }

            await _context.SaveChangesAsync();
            return Json(true);
        }

        // =====================================================
        // 4. REDIRECT SANG KẾT QUẢ
        // =====================================================
        [HttpPost]
        public IActionResult TuVanRedirect(int maMH, string goal, int daysLeft)
        {
            if (daysLeft <= 0) daysLeft = 7;
            return RedirectToAction(
                "TuVanKetQua",
                new { maMH, goal, daysLeft });
        }

        // =====================================================
        // 5. KẾT QUẢ TƯ VẤN (PIPELINE CHUẨN – KHÔNG FALLBACK)
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> TuVanKetQua(int maMH, string goal, int? daysLeft)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return RedirectToAction("Login", "Auth");

            int days = daysLeft ?? 7;

            var mon = await _context.MonHocs.FindAsync(maMH);
            if (mon == null) return NotFound();

            // 1️⃣ Phân tích
            var (listKT, svData) =
                await _svc.PhanTichHocTapAsync(maSV.Value, maMH);

            var scoredList =
                _svc.DuDoanThongMinh(listKT, svData, days, goal);

            var timeline =
                await _svc.BuildTimelineAsync(scoredList, days);

            var (studyPlan, goiYBoSung) =
                await _svc.SinhNoiDungTuVanTheoTimelineAsync(
                    mon.TenMH,
                    goal,
                    days,
                    timeline);

            // ===============================
            // 2️⃣ TẠO DEXUAT (CỰC QUAN TRỌNG)
            // ===============================
            var deXuat = new DeXuat
            {
                MaSV = maSV.Value,
                MaMH = maMH,
                Goal = goal,
                NoiDung = studyPlan,
                Nguon = "AI",
                ThoiGian = DateTime.Now
            };

            _context.DeXuats.Add(deXuat);
            await _context.SaveChangesAsync(); // 👉 Có MaDX ở đây

            // ===============================
            // 3️⃣ TRUYỀN SANG VIEW
            // ===============================
            ViewBag.MaDX = deXuat.MaDX;
            ViewBag.MonHoc = mon.TenMH;
            ViewBag.Goal = goal;
            ViewBag.DaysLeft = days;
            ViewBag.StudyPlan = studyPlan;
            ViewBag.GoiYBoSung = goiYBoSung;

            return View("KetQuaTuVan", timeline);
        }





        // =====================================================
        // 6. LỊCH SỬ TƯ VẤN
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> LichSu()
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null)
                return RedirectToAction("Login", "Auth");

            var list = await _context.DeXuats
                .Include(x => x.MonHoc)
                .Where(x => x.MaSV == maSV)
                .OrderByDescending(x => x.ThoiGian)
                .ToListAsync();

            return View(list);
        }


        [HttpGet]
        public async Task<IActionResult> LichSuTuVan()
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null)
                return RedirectToAction("Login", "Auth");

            var lichSu = await _context.DeXuats
                .Include(x => x.MonHoc)
                .Where(x => x.MaSV == maSV)
                .OrderByDescending(x => x.ThoiGian)
                .ToListAsync();

            return View(lichSu);
        }

        // =========================================
        // 📄 CHI TIẾT 1 LẦN TƯ VẤN
        // =========================================
        public async Task<IActionResult> ChiTietTuVan(int maDX)
        {
            var dx = await _context.DeXuats
                .Include(x => x.MonHoc)
                .FirstOrDefaultAsync(x => x.MaDX == maDX);

            if (dx == null) return NotFound();

            // ⚠️ Lấy lại dữ liệu giống lúc tư vấn
            var (listKT, svData) =
                await _svc.PhanTichHocTapAsync(dx.MaSV, dx.MaMH);

            var scored =
                _svc.DuDoanThongMinh(listKT, svData, 7, dx.Goal);

            var timeline =
                await _svc.BuildTimelineAsync(scored, 7);

            // ❗ Có thể load gợi ý AI nếu đã lưu, hoặc để trống
            var vm = new ChiTietTuVanVm
            {
                DeXuat = dx,
                Timeline = timeline,
                GoiYBoSung = new Dictionary<int, string>() // sau này mở rộng
            };

            return View(vm);
        }



    }
}
