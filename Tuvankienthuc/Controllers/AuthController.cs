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

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Sai email hoặc mật khẩu.";
                return View();
            }

            // 🟡 USER CŨ -> CHƯA CÓ SALT
            if (string.IsNullOrEmpty(user.Salt))
            {
                // Hash kiểu cũ (không salt)
                string oldHash = HashPassword_NoSalt(password);

                if (oldHash == user.MatKhau)
                {
                    // Tự động nâng cấp lên cơ chế có salt
                    user.Salt = GenerateSalt(user.Email);
                    user.MatKhau = HashPassword(password, user.Salt);
                    user.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    ViewBag.Error = "Sai email hoặc mật khẩu.";
                    return View();
                }
            }
            else
            {
                // 🟢 USER MỚI -> CÓ SALT
                string hashed = HashPassword(password, user.Salt);

                if (hashed != user.MatKhau)
                {
                    ViewBag.Error = "Sai email hoặc mật khẩu.";
                    return View();
                }
            }

            // LOGIN THÀNH CÔNG -> LƯU SESSION
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.HoTen);
            HttpContext.Session.SetString("Role", user.Role);

            // LOGIN THÀNH CÔNG -> COOKIE AUTH
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity)
            );

            return RedirectToAction("Index", "Home");
        }

        // ======================================
        // REGISTER
        // ======================================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string passwordConfirm)
        {
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

            user.Salt = GenerateSalt(user.Email);
            user.MatKhau = HashPassword(user.MatKhau, user.Salt);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đăng ký thành công!";
            return RedirectToAction("Login");
        }

        // ======================================
        // RESET PASSWORD
        // ======================================
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            string newPass = "123456";
            user.Salt = GenerateSalt(user.Email);
            user.MatKhau = HashPassword(newPass, user.Salt);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Reset mật khẩu thành công!";
            return RedirectToAction("Index", "User");
        }

        // ======================================
        // LOGOUT
        // ======================================
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // ======================================
        // HASH CÓ SALT
        // ======================================
        private string HashPassword(string password, string salt)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt));
            return Convert.ToBase64String(bytes);
        }

        // HASH KHÔNG SALT (cũ)
        private string HashPassword_NoSalt(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        // TẠO SALT
        private string GenerateSalt(string email)
        {
            string raw = email + Guid.NewGuid().ToString();
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }
    }
}
