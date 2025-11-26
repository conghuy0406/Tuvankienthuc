using Microsoft.AspNetCore.Mvc;
using Tuvankienthuc.Filters;

[RoleAuthorize("GiangVien", "Admin")]
public class GiangVienController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
