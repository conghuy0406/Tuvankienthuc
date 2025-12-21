using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Filters;

[RoleAuthorize("Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================
    // DASHBOARD
    // =========================
    public async Task<IActionResult> Index()
    {
        ViewBag.SoBaoCaoMoi = await _context.BaoCaoVanDes
            .CountAsync(x => !x.IsRead);

        return View();
    }

    // =========================
    // DANH SÁCH BÁO CÁO
    // =========================
    public async Task<IActionResult> BaoCaoVanDe()
    {
        var list = await _context.BaoCaoVanDes
            .Include(x => x.User)
            .Include(x => x.DeXuat)
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync();

        return View(list);
    }

    // =========================
    // ĐÁNH DẤU ĐÃ XỬ LÝ
    // =========================
    public async Task<IActionResult> Resolve(int id)
    {
        var bc = await _context.BaoCaoVanDes.FindAsync(id);
        if (bc != null)
        {
            bc.IsResolved = true;
            bc.IsRead = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("BaoCaoVanDe");
    }
}


