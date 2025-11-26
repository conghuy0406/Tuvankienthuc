using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.ViewModels;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    public class MonHocController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MonHocController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MonHoc
        public async Task<IActionResult> Index()
        {
            // Tối ưu: Nếu không có dữ liệu, danh sách sẽ trống chứ không bị lỗi.
            return View(await _context.MonHocs.ToListAsync());
        }

        // GET: MonHoc/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Tối ưu: Dùng FindAsync() nhanh hơn đối với khóa chính.
            var monHoc = await _context.MonHocs.FindAsync(id);

            if (monHoc == null)
            {
                return NotFound();
            }

            return View(monHoc);
        }

        // GET: MonHoc/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MonHoc/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TenMH,MoTa")] MonHoc monHoc)
        {
            // Tối ưu: Chỉ cho phép binding các thuộc tính cụ thể để bảo mật (Overposting Attack).
            if (ModelState.IsValid)
            {
                _context.Add(monHoc);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(monHoc);
        }

        // GET: MonHoc/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var monHoc = await _context.MonHocs.FindAsync(id);
            if (monHoc == null)
            {
                return NotFound();
            }
            return View(monHoc);
        }

        // POST: MonHoc/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaMH,TenMH,MoTa")] MonHoc monHoc)
        {
            if (id != monHoc.MaMH)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(monHoc);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                  
                }
                return RedirectToAction(nameof(Index));
            }
            return View(monHoc);
        }

        // GET: MonHoc/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var monHoc = await _context.MonHocs.FirstOrDefaultAsync(m => m.MaMH == id);
            if (monHoc == null)
            {
                return NotFound();
            }

            return View(monHoc);
        }

        // POST: MonHoc/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var monHoc = await _context.MonHocs.FindAsync(id);
            if (monHoc != null)
            {
                _context.MonHocs.Remove(monHoc);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }



    }
}