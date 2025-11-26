using Microsoft.AspNetCore.Mvc;
using Tuvankienthuc.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using System.Text;

namespace Tuvankienthuc.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ======================================
        // GET: /Auth/Login
        // ======================================
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("UserId") != null)
                return RedirectToAction("Index", "Home");
            return View();
        }

        // ======================================
        // POST: /Auth/Login
        // ======================================
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View();
            }

            string hashed = HashPassword(password);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.MatKhau == hashed);
            if (user == null)
            {
                ViewBag.Error = "Sai email hoặc mật khẩu.";
                return View();
            }

            // ✅ Lưu Session
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.HoTen);
            HttpContext.Session.SetString("Role", user.Role ?? "SinhVien");

            // ✅ Lưu Claims để dùng trong User.Identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "SinhVien")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(4)
                });

            return RedirectToAction("Index", "Home");
        }

        // ======================================
        // GET: /Auth/Register
        // ======================================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ======================================
        // POST: /Auth/Register
        // ======================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string passwordConfirm)
        {
            user.Role ??= "SinhVien";

            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Dữ liệu không hợp lệ.";
                return View(user);
            }

            if (passwordConfirm != user.MatKhau)
            {
                ViewBag.Error = "Mật khẩu nhập lại không khớp.";
                return View(user);
            }

            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            {
                ViewBag.Error = "Email đã tồn tại.";
                return View(user);
            }

            user.MatKhau = HashPassword(user.MatKhau);
            user.CreatedAt = DateTime.Now;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đăng ký thành công!";
            return RedirectToAction("Login");
        }

        // ======================================
        // GET: /Auth/Logout
        // ======================================
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ======================================
        // HÀM BĂM MẬT KHẨU
        // ======================================
        private string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
