using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Controllers
{
    public class ChatLogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatLogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ChatLogs
        public async Task<IActionResult> Index()
        {
            var logs = _context.ChatLogs
                               .Include(c => c.User)
                               .Include(c => c.DeXuat)
                               .OrderByDescending(c => c.ThoiGian);
            return View(await logs.ToListAsync());
        }

        // GET: ChatLogs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var chat = await _context.ChatLogs
                                     .Include(c => c.User)
                                     .Include(c => c.DeXuat)
                                     .FirstOrDefaultAsync(m => m.Id == id);
            if (chat == null)
                return NotFound();

            return View(chat);
        }

        // GET: ChatLogs/Create
        public IActionResult Create()
        {
            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen");
            ViewData["MaDX"] = new SelectList(_context.DeXuats, "MaDX", "NoiDung");
            return View();
        }

        // POST: ChatLogs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSV,MaDX,NoiDung,TraLoi,ThoiGian")] ChatLog chat)
        {
            if (ModelState.IsValid)
            {
                chat.ThoiGian = DateTime.Now;
                _context.Add(chat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", chat.MaSV);
            ViewData["MaDX"] = new SelectList(_context.DeXuats, "MaDX", "NoiDung", chat.MaDX);
            return View(chat);
        }

        // GET: ChatLogs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var chat = await _context.ChatLogs.FindAsync(id);
            if (chat == null)
                return NotFound();

            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", chat.MaSV);
            ViewData["MaDX"] = new SelectList(_context.DeXuats, "MaDX", "NoiDung", chat.MaDX);
            return View(chat);
        }

        // POST: ChatLogs/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MaSV,MaDX,NoiDung,TraLoi,ThoiGian")] ChatLog chat)
        {
            if (id != chat.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(chat);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ChatLogs.Any(e => e.Id == chat.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["MaSV"] = new SelectList(_context.Users, "Id", "HoTen", chat.MaSV);
            ViewData["MaDX"] = new SelectList(_context.DeXuats, "MaDX", "NoiDung", chat.MaDX);
            return View(chat);
        }

        // GET: ChatLogs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var chat = await _context.ChatLogs
                                     .Include(c => c.User)
                                     .Include(c => c.DeXuat)
                                     .FirstOrDefaultAsync(m => m.Id == id);
            if (chat == null)
                return NotFound();

            return View(chat);
        }

        // POST: ChatLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var chat = await _context.ChatLogs.FindAsync(id);
            if (chat != null)
            {
                _context.ChatLogs.Remove(chat);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
