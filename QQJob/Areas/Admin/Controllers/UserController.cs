using Microsoft.AspNetCore.Mvc;
using QQJob.Areas.Admin.ViewModels;
using QQJob.Repositories.Interfaces;
namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController(ILogger<UserController> logger, IAppUserRepository appUserRepository, ICandidateRepository candidateRepository, IEmployerRepository employerRepository, ICloudinaryService cloudinaryService) : Controller
    {
        private readonly ILogger<UserController> _logger = logger;
        private readonly IAppUserRepository _userRepository = appUserRepository;
        private readonly ICandidateRepository _candidateRepository = candidateRepository;
        private readonly IEmployerRepository _employerRepository = employerRepository;
        private readonly ICloudinaryService _cloudinaryService = cloudinaryService;
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userRepository.GetAllAsync();
            // Populate the ViewModel
            var viewModel = new UsersViewModel
            {
                Users = user.AsQueryable(),
            };
            return View("List", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> VerificationRequestList()
        {
            var user = await _employerRepository.GetAllEmployerAsync();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Evidential(IFormFile file)
        {
            var url = await _cloudinaryService.UploadEvidentAsync(file);
            ViewBag.url = url;
            Console.WriteLine(url);
            return View();
        }
    }
}