using Microsoft.AspNetCore.Mvc;

namespace QQJob.Controllers
{
    public class EmployerController : Controller
    {
        public IActionResult Index ()
        {
            return View();
        }
        public IActionResult EmployerProfile ()
        {
            return View();
        }
        public IActionResult JobsPosted ()
        {
            return View();
        }
        public IActionResult PostJob ()
        {
            return View();
        }
    }
}
