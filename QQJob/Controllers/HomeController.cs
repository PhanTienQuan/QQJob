using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QQJob.Models;

namespace QQJob.Controllers
{
    public class HomeController(UserManager<AppUser> userManager,SignInManager<AppUser> signInManager):Controller
    {

        public async Task<IActionResult> Index()
        {
            if(User.Identity != null && User.Identity.IsAuthenticated)
            {
                if((await userManager.FindByNameAsync(User.Identity.Name)) == null)
                {
                    return RedirectToAction("Logout",new { controller = "Account" });
                }
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

    }
}
