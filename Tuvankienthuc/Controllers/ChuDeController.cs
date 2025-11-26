using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    public class ChuDeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChuDeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ChuDe
        public async Task<IActionResult> Index()
        {
            var chuDes = await _context.ChuDes.Include(c => c.MonHoc).ToListAsync();
            return View(chuDes);
        }

        // GET: ChuDe/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chuDe = await _context.ChuDes
                .Include(c => c.MonHoc)
                .FirstOrDefaultAsync(m => m.MaCD == id);

            if (chuDe == null)
            {
                return NotFound();
            }

            return View(chuDe);
        }

        // GET: ChuDe/Create
        public IActionResult Create()
        {
            ViewData["MaMH"] = new SelectList(_context.MonHocs, "MaMH", "TenMH");
            return View();
        }

        // POST: ChuDe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaMH,TenCD,MoTa,IsKienThucCoBan")] ChuDe chuDe)
        {
            if (ModelState.IsValid)
            {
                _context.Add(chuDe);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaMH"] = new SelectList(_context.MonHocs, "MaMH", "TenMH", chuDe.MaMH);
            return View(chuDe);
        }

        // GET: ChuDe/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chuDe = await _context.ChuDes.FindAsync(id);
            if (chuDe == null)
            {
                return NotFound();
            }

            ViewData["MaMH"] = new SelectList(_context.MonHocs, "MaMH", "TenMH", chuDe.MaMH);
            return View(chuDe);
        }

        // POST: ChuDe/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaCD,MaMH,TenCD,MoTa,IsKienThucCoBan")] ChuDe chuDe)
        {
            if (id != chuDe.MaCD)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chuDe);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ChuDeExists(chuDe.MaCD))
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
            ViewData["MaMH"] = new SelectList(_context.MonHocs, "MaMH", "TenMH", chuDe.MaMH);
            return View(chuDe);
        }

        // GET: ChuDe/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chuDe = await _context.ChuDes
                .Include(c => c.MonHoc)
                .FirstOrDefaultAsync(m => m.MaCD == id);

            if (chuDe == null)
            {
                return NotFound();
            }

            return View(chuDe);
        }

        // POST: ChuDe/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chuDe = await _context.ChuDes.FindAsync(id);
            if (chuDe != null)
            {
                _context.ChuDes.Remove(chuDe);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }



        private bool ChuDeExists(int id)
        {
            return _context.ChuDes.Any(e => e.MaCD == id);
        }
    }
}