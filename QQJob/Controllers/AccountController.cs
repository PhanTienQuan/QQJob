using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace QQJob.Controllers
{
    public class AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ISenderEmail senderEmail, RoleManager<IdentityRole> roleManager) : Controller
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly ISenderEmail _emailSender = senderEmail;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return PartialView("_RegisterModal", new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    CreatedAt = DateTime.UtcNow,
                };

                string roleName = model.AccountType == true ? "Candidate" : "Employer";
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    IdentityRole identityRole = new IdentityRole
                    {
                        Name = roleName
                    };

                    // Saves the role in the underlying AspNetRoles table
                    await _roleManager.CreateAsync(identityRole);
                }
                if (await _userManager.FindByEmailAsync(model.Email) != null)
                {
                    roleName = "Candidate";
                    user.Candidate = new Candidate();
                }
                else
                {
                    roleName = "Employer";
                    user.Employer = new Employer();
                }

                if(await _userManager.FindByEmailAsync(model.Email) != null)
                {
                    ModelState.AddModelError("Email", "This email is already in use.");
                    return Json(new
                    {
                        success = false,
                        errors = GetModelStateErrors()
                    });
                }

                // Store user data in AspNetUsers database table
                var result = await _userManager.CreateAsync(user, model.Password);

                if(result.Succeeded)
                {
                    if(!await _userManager.IsInRoleAsync(user, roleName))
                    {
                        await _userManager.AddToRoleAsync(user, roleName);
                    }
                    //Then send the Confirmation Email to the User
                    await SendConfirmationEmail(model.Email, user);

                    return Json(new { success = true, message = "A confirmation email was send to your mail!!" });
                }
                else
                {
                    ModelState.AddModelError("ALL", "Something went wrong went create your account");
                    return Json(new
                    {
                        success = false,
                        errors = GetModelStateErrors()
                    });
                }
            }

            return Json(new
            {
                success = false,
                errors = GetModelStateErrors()
            });
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
            if(UserId == null || Token == null)
            {
                ViewBag.Message = "The link is Invalid or Expired";
            }

            //Find the User By Id
            var user = await _userManager.FindByIdAsync(UserId);
            if(user == null)
            {
                ViewBag.ErrorMessage = $"The User ID {UserId} is Invalid";
                return View("NotFound");
            }

            if(await _userManager.IsInRoleAsync(user, "Candidate"))
            {
                ViewBag.Message = "Hello candidate";
                await _userManager.ConfirmEmailAsync(user, Token);
                return View("~/Views/Account/CandidateConfirmEmail.cshtml");
            }
            else if(await _userManager.IsInRoleAsync(user, "Employer"))
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

        //Login

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return PartialView("_LoginModal", new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if(user == null)
                {
                    // User not found
                    ModelState.AddModelError("All", "User does not exist.");
                    return Json(new
                    {
                        success = false,
                        errors = GetModelStateErrors()
                    });
                }

                if(!await _userManager.IsEmailConfirmedAsync(user))
                {
                    // Email not confirmed
                    ModelState.AddModelError("All", "Email is not confirmed.");
                    return Json(new
                    {
                        success = false,
                        errors = GetModelStateErrors()
                    });
                }
                var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

                if(result.Succeeded)
                {
                    return Json(new { success = true, url = Url.Action("index", "home") });
                }
                if(result.RequiresTwoFactor)
                {
                    // Handle two-factor authentication case
                }
                if(result.IsLockedOut)
                {
                    // Handle lockout scenario
                }
                else if(result.IsNotAllowed)
                {
                    ModelState.AddModelError("Password", "Sign-in is not allowed.");
                }
                else
                {
                    // Handle failure
                    ModelState.AddModelError("Password", "Wrong password");
                }
                Console.WriteLine($"Login result: {result}");
            }

            return Json(new
            {
                success = false,
                errors = GetModelStateErrors()
            });
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("index", "home");
        }



        [NonAction]
        private Dictionary<string, string[]> GetModelStateErrors()
        {
            return ModelState
                .Where(x => x.Value.Errors.Count > 0) // Only select fields with errors
                .ToDictionary(
                    kvp => kvp.Key, // Field name
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray() // Error messages for the field
                );
        }


        [NonAction]
        private string GenerateSlug(string input)
        {
            if(string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Normalize the string to handle Unicode characters
            string normalized = input.Normalize(NormalizationForm.FormD);

            // Remove diacritical marks (e.g., accents)
            StringBuilder stringBuilder = new StringBuilder();
            foreach(char c in normalized)
            {
                if(CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            string cleaned = stringBuilder.ToString().Normalize(NormalizationForm.FormC);

            // Convert to lowercase and remove invalid characters
            cleaned = cleaned.ToLowerInvariant();
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-]", ""); // Remove invalid characters
            cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();  // Replace multiple spaces with a single space

            // Replace spaces with hyphens
            return Regex.Replace(cleaned, @"\s", "-");
        }
    }
}
