using Microsoft.AspNetCore.Mvc;

namespace QQJob.Controllers
{
    public class MessageController:Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
