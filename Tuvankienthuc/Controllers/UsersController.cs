using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Filters;
using Tuvankienthuc.Models;
using System.Security.Cryptography;
using System.Text;

namespace Tuvankienthuc.Controllers
{
    [RoleAuthorize("Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===============================
        // DANH SÁCH (KHÔNG HIỆN USER BỊ XÓA MỀM)
        // ===============================
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.Role)
                .ToListAsync();

            return View(users);
        }

        // ===============================
        // TẠO USER
        // ===============================
        [HttpGet]
        public IActionResult Create() => View(new User());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (!ModelState.IsValid)
                return View(user);

            if (await _context.Users.AnyAsync(x => x.Email == user.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại");
                return View(user);
            }

            user.MatKhau = HashPassword(user.MatKhau);
            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction("Index");
        }

        // ===============================
        // SỬA USER
        // ===============================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User form)
        {
            if (!ModelState.IsValid)
                return View(form);

            var user = await _context.Users.FindAsync(form.Id);
            if (user == null) return NotFound();

            user.HoTen = form.HoTen;
            user.Email = form.Email;
            user.Role = form.Role;
            user.IsActive = form.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Index");
        }

        // ===============================
        // XÓA MỀM (SOFT DELETE)
        // ===============================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsDeleted = true;       // Không xoá DB
            user.IsActive = false;       // Khoá tài khoản
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xoá tài khoản (soft delete).";
            return RedirectToAction("Index");
        }

        // ===============================
        // HÀM HASH PASSWORD
        // ===============================
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
