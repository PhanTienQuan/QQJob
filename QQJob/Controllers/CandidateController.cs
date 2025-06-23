using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QQJob.Models;
using QQJob.Repositories.Implementations;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels.CandidateViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QQJob.Controllers
{
    [Authorize(Roles = "Candidate")]
    public class CandidateController : Controller
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IChatSessionRepository _chatSessionRepository;
        private readonly IAppUserRepository _appUserRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public CandidateController(
            ICandidateRepository candidateRepository,
            INotificationRepository notificationRepository,
            IChatSessionRepository chatSessionRepository,
            ICloudinaryService cloudinaryService
        )
        {
            _candidateRepository = candidateRepository;
            _notificationRepository = notificationRepository;
            _cloudinaryService = cloudinaryService;
            _chatSessionRepository = chatSessionRepository;
        }

        private T UpdateIfDifferent<T>(T currentValue, T newValue, ref bool isUpdated)
        {
            if (!EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                isUpdated = true;
                return newValue;
            }
            return currentValue;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);

            // Số job đã lưu
            int savedJobCount = candidate.SavedJobs?.Count ?? 0;

            // Số thông báo chưa đọc
            int unreadNotificationCount = await _notificationRepository.GetUserUnreadNotificationCountAsync(userId);

            // Số tin nhắn chưa đọc (giả sử mỗi session có Messages navigation property)
            int unreadMessageCount = 0;
            var chatSessions = await _chatSessionRepository.GetChatSession(userId);
            foreach (var session in chatSessions)
            {
                if (session.Messages != null)
                {
                    unreadMessageCount += session.Messages.Count(m => !m.IsRead && m.SenderId != userId);
                }
            }

            var dashboardViewModel = new DashboardViewModel
            {
                ApplicationCount = candidate.Applications?.Count ?? 0,
                SavedJobCount = savedJobCount,
                UnreadNotificationCount = unreadNotificationCount,
                UnreadMessageCount = unreadMessageCount,
                FollowerCount = candidate.Follows?.Count ?? 0,
                ViewCount = candidate.ViewJobHistories?.Count ?? 0,
                RecentApplications = candidate.Applications?
                    .OrderByDescending(a => a.ApplicationDate)
                    .Take(5)
                    .ToList()
            };

            return View(dashboardViewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);
            if (candidate == null || candidate.User == null)
                return NotFound("Candidate profile not found.");

            var user = candidate.User;

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Avatar = user.Avatar,
                JobTitle = candidate.JobTitle,
                Description = candidate.Description,
                WorkingType = candidate.WorkingType,
                ResumeUrl = candidate.Resume?.Url
            };

            ViewBag.WorkingTypes = new List<SelectListItem>
            {
                new("Full-time", "Full-time"),
                new("Part-time", "Part-time"),
                new("Remote", "Remote"),
                new("Other", "Other")
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(userId))
                return RedirectToAction("Login","Account");

            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);
            if(candidate == null || candidate.User == null)
                return NotFound("Candidate profile not found.");

            bool isUpdated = false;

            // Update avatar if a new file is uploaded
            if (model.AvatarFile?.Length > 0)
            {
                try
                {
                    candidate.User.Avatar = model.Avatar = await _cloudinaryService.UpdateAvatar(model.AvatarFile, model.Id);
                    isUpdated = true;
                }
                catch
                {
                    TempData["Message"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Something happened to cloudinary server!", type = "error" });
                    return View(model);
                }
            }

            // Update fields if they differ from the current values
            candidate.User.FullName = UpdateIfDifferent(candidate.User.FullName, model.FullName?.Trim(), ref isUpdated);
            candidate.User.PhoneNumber = UpdateIfDifferent(candidate.User.PhoneNumber, model.PhoneNumber?.Trim(), ref isUpdated);
            candidate.JobTitle = UpdateIfDifferent(candidate.JobTitle, model.JobTitle?.Trim(), ref isUpdated);
            candidate.Description = UpdateIfDifferent(candidate.Description, model.Description?.Trim(), ref isUpdated);
            candidate.WorkingType = UpdateIfDifferent(candidate.WorkingType, model.WorkingType, ref isUpdated);

            if (isUpdated)
            {
                _candidateRepository.Update(candidate);
                await _candidateRepository.SaveChangesAsync();
                TempData["Message"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "Profile updated successfully!", type = "success" });
            }
            else
            {
                TempData["Message"] = Newtonsoft.Json.JsonConvert.SerializeObject(new { message = "No changes detected.", type = "none" });
            }
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public async Task<IActionResult> Resume()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);
            var model = new ResumeViewModal
            {
                ResumeUrl = candidate?.Resume?.Url
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Resume(ResumeViewModal model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);

            if(model.ResumeFile == null || model.ResumeFile.Length == 0)
            {
                ModelState.AddModelError("ResumeFile","Please select a PDF file.");
                model.ResumeUrl = candidate?.Resume?.Url;
                return View(model);
            }

            // Chỉ nhận file PDF
            if(Path.GetExtension(model.ResumeFile.FileName).ToLower() != ".pdf")
            {
                ModelState.AddModelError("ResumeFile","Only PDF files are allowed.");
                model.ResumeUrl = candidate?.Resume?.Url;
                return View(model);
            }

            // Upload lên Cloudinary
            string resumeUrl;
            try
            {
                resumeUrl = await _cloudinaryService.UploadResumeAsync(model.ResumeFile,userId);
            }
            catch
            {
                ModelState.AddModelError("ResumeFile","Upload failed. Please try again.");
                model.ResumeUrl = candidate?.Resume?.Url;
                return View(model);
            }

            // Lưu vào DB
            if(candidate.Resume == null)
            {
                candidate.Resume = new Resume
                {
                    CandidateId = userId,
                    Url = resumeUrl
                };
            }
            else
            {
                candidate.Resume.Url = resumeUrl;
            }

            _candidateRepository.Update(candidate);
            await _candidateRepository.SaveChangesAsync();

            TempData["Message"] = "Resume uploaded successfully!";
            model.ResumeUrl = resumeUrl;
            return View(model);
        }

        public IActionResult AppliedJob()
        {
            return View();
        }

        public IActionResult Message()
        {
            return View();
        }
            
        public IActionResult Meeting()
        {
            return View();
        }

        public IActionResult Follow()
        {
            return View();
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        public IActionResult DeleteProfile()
        {
            return View();
        }
    }
}
