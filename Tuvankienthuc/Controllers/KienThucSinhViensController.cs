using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    public class KienThucSinhViensController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KienThucSinhViensController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: KienThucSinhViens
        public async Task<IActionResult> Index()
        {
            var data = _context.KienThucSinhViens
                               .Include(k => k.user)
                               .Include(k => k.KienThuc);
            return View(await data.ToListAsync());
        }

        // GET: KienThucSinhViens/Details
        public async Task<IActionResult> Details(int? MaSV, int? MaKT)
        {
            if (MaSV == null || MaKT == null) return NotFound();

            var ktsv = await _context.KienThucSinhViens
                        .Include(k => k.user)
                        .Include(k => k.KienThuc)
                        .FirstOrDefaultAsync(m => m.MaSV == MaSV && m.MaKT == MaKT);

            if (ktsv == null) return NotFound();

            return View(ktsv);
        }

        // GET: KienThucSinhViens/Create
        public IActionResult Create()
        {
            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen");
            ViewData["MaKT"] = new SelectList(_context.KienThucs, "MaKT", "NoiDung");
            return View();
        }

        // POST: KienThucSinhViens/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSV,MaKT,TrangThai,LanHocCuoi")] KienThucSinhVien ktsv)
        {
            if (ModelState.IsValid)
            {
                ktsv.LanHocCuoi = DateTime.Now;
                _context.Add(ktsv);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", ktsv.MaSV);
            ViewData["MaKT"] = new SelectList(_context.KienThucs, "MaKT", "NoiDung", ktsv.MaKT);
            return View(ktsv);
        }

        // GET: KienThucSinhViens/Edit
        public async Task<IActionResult> Edit(int? MaSV, int? MaKT)
        {
            if (MaSV == null || MaKT == null) return NotFound();

            var ktsv = await _context.KienThucSinhViens.FindAsync(MaSV, MaKT);
            if (ktsv == null) return NotFound();

            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", ktsv.MaSV);
            ViewData["MaKT"] = new SelectList(_context.KienThucs, "MaKT", "NoiDung", ktsv.MaKT);
            return View(ktsv);
        }

        // POST: KienThucSinhViens/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int MaSV, int MaKT, [Bind("MaSV,MaKT,TrangThai,LanHocCuoi")] KienThucSinhVien ktsv)
        {
            if (MaSV != ktsv.MaSV || MaKT != ktsv.MaKT) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    ktsv.LanHocCuoi = DateTime.Now; // cập nhật thời gian gần nhất
                    _context.Update(ktsv);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.KienThucSinhViens.Any(e => e.MaSV == MaSV && e.MaKT == MaKT))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", ktsv.MaSV);
            ViewData["MaKT"] = new SelectList(_context.KienThucs, "MaKT", "NoiDung", ktsv.MaKT);
            return View(ktsv);
        }

        // GET: KienThucSinhViens/Delete
        public async Task<IActionResult> Delete(int? MaSV, int? MaKT)
        {
            if (MaSV == null || MaKT == null) return NotFound();

            var ktsv = await _context.KienThucSinhViens
                        .Include(k => k.user)
                        .Include(k => k.KienThuc)
                        .FirstOrDefaultAsync(m => m.MaSV == MaSV && m.MaKT == MaKT);

            if (ktsv == null) return NotFound();

            return View(ktsv);
        }

        // POST: KienThucSinhViens/DeleteConfirmed
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int MaSV, int MaKT)
        {
            var ktsv = await _context.KienThucSinhViens.FindAsync(MaSV, MaKT);
            if (ktsv != null)
            {
                _context.KienThucSinhViens.Remove(ktsv);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
