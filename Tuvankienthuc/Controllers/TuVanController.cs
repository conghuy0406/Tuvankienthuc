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

        // ╔══════════════════════════════════════╗
        // 1) TRANG CHỌN MÔN HỌC
        // ╚══════════════════════════════════════╝
        [HttpGet]
        public IActionResult Index()
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return RedirectToAction("Login", "Auth");

            var mon = _context.MonHocs
                    .Include(m => m.GiangVien)
                    .OrderBy(m => m.TenMH)
                    .ToList();

            return View(mon);
        }

        // ╔══════════════════════════════════════╗
        // 2) TRANG TỰ ĐÁNH GIÁ KIẾN THỨC
        // ╚══════════════════════════════════════╝
        [HttpGet]
        public async Task<IActionResult> TuDanhGia(int maMH)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return RedirectToAction("Login", "Auth");

            var mon = await _context.MonHocs.FindAsync(maMH);
            if (mon == null) return NotFound();

            var ds = await _svc.LayDanhSachKienThucChoTuDanhGiaAsync(maSV.Value, maMH);

            ViewBag.MaMH = maMH;
            ViewBag.TenMonHoc = mon.TenMH;

            return View(ds);
        }

        // ╔══════════════════════════════════════╗
        // 3) AJAX UPDATE TRẠNG THÁI KIẾN THỨC
        // ╚══════════════════════════════════════╝
        [HttpPost]
        public async Task<IActionResult> CapNhatTrangThai(int maKT, bool daHieu)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return Json(false);

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
                _context.KienThucSinhViens.Update(kt);
            }

            await _context.SaveChangesAsync();
            return Json(true);
        }

        // ╔══════════════════════════════════════╗
        // 4) TRANG LOADING
        // ╚══════════════════════════════════════╝
        [HttpPost]
        public IActionResult TuVanRedirect(int maMH, string goal, int daysLeft)
        {
            if (daysLeft <= 0) daysLeft = 7;

            return RedirectToAction("TuVanKetQua", new { maMH, goal, daysLeft });
        }


        // ╔══════════════════════════════════════╗
        // 5) TRANG KẾT QUẢ TƯ VẤN (FULL AI PIPELINE)
        // ╚══════════════════════════════════════╝
        [HttpGet]
        public async Task<IActionResult> TuVanKetQua(int maMH, string goal, int? daysLeft)
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return RedirectToAction("Login", "Auth");

            int days = daysLeft ?? 7;

            // Bước 1 — Lấy môn học
            var mon = await _context.MonHocs.FindAsync(maMH);
            if (mon == null) return NotFound();

            // Bước 2 — Lấy danh sách kiến thức & trạng thái sinh viên
            var (listKT, svData) =
                await _svc.PhanTichHocTapAsync(maSV.Value, maMH);

            // Bước 3 — Tính SCORE qua AI trước → fallback heuristic
            var scoreList =
                await _svc.TinhScoreTongHopAsync(listKT, svData, days, goal);
            // ✔ scoreList = List<(KienThuc kt, float score)>

            // Bước 4 — AI reorder thứ tự học
            var finalList = await _svc.SapXepLaiBangAIAsync(scoreList, mon.TenMH);

            // Bước 5 — AI tạo TIMELINE JSON (chuẩn, không fallback thủ công)
            var (aiTimeline, modelTimeline) =
                await _svc.TaoTimelineBangAIAsync(mon.TenMH, goal, days, finalList);

            // Bước 6 — AI sinh kế hoạch học tập (Study Plan)
            var (studyPlan, modelPlan) =
                await _svc.SinhStudyPlanAsync(mon.TenMH, goal, days, finalList);

            // Bước 7 — AI giải thích lý do top kiến thức quan trọng
            var moTaKT =
                await _svc.TaoMoTaAITheoKienThucAsync(goal, mon.TenMH, finalList);

            // ----- Push dữ liệu lên View -----
            ViewBag.MonHoc = mon.TenMH;
            ViewBag.Goal = goal;
            ViewBag.DaysLeft = days;

            ViewBag.StudyPlan = studyPlan;
            ViewBag.ModelPlan = modelPlan;

            ViewBag.MoTaKT = moTaKT;

            // Timeline AI
            ViewBag.TimelineJson = aiTimeline;
            ViewBag.TimelineModel = modelTimeline;

            // Trả ra View
            return View("KetQuaTuVan", finalList);
        }


        // ╔══════════════════════════════════════╗
        // 6) LỊCH SỬ TƯ VẤN
        // ╚══════════════════════════════════════╝
        [HttpGet]
        public async Task<IActionResult> LichSu()
        {
            int? maSV = HttpContext.Session.GetInt32("UserId");
            if (maSV == null) return RedirectToAction("Login", "Auth");

            var list = await _context.DeXuats
                .Include(x => x.MonHoc)
                .Where(x => x.MaSV == maSV)
                .OrderByDescending(x => x.ThoiGian)
                .ToListAsync();

            return View(list);
        }
    }
}
