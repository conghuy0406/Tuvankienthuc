using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;
using Tuvankienthuc.Services;

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

            // 1️⃣ Data
            var (listKT, svData) =
                await _svc.PhanTichHocTapAsync(maSV.Value, maMH);

            // 2️⃣ SCORE – LOCAL (KHÔNG AI, KHÔNG FALLBACK)
            var scoredList =
                _svc.DuDoanThongMinh(listKT, svData, days, goal);

            // 3️⃣ TIMELINE – LOCAL
            string timelineJson =
                _svc.BuildTimelineJson(scoredList, days);

            // 4️⃣ AI – GỘP STUDY PLAN + GỢI Ý (1 LẦN DUY NHẤT)
            var (studyPlan, goiYBoSung) =
                await _svc.SinhNoiDungTuVanAsync(
                    mon.TenMH,
                    goal,
                    days,
                    timelineJson,
                    scoredList);

            // 5️⃣ Push ViewBag
            ViewBag.MonHoc = mon.TenMH;
            ViewBag.Goal = goal;
            ViewBag.DaysLeft = days;

            ViewBag.TimelineJson = timelineJson;
            ViewBag.StudyPlan = studyPlan;
            ViewBag.GoiYBoSung = goiYBoSung;

            // ⚠️ TRẢ ĐÚNG MODEL
            return View("KetQuaTuVan", scoredList);
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
    }
}
