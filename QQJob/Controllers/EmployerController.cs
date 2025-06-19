using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QQJob.Helper;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using QQJob.ViewModels.EmployerViewModels;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace QQJob.Controllers
{
    [Authorize(Roles = "Employer")]
    public class EmployerController(IEmployerRepository employerRepository,
        IApplicationRepository applicationRepository,
        ISkillRepository skillRepository,
        IJobRepository jobRepository,
        ICloudinaryService cloudinaryService,
        IChatSessionRepository chatSessionRepository,
        IAppUserRepository appUserRepository,
        INotificationRepository notificationRepository
        ):Controller
    {
        private readonly IEmployerRepository _employerRepository = employerRepository;
        private readonly IApplicationRepository _applicationRepository = applicationRepository;
        private readonly ISkillRepository _skillRepository = skillRepository;
        private readonly IJobRepository _jobRepository = jobRepository;
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _employerRepository.GetByIdAsync(userId);
            var dashboardViewModel = new DashboardViewModel
            {
                PostedJobCount = 0,
                ApplicationCount = 0,
                FollowerCount = 0,
                ViewCount = 0,
                RecentApplicants = new List<Application>()
            };

            if(user.Jobs == null)
            {
                return View(dashboardViewModel);
            }

            dashboardViewModel.PostedJobCount = user.Jobs.Count;
            dashboardViewModel.ApplicationCount = user.Jobs.Sum(j => j.Applications.Count());
            dashboardViewModel.FollowerCount = user.Follows.Count();
            dashboardViewModel.ViewCount = user.Jobs.Sum(j => j.ViewCount);
            dashboardViewModel.RecentApplicants = await applicationRepository.GetApplicationsByEmployerId(userId);

            return View(dashboardViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ConvertToPdf(IFormFile file)
        {
            if(file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var pdfFile = ConversionHelper.ConvertWordToPDF(file);
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(),"wwwroot","uploads");
            var pdfFileName = pdfFile.FileName;
            var pdfFilePath = Path.Combine(uploadsFolder,pdfFileName);

            if(!System.IO.File.Exists(pdfFilePath))
            {
                TempData["Message"] = JsonConvert.SerializeObject(new
                {
                    message = "Failed to convert DOCX to PDF.",
                    type = "warning"
                });
                return BadRequest(new { error = "Failed to convert DOCX to PDF." });
            }

            // Return URL to PDF
            var pdfUrl = Url.Content($"~/uploads/{pdfFileName}");
            return Json(new { pdfUrl });
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var eId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(string.IsNullOrEmpty(eId))
            {
                return RedirectToAction("Login","Account");
            }

            var employer = await _employerRepository.GetByIdAsync(eId);
            if(employer == null)
            {
                return NotFound("Employer profile not found.");
            }

            List<SocialLink>? socialLinks = string.IsNullOrWhiteSpace(employer.User.SocialLink) ? new List<SocialLink>() : JsonConvert.DeserializeObject<List<SocialLink>>(employer.User.SocialLink);

            var model = new EmployerProfileViewModel
            {
                Id = employer.EmployerId,
                Avatar = employer.User.Avatar,
                FullName = employer.User.FullName,
                Email = employer.User.Email,
                PhoneNumber = employer.User.PhoneNumber,
                Website = employer.Website,
                FoundedDate = employer.FoundedDate != DateTime.MinValue ? employer.FoundedDate : null,
                CompanySize = employer.CompanySize,
                ForPublicView = true,
                SocialLinks = socialLinks,
                CompanyField = employer.CompanyField,
                IsVerified = employer.User.IsVerified,
                CompanyEvidentUrl = employer.CompanyEvident?.Url
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(EmployerProfileViewModel model)
        {
            if(!ModelState.IsValid) return BadRequest(ModelState);

            var employer = await _employerRepository.GetByIdAsync(model.Id);
            if(employer == null)
            {
                return View(model);
            }

            bool isUpdated = false;
            // Update avatar if a new file is uploaded
            if(model.AvatarFile?.Length > 0)
            {
                try
                {
                    employer.User.Avatar = model.Avatar = await cloudinaryService.UpdateAvatar(model.AvatarFile,model.Id);
                    isUpdated = true;
                }
                catch
                {
                    TempData["Message"] = JsonConvert.SerializeObject(new { message = "Something happen to cloudinary server!",type = "error" });
                    return View(model);
                }
            }

            // Update fields if they differ from the current values
            employer.User.FullName = UpdateIfDifferent(employer.User.FullName,model.FullName.Trim(),ref isUpdated);
            employer.User.PhoneNumber = UpdateIfDifferent(employer.User.PhoneNumber,model.PhoneNumber?.Trim(),ref isUpdated);
            employer.Website = UpdateIfDifferent(employer.Website,model.Website?.Trim(),ref isUpdated);
            employer.FoundedDate = UpdateIfDifferent(employer.FoundedDate,model.FoundedDate ??= DateTime.MinValue,ref isUpdated);
            employer.CompanySize = UpdateIfDifferent(employer.CompanySize,model.CompanySize?.Trim(),ref isUpdated);
            employer.CompanyField = UpdateIfDifferent(employer.CompanyField,model.CompanyField,ref isUpdated);
            var socialLinksJson = JsonConvert.SerializeObject(model.SocialLinks);
            employer.User.SocialLink = UpdateIfDifferent(employer.User.SocialLink,socialLinksJson,ref isUpdated);


            if(isUpdated)
            {
                _employerRepository.Update(employer);
                await _employerRepository.SaveChangesAsync();
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Profile updated successfully!",type = "success" });
            }
            else
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "No changes detected.",type = "none" });
            }

            model.SocialLinks = JsonConvert.DeserializeObject<List<SocialLink>>(employer.User.SocialLink) ?? [];
            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> UploadEvident(IFormFile file,string userId)
        {
            if(file == null || file.Length == 0)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new
                {
                    message = "No file uploaded.",
                    type = "error"
                });
                return RedirectToAction("Profile");
            }

            if(string.IsNullOrWhiteSpace(userId))
            {
                TempData["Message"] = JsonConvert.SerializeObject(new
                {
                    message = "Invalid user.",
                    type = "error"
                });
                return RedirectToAction("Profile");
            }

            var todayUploadAttempt = await notificationRepository.GetUploadAttemptToday(userId);
            if(todayUploadAttempt >= 2)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new
                {
                    message = "Only 2 upload attempts per day! Try again tomorrow.",
                    type = "warning"
                });
                return RedirectToAction("Profile");
            }

            var user = await employerRepository.GetByIdAsync(userId);
            if(user == null)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new
                {
                    message = "User not found.",
                    type = "error"
                });
                return RedirectToAction("Profile");
            }

            var userNotification = new Notification
            {
                CreatedDate = DateTime.Now,
                Content = "Your evidence was uploaded successfully.",
                IsReaded = false,
                ReceiverId = userId,
                Type = NotificationType.EvidenceUploaded,
                UserType = UserType.User
            };

            var adminNotification = new Notification
            {
                CreatedDate = DateTime.Now,
                Content = $"Employer {user.User.FullName} (ID: {user.EmployerId}) uploaded new evidence.",
                IsReaded = false,
                Type = NotificationType.EvidenceUploaded,
                UserType = UserType.Admin
            };

            try
            {
                var allowedExtensions = new[] { ".doc",".docx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if(allowedExtensions.Contains(extension))
                {
                    file = ConversionHelper.ConvertWordToPDF(file);
                }

                var path = await cloudinaryService.UploadEvidentAsync(file,userId);
                if(path == "Invalid file")
                {
                    TempData["Message"] = JsonConvert.SerializeObject(new { message = "Invalid file!",type = "error" });
                    return RedirectToAction("Profile");
                }

                if(user.CompanyEvident != null)
                {
                    var originalFileName = Path.GetFileName(user.CompanyEvident.Url);
                    var newFileName = Path.GetFileName(path);

                    if(originalFileName != newFileName)
                    {
                        await cloudinaryService.DeleteFile(user.CompanyEvident.Url);
                    }
                }

                user.User.IsVerified = UserStatus.Pending;
                user.CompanyEvident = new CompanyEvident
                {
                    EmployerId = userId,
                    Url = path,
                    CreatedAt = DateTime.Now
                };
                employerRepository.Update(user);
                await employerRepository.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error uploading evidence: {ex.Message}");
                // Optionally log the exception: _logger?.LogError(ex, "Error uploading evidence");
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Something happened to the cloud server!",type = "error" });
                return RedirectToAction("Profile");
            }

            await notificationRepository.AddAsync(userNotification);
            await notificationRepository.AddAsync(adminNotification);
            await notificationRepository.SaveChangesAsync();

            TempData["Message"] = JsonConvert.SerializeObject(new { message = "Your evidence was uploaded successfully",type = "success" });
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public async Task<IActionResult> JobsPosted(int page = 1,int pageSize = 5)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var (jobs, pagingModel) = await _jobRepository.GetJobsAsync(page,pageSize,j => j.EmployerId == id);

            var model = new PostedJobsViewModel()
            {
                Jobs = new List<PostedJobViewModel>()
            };

            foreach(var job in jobs)
            {
                model.Jobs.Add(new PostedJobViewModel
                {
                    Id = job.JobId,
                    Title = job.JobTitle,
                    City = job.City,
                    Description = job.Description,
                    Open = job.PostDate,
                    Close = job.CloseDate,
                    AppliedCount = job.Applications != null ? job.Applications.Count() : 0,
                    Status = job.Status,
                    Slug = job.Slug
                });
            }

            model.Paging = pagingModel;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetJobListPartial(PagingModel paging,string? searchValue = null,Status? searchStatus = null,DateTime? fromDate = null,DateTime? toDate = null)
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var current = paging.CurrentPage;
            var (jobs, pagingModel) = await _jobRepository.GetJobsAsync(paging.CurrentPage,paging.PageSize,j => j.EmployerId == id,searchValue,searchStatus,fromDate,toDate);

            var model = new PostedJobsViewModel()
            {
                Jobs = new List<PostedJobViewModel>()
            };

            foreach(var job in jobs)
            {
                model.Jobs.Add(new PostedJobViewModel
                {
                    Id = job.JobId,
                    Title = job.JobTitle,
                    City = job.City,
                    Description = job.Description,
                    Open = job.PostDate,
                    Close = job.CloseDate,
                    AppliedCount = job.Applications != null ? job.Applications.Count() : 0,
                    Status = job.Status,
                    Slug = job.Slug
                });
            }

            model.Paging = pagingModel;
            model.SearchValue = searchValue;
            model.SearchStatus = searchStatus;
            model.FromDate = fromDate;
            model.ToDate = toDate;

            return PartialView("_PostedJobList",model);
        }

        [HttpGet]
        public async Task<IActionResult> PostJob()
        {
            var skills = await _skillRepository.GetAllAsync();
            ViewBag.SkillList = skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();
            var model = new PostJobViewModel()
            {
                EmployerId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Opening = 1
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PostJob(PostJobViewModel model)
        {
            var skills = await _skillRepository.GetAllAsync();

            ViewBag.SkillList = skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            if(!ModelState.IsValid)
            {
                return View(model);
            }

            var selectedSkill = new List<Skill>();

            foreach(var id in model.SelectedSkill.Split(","))
            {
                var skill = await _skillRepository.GetByIdAsync(int.Parse(id));
                if(skill != null) // Ensure skill is not null before adding
                {
                    selectedSkill.Add(skill);
                }
            }
            var employer = await _employerRepository.GetByIdAsync(model.EmployerId);
            var confirmed = employer.User.IsVerified;
            var status = confirmed == UserStatus.Verified ? Status.Approved : Status.Pending;

            Job newJob = new()
            {
                EmployerId = model.EmployerId,
                JobTitle = model.JobTitle,
                Description = model.Description,
                City = model.City,
                ExperienceLevel = model.ExperienceLevel,
                Salary = model.Salary,
                SalaryType = model.SalaryType,
                Opening = model.Opening,
                PostDate = DateTime.Now,
                CloseDate = (DateTime)model.Close,
                Skills = selectedSkill,
                Status = status,
                JobType = model.JobType,
                LocationRequirement = model.LocationRequirement,
                Slug = await GenerateUniqueSlugAsync(model.JobTitle)
            };

            await _jobRepository.AddAsync(newJob);
            await _jobRepository.SaveChangesAsync();

            var notification = new Notification
            {
                Content = "You have post a job successful!",
                CreatedDate = DateTime.Now,
                ReceiverId = model.EmployerId,
                Type = NotificationType.PostJob,
                UserType = UserType.User
            };

            await notificationRepository.AddAsync(notification);
            await notificationRepository.SaveChangesAsync();

            var message = "Post successfully!" + (confirmed != UserStatus.Verified ? " Wait for admin to verify your post" : "");
            TempData["Message"] = JsonConvert.SerializeObject(new { message,type = "success" });
            return RedirectToAction("JobsPosted");
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

            while(await _jobRepository.AnyAsync(u => u.Slug == slug))
            {
                slug = $"{baseSlug}-{counter++}";
            }

            return slug;
        }

        [HttpGet]
        public async Task<IActionResult> EditJob(int id)
        {
            var job = await _jobRepository.GetByIdAsync(id);

            var jobDetailViewModel = new EditJobViewModel()
            {
                Id = job.JobId,
                JobTitle = job.JobTitle,
                City = job.City,
                Description = job.Description,
                Close = job.CloseDate,
                Salary = job.Salary,
                Opening = job.Opening,
                ExperienceLevel = job.ExperienceLevel,
                JobType = job.JobType,
                LocationRequirement = job.LocationRequirement,
                SalaryType = job.SalaryType,
            };
            var skills = await _skillRepository.GetAllAsync();
            ViewBag.SkillList = skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            ViewBag.InitSkills = skills.Where(skill => job.Skills.Contains(skill)).Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            return View(jobDetailViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> EditJob(EditJobViewModel model)
        {
            var job = await _jobRepository.GetByIdAsync(model.Id);

            var jobDetailViewModel = new EditJobViewModel()
            {
                Id = job.JobId,
                JobTitle = job.JobTitle,
                City = job.City,
                Description = job.Description,
                Close = job.CloseDate,
                Salary = job.Salary,
                Opening = job.Opening,
                ExperienceLevel = job.ExperienceLevel,
                JobType = job.JobType,
                LocationRequirement = job.LocationRequirement,
                SalaryType = job.SalaryType,
                SelectedSkill = string.Join(",",job.Skills.Select(skill => skill.SkillId))
            };

            if(ObjectComparer.AreEqual(model,jobDetailViewModel))
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Nothing changed!",type = "none" });
                return RedirectToAction("EditJob");
            }

            var skills = await _skillRepository.GetAllAsync();
            ViewBag.SkillList = skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            ViewBag.InitSkills = job.Skills.Select(skill => new
            {
                Id = skill.SkillId,
                Name = skill.SkillName
            }).ToList();

            if(!ModelState.IsValid)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Some field are wrong!",type = "error" });
                return View(model);
            }

            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> Message()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessions = await chatSessionRepository.GetChatSession(userId,10,10);
            MessageViewModel messageViewModel = new MessageViewModel()
            {
                Sessions = sessions,
                CurrentChatSession = sessions != null ? sessions.FirstOrDefault() : new ChatSession(),
                CurrentUser = await appUserRepository.GetByIdAsync(userId),
            };
            return View(messageViewModel);
        }
        [NonAction]
        private T UpdateIfDifferent<T>(T currentValue,T newValue,ref bool isUpdated)
        {
            if(!EqualityComparer<T>.Default.Equals(currentValue,newValue))
            {
                isUpdated = true;
                return newValue;
            }
            return currentValue;
        }
    }

}
