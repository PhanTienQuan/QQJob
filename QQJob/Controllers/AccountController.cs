using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace QQJob.Controllers
{
    public class AccountController(UserManager<AppUser> userManager,SignInManager<AppUser> signInManager,ISenderEmail emailSender,RoleManager<IdentityRole> roleManager,IAppUserRepository appUserRepository):Controller
    {
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

            if(await userManager.FindByEmailAsync(model.Email) != null)
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
                Employer = model.AccountType == false ? new Employer() : null,
                IsVerified = UserStatus.Unverified,
                Avatar = "/assets/img/avatars/default-avatar.jpg",
                Slug = await GenerateUniqueSlugAsync(model.Fullname)
            };

            string roleName = model.AccountType == true ? "Candidate" : "Employer";
            if(!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole { Name = roleName });
            }

            var result = await userManager.CreateAsync(user,model.Password);
            if(!result.Succeeded)
            {
                return Json(new
                {
                    success = false,
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            if(!await userManager.IsInRoleAsync(user,roleName))
            {
                await userManager.AddToRoleAsync(user,roleName);
            }
            var resendLink = Url.Action("ResendConfirmationEmail","Account");
            var resendMessage = $@"A verification email was send to your email. Didn't receive an email? <a href=""{resendLink}"">Click here</a>";
            await SendConfirmationEmail(model.Email,user);
            return Json(new { success = true,message = resendMessage,email = model.Email,password = model.Password });
        }

        [NonAction]
        private async Task SendConfirmationEmail(string? email,AppUser? user)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
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
            await emailSender.SendEmailAsync(email,subject,messageBody,true);
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
        public async Task<IActionResult> Login(string? ReturnUrl = null)
        {
            LoginViewModel model = new LoginViewModel
            {
                ReturnUrl = ReturnUrl,
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
            };
            return PartialView("_LoginModal",model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email);
                if(user == null)
                {
                    ModelState.AddModelError("All","User does not exist.");
                    return Json(new
                    {
                        success = false,
                        errors = GetModelStateErrors()
                    });
                }
                else if(!await userManager.IsEmailConfirmedAsync(user))
                {
                    ModelState.AddModelError("All","Email is not confirmed.");
                    return Json(new
                    {
                        success = false,
                        errors = GetModelStateErrors()
                    });
                }
                else
                {
                    var logins = await userManager.GetLoginsAsync(user);
                    if(logins.Any())
                    {
                        var provider = logins.First().LoginProvider;
                        ModelState.AddModelError("All",$"This account uses {provider} login. Please sign in with {provider}.");
                        return Json(new
                        {
                            success = false,
                            errors = GetModelStateErrors()
                        });
                    }
                    else if(string.IsNullOrEmpty(model.Password) || !await userManager.CheckPasswordAsync(user,model.Password))
                    {
                        ModelState.AddModelError("All","Wrong password.");
                        return Json(new
                        {
                            success = false,
                            errors = GetModelStateErrors()
                        });
                    }
                    else
                    {
                        // Success login
                        var result = await signInManager.PasswordSignInAsync(
                            user.UserName ?? string.Empty,
                            model.Password ?? string.Empty,
                            model.RememberMe,
                            lockoutOnFailure: false
                        );
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
                        return Json(new { success = true,url = Url.Action("index","home") });
                    }
                }
            }
            else
            {
                model.ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                return Json(new
                {
                    success = false,
                    errors = GetModelStateErrors()
                });
            }
        }
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("index","home");
        }

        [HttpGet]
        public IActionResult SetAccountType()
        {
            var model = new AccountTypeViewModel
            {
                AccountType = true,
            };
            return PartialView("SetAccountTypeModal",model);
        }

        [HttpPost]
        public async Task<IActionResult> SetAccountType(AccountTypeViewModel model)
        {
            if(!ModelState.IsValid) { return RedirectToAction("SetAccountType"); }

            var user = (await appUserRepository.GetUserAsync(u => u.Email == model.Email)).FirstOrDefault();
            if(user == null)
            {
                return RedirectToAction("SetAccountType");
            }

            if(model.AccountType == true)
            {
                user.Candidate = new Candidate();
                await userManager.AddToRoleAsync(user,"Candidate");
            }
            else
            {
                user.Employer = new Employer();
                await userManager.AddToRoleAsync(user,"Employer");
            }

            appUserRepository.Update(user);
            await appUserRepository.SaveChangesAsync();

            // Sign in the user locally after linking their external login.
            await signInManager.SignInAsync(user,isPersistent: false);

            return RedirectToAction("Index",new { controller = "Home" });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider,string? returnUrl)
        {
            var redirectUrl = Url.Action(
                action: "ExternalLoginCallback",
                controller: "Account",
                values: new { ReturnUrl = returnUrl ?? Url.Content("~/") }
            );

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider,redirectUrl);

            return new ChallengeResult(provider,properties);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl,string? remoteError)
        {
            // Check if an error occurred during the external authentication process.
            // If so, display an alert to the user and close the popup window.
            if(remoteError != null)
            {
                return Content($"<script>alert('Error from external provider: {remoteError}'); window.close();</script>","text/html");
            }

            // Retrieve login information about the user from the external login provider (e.g., Google, Facebook).
            // This includes details like the provider's name and the user's identifier within that provider.
            var info = await signInManager.GetExternalLoginInfoAsync();

            // If the login information could not be retrieved, display an error message
            // and close the popup window.
            if(info == null)
            {
                return Content($"<script>alert('Error loading external login information.'); window.close();</script>","text/html");
            }

            // Attempt to sign in the user using their external login details.
            // If a corresponding record exists in the AspNetUserLogins table, the user will be logged in.
            var signInResult = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,    // The name of the external login provider (e.g., Google, Facebook).
                info.ProviderKey,      // The unique identifier of the user within the external provider.
                isPersistent: false,   // Indicates whether the login session should persist across browser restarts.
                bypassTwoFactor: true  // Bypass two-factor authentication if enabled.
            );

            // If the external login succeeds, redirect the parent window to the returnUrl
            // and close the popup window.
            if(signInResult.Succeeded)
            {
                return Content($"<script>window.opener.location.href = '{returnUrl}'; window.close();</script>","text/html");
            }

            // If the user does not have a corresponding record in the AspNetUserLogins table,
            // attempt to create a new account using the user's email from the external provider.
            var email = info.Principal.FindFirstValue(ClaimTypes.Email); // Retrieve the user's email from the external login provider.

            if(email != null)
            {
                // Check if a local user account with the retrieved email already exists.
                var user = await userManager.FindByEmailAsync(email);

                // If no local account exists, create a new user in the AspNetUsers table.
                if(user == null)
                {
                    user = new AppUser
                    {
                        UserName = email, // Set the username to the user's email.
                        Email = email,
                        FullName = info.Principal.FindFirstValue(ClaimTypes.GivenName) + " " + info.Principal.FindFirstValue(ClaimTypes.Surname),
                        CreatedAt = DateTime.UtcNow,
                        EmailConfirmed = true,
                        IsVerified = UserStatus.Unverified,
                        Avatar = "/assets/img/avatars/default-avatar.jpg",
                        Slug = await GenerateUniqueSlugAsync(info.Principal.FindFirstValue(ClaimTypes.GivenName) + " " + info.Principal.FindFirstValue(ClaimTypes.Surname))
                    };

                    // Create the new user in the database.
                    await userManager.CreateAsync(user);
                }

                // Link the external login to the newly created or existing user account.
                // This inserts a record into the AspNetUserLogins table.
                await userManager.AddLoginAsync(user,info);

                // Sign in the user locally after linking their external login.
                await signInManager.SignInAsync(user,isPersistent: false);

                if(!(await userManager.GetRolesAsync(user)).Any())
                {
                    return Content($"<script>window.opener.showSetAccountTypeModel(); window.close();</script>","text/html");
                }
                else
                {
                    // Redirect the parent window to the returnUrl and close the popup window.
                    return Content($"<script>window.opener.location.href = '{returnUrl}'; window.close();</script>","text/html");
                }
            }

            // If the email claim is not provided by the external login provider,
            // display an error message and close the popup window.
            return Content($"<script>alert('Email claim not received. Please contact support.'); window.close();</script>","text/html");
        }

        [AllowAnonymous]
        public IActionResult OnExternalLoginDenied()
        {
            return Content($"<script>window.opener.location.href = '/'; window.close();</script>","text/html");
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
        public static string GenerateSlug(string text)
        {
            if(string.IsNullOrWhiteSpace(text))
                return Guid.NewGuid().ToString("N");

            // Convert to lowercase
            string slug = text.ToLowerInvariant();

            // Remove diacritics (accents, etc.)
            slug = RemoveDiacritics(slug);

            // Replace spaces and special characters with dashes
            slug = Regex.Replace(slug,@"[^a-z0-9\s-]",""); // keep only a-z, 0-9, space, dash
            slug = Regex.Replace(slug,@"[\s-]+","-").Trim('-'); // collapse and trim dashes

            return slug;
        }
        [NonAction]
        private static string RemoveDiacritics(string text)
        {
            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach(var c in normalized)
            {
                if(CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
        private async Task<string> GenerateUniqueSlugAsync(string fullName)
        {
            var baseSlug = GenerateSlug(fullName);
            var slug = baseSlug;
            int counter = 1;

            while(await userManager.Users.AnyAsync(u => u.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            return slug;
        }


    }
}
