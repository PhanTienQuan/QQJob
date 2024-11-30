using Microsoft.AspNetCore.Mvc;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController(ILogger<HomeController> logger) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;

        public IActionResult Index()
        {
            return View();
        }
    }
}
