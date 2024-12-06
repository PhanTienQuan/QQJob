using Microsoft.AspNetCore.Mvc;
using QQJob.Areas.Admin.ViewModels;
using QQJob.Repositories.Interfaces;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController ( ILogger<UserController> logger, IAppUserRepository appUserRepository ) : Controller
    {
        private readonly ILogger<UserController> _logger = logger;
        private readonly IAppUserRepository _userRepository = appUserRepository;

        public async Task<IActionResult> Index ()
        {
            var user = await _userRepository.GetAllAsync();
            // Populate the ViewModel
            var viewModel = new UsersViewModel
            {
                Users = user.AsQueryable(),
            };
            return View("List", viewModel);
        }
    }
}