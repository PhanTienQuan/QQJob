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
    public class AccountController(UserManager<AppUser> userManager,SignInManager<AppUser> signInManager,ISenderEmail senderEmail,RoleManager<IdentityRole> roleManager):Controller
    {
        private readonly UserManager<AppUser> _userManager = userManager;
        private readonly SignInManager<AppUser> _signInManager = signInManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly ISenderEmail _emailSender = senderEmail;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return PartialView("_RegisterModal",new RegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return Json(new { success = false,errors = GetModelStateErrors() });
            }

            if(await _userManager.FindByEmailAsync(model.Email) != null)
            {
                return Json(new { success = false,errors = new Dictionary<string,string[]> { { "Email",new[] { "This email is already in use." } } } });
            }

            var user = new AppUser
            {
                FullName = model.Fullname,
                UserName = model.Email,
                Email = model.Email,
                CreatedAt = DateTime.UtcNow,
                Candidate = model.AccountType == true ? new Candidate() : null,
                Employer = model.AccountType == false ? new Employer() : null
            };

            string roleName = model.AccountType == true ? "Candidate" : "Employer";
            if(!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole { Name = roleName });
            }

            var result = await _userManager.CreateAsync(user,model.Password);
            if(!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            if(!await _userManager.IsInRoleAsync(user,roleName))
            {
                await _userManager.AddToRoleAsync(user,roleName);
            }
            var resendLink = Url.Action("ResendConfirmationEmail","Account");
            var resendMessage = $@"A verification email was send to your email. Didn't receive an email? <a href=""{resendLink}"">Click here</a>";
            await SendConfirmationEmail(model.Email,user);
            return Json(new { success = true,message = resendMessage,email = model.Email,password = model.Password });
        }


        [NonAction]
        private async Task SendConfirmationEmail(string? email,AppUser? user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail","Account",new { UserId = user.Id,Token = token },protocol: HttpContext.Request.Scheme);

            var safeLink = HtmlEncoder.Default.Encode(confirmationLink);

            var subject = "Welcome to QQJob jobfinding platform! Please Confirm Your Email";

            var messageBody = $@"
                <div style=""font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:1.6;color:#333;"">
                    <p>Hi {user.FullName},</p>
                    <p>Thank you for creating an account at <strong>QQJob</strong>.
                    To start enjoying all of our features, please confirm your email address by clicking the button below:</p>
                    <p>
                        <a href=""{safeLink}"" 
                            style=""background-color:#007bff;color:#fff;padding:10px 20px;text-decoration:none;
                                    font-weight:bold;border-radius:5px;display:inline-block;"">
                            Confirm Email
                        </a>
                    </p>
                    <p>If the button doesn’t work for you, copy and paste the following URL into your browser:
                        <br />
                        <a href=""{safeLink}"" style=""color:#007bff;text-decoration:none;"">{safeLink}</a>
                    </p>
                    <p>If you did not sign up for this account, please ignore this email.</p>
                    <p>Thanks,<br />
                    The QQJob Team</p>
                </div>
            ";
            //Send the Confirmation Email to the User Email Id
            await _emailSender.SendEmailAsync(email,subject,messageBody,true);
            //Build the Email Confirmation Link which must include the Callback URL
            var ConfirmationLink = Url.Action("ConfirmEmail","Account",new { UserId = user.Id,Token = token },protocol: HttpContext.Request.Scheme);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string UserId,string Token)
        {
            if(string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(Token))
            {
                ViewBag.ErrorMessage = "The link is invalid or has expired. Please request a new one if needed.";
                return View();
            }

            var user = await userManager.FindByIdAsync(UserId);
            if(user == null)
            {
                ViewBag.ErrorMessage = "We could not find a user associated with the given link.";
                return View();
            }

            var result = await userManager.ConfirmEmailAsync(user,Token);
            if(result.Succeeded)
            {
                ViewBag.Message = "Thank you for confirming your email address. Your account is now verified!";
                return View();
            }

            ViewBag.ErrorMessage = "We were unable to confirm your email address. Please try again or request a new link.";
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendConfirmationEmail(bool IsResend = true)
        {
            if(IsResend)
            {
                ViewBag.Message = "Resend Confirmation Email";
            }
            else
            {
                ViewBag.Message = "Send Confirmation Email";
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmationEmail(string Email)
        {
            ViewBag.SuccessMessage = "A confirmation email is sended to your email!";
            ViewBag.Message = "Send Confirmation Email";
            var user = await userManager.FindByEmailAsync(Email);
            if(user == null || await userManager.IsEmailConfirmedAsync(user))
            {
                return View();
            }

            await SendConfirmationEmail(Email,user);
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return PartialView("_LoginModal",new LoginViewModel());
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
                    ModelState.AddModelError("All","User does not exist.");
                    return Json(new
                    {
                        success = false,
                        errors = GetModelStateErrors()
                    });
                }

                if(!await _userManager.IsEmailConfirmedAsync(user))
                {
                    // Email not confirmed
                    ModelState.AddModelError("All","Email is not confirmed.");
                    return Json(new
                    {
                        success = false,
                        errors = GetModelStateErrors()
                    });
                }

                var result = await _signInManager.PasswordSignInAsync(user.UserName,model.Password,model.RememberMe,lockoutOnFailure: false);

                if(result.Succeeded)
                {
                    return Json(new { success = true,url = Url.Action("index","home") });
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
                    ModelState.AddModelError("Password","Sign-in is not allowed.");
                }
                else
                {
                    // Handle failure
                    ModelState.AddModelError("All","Wrong password");
                }
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
            return RedirectToAction("index","home");
        }

        [NonAction]
        private Dictionary<string,string[]> GetModelStateErrors()
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
            cleaned = Regex.Replace(cleaned,@"[^a-z0-9\s-]",""); // Remove invalid characters
            cleaned = Regex.Replace(cleaned,@"\s+"," ").Trim();  // Replace multiple spaces with a single space

            // Replace spaces with hyphens
            return Regex.Replace(cleaned,@"\s","-");
        }
    }
}
