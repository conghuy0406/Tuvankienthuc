using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.ViewModels;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    public class KienThucController : Controller
    {
        private readonly ApplicationDbContext _context;

        public KienThucController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: KienThuc
        public async Task<IActionResult> Index()
        {
            var kienThucs = await _context.KienThucs.Include(k => k.ChuDe).ToListAsync();
            return View(kienThucs);
        }

        // GET: KienThuc/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kienThuc = await _context.KienThucs
                .Include(k => k.ChuDe)
                .FirstOrDefaultAsync(m => m.MaKT == id);

            if (kienThuc == null)
            {
                return NotFound();
            }

            return View(kienThuc);
        }

        // GET: KienThuc/Create
        public IActionResult Create()
        {
            ViewData["MaCD"] = new SelectList(_context.ChuDes, "MaCD", "TenCD");
            return View();
        }

        // POST: KienThuc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaCD,NoiDung,DoKho")] KienThuc kienThuc)
        {
            if (ModelState.IsValid)
            {
                _context.Add(kienThuc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaCD"] = new SelectList(_context.ChuDes, "MaCD", "TenCD", kienThuc.MaCD);
            return View(kienThuc);
        }

        // GET: KienThuc/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kienThuc = await _context.KienThucs.FindAsync(id);
            if (kienThuc == null)
            {
                return NotFound();
            }

            ViewData["MaCD"] = new SelectList(_context.ChuDes, "MaCD", "TenCD", kienThuc.MaCD);
            return View(kienThuc);
        }

        // POST: KienThuc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaKT,MaCD,NoiDung,DoKho")] KienThuc kienThuc)
        {
            if (id != kienThuc.MaKT)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(kienThuc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KienThucExists(kienThuc.MaKT))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaCD"] = new SelectList(_context.ChuDes, "MaCD", "TenCD", kienThuc.MaCD);
            return View(kienThuc);
        }

        // GET: KienThuc/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var kienThuc = await _context.KienThucs
                .Include(k => k.ChuDe)
                .FirstOrDefaultAsync(m => m.MaKT == id);

            if (kienThuc == null)
            {
                return NotFound();
            }

            return View(kienThuc);
        }

        // POST: KienThuc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var kienThuc = await _context.KienThucs.FindAsync(id);
            if (kienThuc != null)
            {
                _context.KienThucs.Remove(kienThuc);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool KienThucExists(int id)
        {
            return _context.KienThucs.Any(e => e.MaKT == id);
        }
    }
}