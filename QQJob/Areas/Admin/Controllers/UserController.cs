using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QQJob.Areas.Admin.ViewModels;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UserController(IAppUserRepository appUserRepository,IEmployerRepository employerRepository,UserManager<AppUser> userManager,IChatSessionRepository chatSessionRepository,IChatMessageRepository chatMessageRepository,INotificationRepository notificationRepository):Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await appUserRepository.GetUsersAsync();
            var count = await appUserRepository.GetCount();
            ViewBag.Count = count;
            List<ListUserViewModel> list = new List<ListUserViewModel>();
            foreach(var user in users)
            {
                var roles = await userManager.GetRolesAsync(user);
                list.Add(new ListUserViewModel
                {
                    User = user,
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }
            return View("List",list);
        }

        [HttpGet]
        public async Task<IActionResult> VerificationRequestList()
        {
            var users = await employerRepository.GetAllRQEmployerAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> EvidentSearch(string searchString = "")
        {
            var users = await employerRepository.GetAllRQEmployerAsync();
            var filteredUsers = users
                .Where(u => u.EmployerId.Contains(searchString,StringComparison.OrdinalIgnoreCase) ||
                            (u.User.UserName != null && u.User.UserName.Contains(searchString,StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Return the partial view with the filtered users
            return PartialView("_verificationRequestTable",filteredUsers);
        }
        [HttpPost]
        public async Task<IActionResult> ApproveVerification(string employerId)
        {
            var employer = await employerRepository.GetByIdAsync(employerId);
            if(employer == null || employer.User == null)
                return NotFound();

            employer.User.IsVerified = UserStatus.Verified;
            appUserRepository.Update(employer.User);
            await appUserRepository.SaveChangesAsync();

            var notification = new Notification
            {
                Content = "Your verification request has been approved.",
                ReceiverId = employer.User.Id,
                CreatedDate = DateTime.Now,
                Type = NotificationType.VerificationApproved,
                UserType = UserType.User,
            };

            await notificationRepository.AddAsync(notification);
            await notificationRepository.SaveChangesAsync();
            TempData["Message"] = "Approve Successful!";
            return Json(new { success = true,status = "Verified" });
        }

        [HttpPost]
        public async Task<IActionResult> DenyVerification(string employerId)
        {
            var employer = await employerRepository.GetByIdAsync(employerId);
            if(employer == null || employer.User == null)
                return NotFound();

            employer.User.IsVerified = UserStatus.Rejected;
            appUserRepository.Update(employer.User);
            await appUserRepository.SaveChangesAsync();

            var notification = new Notification
            {
                Content = "Your verification request has been rejected.",
                ReceiverId = employer.User.Id,
                CreatedDate = DateTime.Now,
                Type = NotificationType.VerificatiomnRejected,
                UserType = UserType.User,
            };

            await notificationRepository.AddAsync(notification);
            await notificationRepository.SaveChangesAsync();

            TempData["Message"] = "Reject Successful!";
            return Json(new { success = true,status = "Rejected" });
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

            if(await chatSessionRepository.UpdateRangeNullUserAsync(UserId) && await chatMessageRepository.UpdateRangeNullUserAsync(UserId))
            {
                //Delete the User Using DeleteAsync Method of UserManager Service
                var result = await userManager.DeleteAsync(user);
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
            }
            else
            {
                TempData["Message"] = "Something when wrong!";
            }

            return View("Index");

        }

        [HttpGet]
        public async Task<IActionResult> Evident(string userId)
        {
            var user = await employerRepository.GetByIdAsync(userId);
            var evidentViewModel = new EvidentViewModel
            {
                UserId = userId,
                EvidentUrl = user?.CompanyEvident == null ? "" : user.CompanyEvident.Url,
            };
            return View(evidentViewModel);
        }
    }
}