using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QQJob.AIs;
using QQJob.Helper;
using Microsoft.EntityFrameworkCore;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels.CandidateViewModels;
using System.Security.Claims;

namespace QQJob.Controllers
{
    [Authorize(Roles = "Candidate")]
    public class CandidateController : Controller
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly ISkillRepository _skillRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IChatSessionRepository _chatSessionRepository;
        private readonly IAppUserRepository _appUserRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly TextCompletionAI _textCompletionAI;
        private readonly EmbeddingAI _embeddingAI;
        public CandidateController(
            ICandidateRepository candidateRepository,
            ISkillRepository skillRepository,
            INotificationRepository notificationRepository,
            IChatSessionRepository chatSessionRepository,
            ICloudinaryService cloudinaryService,
            TextCompletionAI textCompletionAI,
            EmbeddingAI embeddingAI
        )
        {
            _candidateRepository = candidateRepository;
            _skillRepository = skillRepository;
            _notificationRepository = notificationRepository;
            _cloudinaryService = cloudinaryService;
            _chatSessionRepository = chatSessionRepository;
            _textCompletionAI = textCompletionAI;
            _embeddingAI = embeddingAI;
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
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);
            if (candidate == null || candidate.User == null)
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
                ResumeUrl = candidate?.Resume?.Url,
                Educations = candidate?.Educations?.ToList() ?? new List<Education>(),
                Experiences = candidate?.CandidateExps?.ToList() ?? new List<CandidateExp>(),
                Awards = candidate?.Awards?.ToList() ?? new List<Award>()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Resume(ResumeViewModal model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);

            if (model.ResumeFile == null || model.ResumeFile.Length == 0)
            {
                ModelState.AddModelError("ResumeFile", "Please select a PDF file.");
                model.ResumeUrl = candidate?.Resume?.Url;
                return View(model);
            }

            // Chỉ nhận file PDF
            if (Path.GetExtension(model.ResumeFile.FileName).ToLower() != ".pdf")
            {
                ModelState.AddModelError("ResumeFile", "Only PDF files are allowed.");
                model.ResumeUrl = candidate?.Resume?.Url;
                return View(model);
            }

            // Upload lên Cloudinary
            string resumeUrl;
            try
            {
                resumeUrl = await _cloudinaryService.UploadResumeAsync(model.ResumeFile, userId);
            }
            catch
            {
                ModelState.AddModelError("ResumeFile", "Upload failed. Please try again.");
                model.ResumeUrl = candidate?.Resume?.Url;
                return View(model);
            }

            using var memoryStream = new MemoryStream();
            await model.ResumeFile.CopyToAsync(memoryStream);
            byte[] fileBytes = memoryStream.ToArray();
            var resumeText = await TextExtractionHelper.ExtractCvTextAsync(fileBytes, model.ResumeFile.FileName);
            var summary = await _textCompletionAI.SummarizeResume(resumeText);
            // Lưu vào DB
            if (candidate.Resume == null)
            {
                candidate.Resume = new Resume
                {
                    CandidateId = userId,
                    Url = resumeUrl,
                    AiSumary = summary,
                };
            }
            else
            {
                candidate.Resume.Url = resumeUrl;
                candidate.Resume.AiSumary = summary;
            }

            _candidateRepository.Update(candidate);
            await _candidateRepository.SaveChangesAsync();

            TempData["Message"] = "Resume uploaded successfully!";
            model.ResumeUrl = resumeUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateResumeInfo(ResumeViewModal model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);

            // Update Education
            candidate.Educations = model.Educations ?? new List<Education>();

            // Update Experience
            candidate.CandidateExps = model.Experiences ?? new List<CandidateExp>();

            // Update Award
            candidate.Awards = model.Awards ?? new List<Award>();

            _candidateRepository.Update(candidate);
            await _candidateRepository.SaveChangesAsync();

            TempData["Message"] = "Resume info updated successfully!";
            return RedirectToAction("Resume");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEducation(string universityName, string startDate, string endDate, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Not logged in" });

            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);
            if (candidate == null)
                return Json(new { success = false, message = "Candidate not found" });

            DateTime? start = null, end = null;
            if (DateTime.TryParse(startDate, out var s)) start = s;
            if (DateTime.TryParse(endDate, out var e)) end = e;

            var education = new Education
            {
                CandidateId = userId,
                UniversityName = universityName,
                StartDate = start,
                EndDate = end,
                Description = description
            };

            candidate.Educations ??= new List<Education>();
            candidate.Educations.Add(education);

            _candidateRepository.Update(candidate);
            await _candidateRepository.SaveChangesAsync();

            return Json(new { success = true, message = "Education added successfully!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEducation(int educationId, string universityName, string startDate, string endDate, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);
            var education = candidate?.Educations?.FirstOrDefault(e => e.EducationId == educationId);
            if (education == null)
                return Json(new { success = false, message = "Education not found" });

            education.UniversityName = universityName;
            education.StartDate = DateTime.TryParse(startDate, out var s) ? s : null;
            education.EndDate = DateTime.TryParse(endDate, out var e) ? e : null;
            education.Description = description;

            _candidateRepository.Update(candidate);
            await _candidateRepository.SaveChangesAsync();
            return Json(new { success = true, message = "Education updated!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducation(int educationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(userId);
            var education = candidate?.Educations?.FirstOrDefault(e => e.EducationId == educationId);
            if (education == null)
                return Json(new { success = false, message = "Education not found" });

            candidate.Educations.Remove(education);
            _candidateRepository.Update(candidate);
            await _candidateRepository.SaveChangesAsync();
            return Json(new { success = true, message = "Education deleted!" });
        }

        public IActionResult AppliedJob()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var candidate = await _candidateRepository.GetCandidateWithDetailsAsync(id);
            if (candidate == null || candidate.User == null)
                return NotFound();

            var model = new CandidateDetailViewModel
            {
                CandidateId = candidate.CandidateId,
                FullName = candidate.User.FullName,
                Avatar = candidate.User.Avatar,
                JobTitle = candidate.JobTitle,
                Description = candidate.Description,
                Skills = candidate.Skills?.Select(s => s.SkillName).ToList() ?? new List<string>(),
                Educations = candidate.Educations?.ToList() ?? new List<Education>(),
                Experiences = candidate.CandidateExps?.ToList() ?? new List<CandidateExp>(),
                Awards = candidate.Awards?.ToList() ?? new List<Award>(),
                ResumeUrl = candidate.Resume?.Url
            };

            return View(model);
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
