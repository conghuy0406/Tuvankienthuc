using Microsoft.AspNetCore.Mvc;
using Tuvankienthuc.Filters;

[RoleAuthorize("Admin")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
