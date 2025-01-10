using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController(ILogger<UserController> logger,IAppUserRepository appUserRepository,ICandidateRepository candidateRepository,IEmployerRepository employerRepository,ICloudinaryService cloudinaryService,UserManager<AppUser> userManager):Controller
    {
        private readonly ILogger<UserController> _logger = logger;
        private readonly IAppUserRepository _userRepository = appUserRepository;
        private readonly ICandidateRepository _candidateRepository = candidateRepository;
        private readonly IEmployerRepository _employerRepository = employerRepository;
        private readonly ICloudinaryService _cloudinaryService = cloudinaryService;
        private readonly UserManager<AppUser> _userManager = userManager;
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userRepository.GetUsersAsync();
            var count = await _userRepository.GetCount();
            ViewBag.Count = count;
            return View("List",user);
        }

        [HttpGet]
        public async Task<IActionResult> VerificationRequestList()
        {
            var user = await _employerRepository.GetAllRQEmployerAsync();
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

        [HttpPost]
        public async Task<IActionResult> Delete(string UserId)
        {
            //First Fetch the User you want to Delete
            var user = await userManager.FindByIdAsync(UserId);
            if(user == null)
            {
                // Handle the case where the user wasn't found
                ViewBag.ErrorMessage = $"User with Id = {UserId} cannot be found";
                return View("NotFound");
            }
            else
            {
                //Delete the User Using DeleteAsync Method of UserManager Service
                var result = await _userManager.DeleteAsync(user);
                if(result.Succeeded)
                {
                    TempData["Message"] = "Delete Successful!";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Handle failure
                    foreach(var error in result.Errors)
                    {
                        ModelState.AddModelError("",error.Description);
                    }
                }
                return View("Index");
            }
        }
    }
}