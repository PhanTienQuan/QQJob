using Microsoft.AspNetCore.Mvc;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController(ILogger<UserController> logger) : Controller
    {
        private readonly ILogger<UserController> _logger = logger;

        public IActionResult Index()
        {
            return View("List");
        }
    }
}
