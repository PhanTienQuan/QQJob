using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.ViewModels;
using System.Data;
using System.Text.Encodings.Web;

namespace QQJob.Controllers
{
    public class AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ISenderEmail senderEmail, RoleManager<IdentityRole> roleManager) : Controller
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;

        private readonly ISenderEmail _emailSender = senderEmail;

        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Register()
        {
            RegisterViewModel registerModel = new();
            return PartialView("_RegisterModal", registerModel);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.UserName.Trim(),
                    Email = model.Email,
                };
                string roleName = model.AccountType == true ? "Candidate" : "Employer";
                bool roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    // Create the role
                    // We just need to specify a unique role name to create a new role
                    IdentityRole identityRole = new IdentityRole
                    {
                        Name = roleName
                    };
                    // Saves the role in the underlying AspNetRoles table
                    IdentityResult createRoleResult = await _roleManager.CreateAsync(identityRole);
                    if (!createRoleResult.Succeeded)
                    {
                        return Json(new
                        {
                            success = false,
                            errors = createRoleResult.Errors.GroupBy(e => e.Code).ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.Description).ToList()
                            ).Concat(new[] {
                                new KeyValuePair<string, List<string>>("ALL", createRoleResult.Errors.Select(e => e.Description).ToList())
                            })
                            .ToDictionary(x => x.Key, x => x.Value)
                        });
                    }
                }

                // Store user data in AspNetUsers database table
                var result = await _userManager.CreateAsync(user, model.Password);

                // If user is successfully created, sign-in the user using
                // SignInManager and redirect to index action of HomeController
                if (result.Succeeded)
                {
                    if (!await _userManager.IsInRoleAsync(user, roleName))
                    {
                        await _userManager.AddToRoleAsync(user, roleName);
                    }
                    //Then send the Confirmation Email to the User
                    await SendConfirmationEmail(model.Email, user);

                    // If the user is signed in and in the Admin role, then it is
                    // the Admin user that is creating a new user. 
                    // So, redirect the Admin user to ListUsers action of Administration Controller
                    //if (_signInManager.IsSignedIn(User) && User.IsInRole("Admin"))
                    //{

                    //    return RedirectToAction("ListUsers", "Administration");
                    //}

                    //If it is not Admin user, then redirect the user to RegistrationSuccessful View

                    return Json(new { success = true, message = "A confirmation email was send to your mail!!", email = model.Email, password = model.Password });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        errors = result.Errors.GroupBy(e => e.Code).ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.Description).ToList()
                            ).Concat([
                                new KeyValuePair<string, List<string>>("ALL", result.Errors.Select(e => e.Description).ToList())
                            ])
                            .ToDictionary(x => x.Key, x => x.Value)
                    });
                }
            }
            // Return validation errors
            var errors = ModelState
                .Where(x => x.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );

            return Json(new { success = false, errors });
        }

        [HttpGet]
        public IActionResult Login()
        {
            return PartialView("_LoginModal");
        }

        [NonAction]
        private async Task SendConfirmationEmail(string? email, AppUser? user)
        {
            //Generate the Token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            //Build the Email Confirmation Link which must include the Callback URL
            var ConfirmationLink = Url.Action("ConfirmEmail", "Account", new { UserId = user.Id, Token = token }, protocol: HttpContext.Request.Scheme);

            //Send the Confirmation Email to the User Email Id
            await _emailSender.SendEmailAsync(email, "Confirm Your Email", $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(ConfirmationLink)}'>clicking here</a>.", true);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string UserId, string Token)
        {
            Console.WriteLine("Hello this is confirm email");
            if (UserId == null || Token == null)
            {
                ViewBag.Message = "The link is Invalid or Expired";
            }

            //Find the User By Id
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
            {
                ViewBag.ErrorMessage = $"The User ID {UserId} is Invalid";
                return View("NotFound");
            }

            if (await _userManager.IsInRoleAsync(user, "Candidate"))
            {
                ViewBag.Message = "Hello candidate";
                await _userManager.ConfirmEmailAsync(user, Token);
                return View("~/Views/Account/CandidateConfirmEmail.cshtml");
            }
            else if (await _userManager.IsInRoleAsync(user, "Employer"))
            {
                ViewBag.Message = "Heloo employer";
                await _userManager.ConfirmEmailAsync(user, Token);
                return View("~/Views/Account/EmployerConfirmEmail.cshtml");
            }
            else
            {
                ViewBag.Message = "oppps";
                return View("EmployerConfirmEmail");
            }
        }
    }
}
