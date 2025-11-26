using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;
using Tuvankienthuc.ViewModels;


namespace Tuvankienthuc.Controllers
{
    public class TaiLieuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaiLieuController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: TaiLieu
        public async Task<IActionResult> Index()
        {
            // Nếu View Index hiện tại bind List<TaiLieu> thì giữ nguyên:
            var list = await _context.TaiLieus.OrderByDescending(x => x.NgayThem).ToListAsync();
            return View(list);
        }

        // GET: TaiLieu/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var tl = await _context.TaiLieus.FirstOrDefaultAsync(m => m.MaTL == id);
            if (tl == null) return NotFound();

            // Lấy danh sách chủ đề đã gắn để hiển thị
            var chuDeNames = await (from map in _context.TaiLieuChuDes
                                    join cd in _context.ChuDes on map.MaCD equals cd.MaCD
                                    where map.MaTL == tl.MaTL
                                    select cd.TenCD).ToListAsync();
            ViewBag.ChuDeNames = chuDeNames;

            return View(tl);
        }

        // GET: TaiLieu/Create
        public async Task<IActionResult> Create()
        {
            await BuildChuDeSelectList(); // danh sách chủ đề để chọn (multi-select)
            return View(new TaiLieuUpsertVM());
        }

        // POST: TaiLieu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaiLieuUpsertVM model)
        {
            if (!ModelState.IsValid)
            {
                await BuildChuDeSelectList(model.SelectedMaCDs);
                return View(model);
            }

            _context.TaiLieus.Add(model.TaiLieu);
            await _context.SaveChangesAsync();

            // Gắn map chủ đề
            if (model.SelectedMaCDs?.Any() == true)
            {
                foreach (var maCD in model.SelectedMaCDs.Distinct())
                {
                    _context.TaiLieuChuDes.Add(new TaiLieuChuDe { MaTL = model.TaiLieu.MaTL, MaCD = maCD });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: TaiLieu/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tl = await _context.TaiLieus.FindAsync(id);
            if (tl == null) return NotFound();

            // Lấy các MaCD hiện tại của tài liệu
            var selected = await _context.TaiLieuChuDes
                                         .Where(x => x.MaTL == tl.MaTL)
                                         .Select(x => x.MaCD)
                                         .ToListAsync();

            await BuildChuDeSelectList(selected);

            return View(new TaiLieuUpsertVM
            {
                TaiLieu = tl,
                SelectedMaCDs = selected
            });
        }

        // POST: TaiLieu/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaiLieuUpsertVM model)
        {
            if (id != model.TaiLieu.MaTL) return NotFound();

            if (!ModelState.IsValid)
            {
                await BuildChuDeSelectList(model.SelectedMaCDs);
                return View(model);
            }

            try
            {
                // Cập nhật thông tin tài liệu
                _context.Update(model.TaiLieu);
                await _context.SaveChangesAsync();

                // Đồng bộ map chủ đề
                var existing = await _context.TaiLieuChuDes
                                             .Where(x => x.MaTL == id)
                                             .ToListAsync();

                var existingMaCD = existing.Select(x => x.MaCD).ToHashSet();
                var newMaCD = (model.SelectedMaCDs ?? new List<int>()).ToHashSet();

                // Xoá những map không còn được chọn
                var toRemove = existing.Where(x => !newMaCD.Contains(x.MaCD)).ToList();
                if (toRemove.Count > 0)
                    _context.TaiLieuChuDes.RemoveRange(toRemove);

                // Thêm những map mới
                var toAddIds = newMaCD.Except(existingMaCD);
                foreach (var maCD in toAddIds)
                    _context.TaiLieuChuDes.Add(new TaiLieuChuDe { MaTL = id, MaCD = maCD });

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaiLieuExists(model.TaiLieu.MaTL)) return NotFound();
                else throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: TaiLieu/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var tl = await _context.TaiLieus.FirstOrDefaultAsync(m => m.MaTL == id);
            if (tl == null) return NotFound();

            // (tuỳ chọn) lấy tên chủ đề đã gắn để hiển thị ở view xác nhận xoá
            var chuDeNames = await (from map in _context.TaiLieuChuDes
                                    join cd in _context.ChuDes on map.MaCD equals cd.MaCD
                                    where map.MaTL == tl.MaTL
                                    select cd.TenCD).ToListAsync();
            ViewBag.ChuDeNames = chuDeNames;

            return View(tl);
        }

        // POST: TaiLieu/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tl = await _context.TaiLieus.FindAsync(id);
            if (tl != null)
            {
                // Map TaiLieuChuDe đã cấu hình OnDelete.Cascade ở phía MaTL,
                // nên xoá tài liệu sẽ tự xoá map.
                _context.TaiLieus.Remove(tl);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool TaiLieuExists(int id) => _context.TaiLieus.Any(e => e.MaTL == id);

        // Helpers
        private async Task BuildChuDeSelectList(IEnumerable<int>? selected = null)
        {
            // Hiển thị tên Chủ đề kèm tên Môn học cho dễ chọn
            var data = await _context.ChuDes
                .Include(cd => cd.MonHoc)
                .Select(cd => new
                {
                    cd.MaCD,
                    Ten = cd.MonHoc.TenMH + " - " + cd.TenCD
                })
                .OrderBy(x => x.Ten)
                .ToListAsync();

            ViewBag.ChuDeList = new MultiSelectList(data, "MaCD", "Ten", selected);
        }
    }
}
