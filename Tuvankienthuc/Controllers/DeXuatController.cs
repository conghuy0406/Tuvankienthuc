using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;
using System.Threading.Tasks;

namespace Tuvankienthuc.Controllers
{
    public class DeXuatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DeXuatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DeXuat
        public async Task<IActionResult> Index()
        {
            var deXuats = await _context.DeXuats
                                        .Include(d => d.User)
                                        .Include(d => d.MonHoc)
                                        .ToListAsync();
            return View(deXuats);
        }

        // GET: DeXuat/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var deXuat = await _context.DeXuats
                                       .Include(d => d.User)
                                       .Include(d => d.MonHoc)
                                       .FirstOrDefaultAsync(m => m.MaDX == id);

            if (deXuat == null)
                return NotFound();

            return View(deXuat);
        }

        // GET: DeXuat/Create
        public IActionResult Create()
        {
            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen");
            ViewData["MaMH"] = new SelectList(_context.MonHocs, "MaMH", "TenMH");
            return View();
        }

        // POST: DeXuat/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSV,MaMH,NoiDung,Nguon,Goal")] DeXuat deXuat)
        {
            if (ModelState.IsValid)
            {
                deXuat.ThoiGian = DateTime.Now;
                _context.Add(deXuat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", deXuat.MaSV);
            ViewData["MaMH"] = new SelectList(_context.MonHocs, "MaMH", "TenMH", deXuat.MaMH);
            return View(deXuat);
        }

        // GET: DeXuat/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var deXuat = await _context.DeXuats.FindAsync(id);
            if (deXuat == null)
                return NotFound();

            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", deXuat.MaSV);
            ViewData["MaMH"] = new SelectList(_context.MonHocs, "MaMH", "TenMH", deXuat.MaMH);
            return View(deXuat);
        }

        // POST: DeXuat/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaDX,MaSV,MaMH,NoiDung,Nguon,Goal,ThoiGian")] DeXuat deXuat)
        {
            if (id != deXuat.MaDX)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(deXuat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DeXuatExists(deXuat.MaDX))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", deXuat.MaSV);
            ViewData["MaMH"] = new SelectList(_context.MonHocs, "MaMH", "TenMH", deXuat.MaMH);
            return View(deXuat);
        }

        // GET: DeXuat/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var deXuat = await _context.DeXuats
                                       .Include(d => d.User)
                                       .Include(d => d.MonHoc)
                                       .FirstOrDefaultAsync(m => m.MaDX == id);

            if (deXuat == null)
                return NotFound();

            return View(deXuat);
        }

        // POST: DeXuat/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var deXuat = await _context.DeXuats.FindAsync(id);
            if (deXuat != null)
            {
                _context.DeXuats.Remove(deXuat);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DeXuatExists(int id)
        {
            return _context.DeXuats.Any(e => e.MaDX == id);
        }
    }
}
