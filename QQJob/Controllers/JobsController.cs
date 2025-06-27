using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QQJob.AIs;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using QQJob.ViewModels.CandidateViewModels;
using System.Security.Claims;

namespace QQJob.Controllers
{
    public class JobsController(
        IJobRepository jobRepository,
        IEmployerRepository employerRepository,
        IJobSimilarityMatrixRepository jobSimilarityMatrixRepository,
        EmbeddingAI embeddingAI,
        IJobEmbeddingRepository jobEmbeddingRepository,
        TextCompletionAI textCompletionAI,
        ISkillRepository skillRepository,
        ICandidateRepository candidateRepository,
        IApplicationRepository applicationRepository,
        UserManager<AppUser> userManager
        ):Controller
    {
        public async Task<IActionResult> Index(int currentPage = 1,int pageSize = 5)
        {
            var (jobs, pageing) = await jobRepository.GetJobsAsync(currentPage,pageSize,j => j.Status == Status.Approved);
            var joblist = jobs
                .Select(job => new JobViewModel()
                {
                    Id = job.JobId,
                    JobTitle = job.JobTitle,
                    City = job.City,
                    Open = job.PostDate,
                    Close = job.CloseDate,
                    AppliedCount = job.Applications != null ? job.Applications.Count : 0,
                    Status = job.Status,
                    Skills = [.. job.Skills],
                    ExperienceLevel = job.ExperienceLevel,
                    JobType = job.JobType,
                    LocationRequirement = job.LocationRequirement,
                    Salary = job.Salary,
                    SalaryType = job.SalaryType,
                    AvatarUrl = job.Employer.User.Avatar,
                    Slug = job.Slug
                })
                .ToList();
            var jobListViewModel = new JobListViewModel()
            {
                Jobs = joblist,
                Paging = pageing
            };

            var skills = await skillRepository.GetAllAsync();
            ViewBag.Skills = skills;
            return View(jobListViewModel);
        }
        [HttpGet]
        [Route("jobs/{id}/{slug}")]
        public async Task<IActionResult> Detail(int id,string slug)
        {
            var job = await jobRepository.GetByIdAsync(id);
            if(job == null)
            {
                return NotFound();
            }

            // If slug does not match — redirect to correct URL (SEO)
            if(job.Slug != slug)
            {
                return RedirectToRoute(new
                {
                    id = job.JobId,
                    slug = job.Slug
                });
            }
            var currentUser = await userManager.GetUserAsync(User);
            if(job.Status != Status.Approved && (currentUser == null || job.EmployerId != currentUser.Id))
            {
                return RedirectToAction("Index"); // Only employer can view pending job details
            }
            var relatedJobIds = await jobSimilarityMatrixRepository.GetRelatedJobIdsAsync(job.JobId);
            var relatedJobs = await jobRepository.FindJobs(j => relatedJobIds.Contains(j.JobId));
            var relatedJobView = relatedJobs.Select(j => new RelatedJobViewModel()
            {
                Id = j.JobId,
                Avatar = j.Employer.User.Avatar,
                City = j.City,
                JobType = j.JobType,
                Title = j.JobTitle,
                Skills = j.Skills.Select(s => s.SkillName).Take(3).ToList() ?? [],
                Opening = j.Opening,
                Slug = j.Slug
            }).ToList();

            string? resumeUrl = null;
            if(User.Identity.IsAuthenticated && User.IsInRole("Candidate"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var candidate = await candidateRepository.GetCandidateWithDetailsAsync(userId);
                resumeUrl = candidate?.Resume?.Url;
            }

            var jobDetailViewModel = new JobDetailViewModel()
            {
                Id = job.JobId,
                EmployerId = job.EmployerId,
                JobTitle = job.JobTitle,
                City = job.City,
                Description = job.Description,
                PostDate = job.PostDate,
                CloseDate = job.CloseDate,
                AppliedCount = job.Applications != null ? job.Applications.Count() : 0,
                Status = job.Status,
                Skills = job.Skills,
                Salary = job.Salary,
                Opening = job.Opening,
                ExperienceLevel = job.ExperienceLevel,
                Website = job.Employer.Website,
                ImgUrl = job.Employer.User.Avatar,
                JobType = job.JobType,
                LocationRequirement = job.LocationRequirement,
                SalaryType = job.SalaryType,
                RelatedJobs = relatedJobView,
                SocialLinks = string.IsNullOrWhiteSpace(job.Employer.User.SocialLink) ? [] : JsonConvert.DeserializeObject<List<SocialLink>>(job.Employer.User.SocialLink)
            };

            jobDetailViewModel.IsSaved = false;
            if(User.Identity.IsAuthenticated && User.IsInRole("Candidate"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var candidate = await candidateRepository.GetCandidateWithDetailsAsync(userId);
                if(candidate == null)
                    return Json(new { success = false,message = "Candidate not found" });

                if(candidate.SavedJobs == null)
                    candidate.SavedJobs = new List<SavedJob>();

                if(candidate?.SavedJobs != null)
                {
                    jobDetailViewModel.IsSaved = candidate.SavedJobs.Any(sj => sj.JobId == id);
                }
            }

            ViewBag.ResumeUrl = resumeUrl;

            return View(jobDetailViewModel);
        }

        [HttpPost]
        [Route("jobs/{id}/save")]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> SaveJob(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if(string.IsNullOrEmpty(userId))
                    return Json(new { success = false,message = "Not logged in" });

                var candidate = await candidateRepository.GetCandidateWithDetailsAsync(userId);
                if(candidate == null)
                    return Json(new { success = false,message = "Candidate not found" });

                if(candidate.SavedJobs == null)
                    candidate.SavedJobs = new List<SavedJob>();

                var alreadySaved = candidate.SavedJobs.Any(sj => sj.JobId == id);
                if(alreadySaved)
                {
                    var savedJob = candidate.SavedJobs.First(sj => sj.JobId == id);
                    candidate.SavedJobs.Remove(savedJob);
                    candidateRepository.Update(candidate);
                    await candidateRepository.SaveChangesAsync();
                    return Json(new { success = true,saved = false,message = "Removed from favorites." });
                }
                else
                {
                    candidate.SavedJobs.Add(new SavedJob
                    {
                        JobId = id,
                        CandidateId = userId,
                        SaveDate = DateTime.UtcNow
                    });
                    candidateRepository.Update(candidate);
                    await candidateRepository.SaveChangesAsync();
                    return Json(new { success = true,saved = true,message = "Job saved to favorites!" });
                }
            }
            catch(Exception ex)
            {
                // Log lỗi ra console hoặc trả về message cho client
                return Json(new { success = false,message = "Error: " + ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> ApplyJob(ApplyJobViewModel model)
        {
            if(!ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var candidate = await candidateRepository.GetCandidateWithDetailsAsync(userId);
                model.ResumeUrl = candidate?.Resume?.Url;
                return PartialView("_ApplyModal",model);
            }

            var userIdApply = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var candidateApply = await candidateRepository.GetCandidateWithDetailsAsync(userIdApply);
            if(candidateApply == null)
            {
                return Json(new { success = false,message = "Candidate not found." });
            }

            var hasApplied = await applicationRepository.FindAsync(app => app.JobId == model.JobId && app.CandidateId == userIdApply);

            if(hasApplied != null && hasApplied.Count() > 0)
            {
                return Json(new { success = false,message = "You have already applied for this job." });
            }


            var job = await jobRepository.GetJobDetail(j => j.JobId == model.JobId);
            var jobDetail = new
            {
                job.JobId,
                job.JobTitle,
                job.Description,
                Skills = job.Skills.Select(s => s.SkillName).ToList(),
                job.JobType,
                job.Salary,
                job.SalaryType,
                job.City,
                job.LocationRequirement,
                job.ExperienceLevel
            };

            var application = new Application
            {
                JobId = model.JobId,
                CandidateId = userIdApply,
                CoverLetter = model.CoverLetter,
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.Now,
                AIRanking = await textCompletionAI.RankApplication(JsonConvert.SerializeObject(new { jobDetail,candidateApply.Resume.AiSumary,model.JobId,userIdApply }))
            };

            candidateApply.Applications ??= new List<Application>();
            candidateApply.Applications.Add(application);

            candidateRepository.Update(candidateApply);
            await candidateRepository.SaveChangesAsync();

            return Json(new { success = true,message = "Application submitted successfully!" });
        }

        [HttpGet]
        public async Task<IActionResult> GenericSearch(
            [Bind(Prefix = "Paging")] PagingModel pagingModel,
            string? AiSearchQuery,
            string? JobTitle,
            string? City,
            string? ExperienceLevel,
            string? Salary,
            List<string>? Skills,
            bool StrictSearch = false
            )
        {
            if(AiSearchQuery != null && AiSearchQuery.Length >= 200)
            {
                return BadRequest("Your search query can't be longer than 200 characters!");
            }
            // Get Salary range
            var (min, max) = Helper.Helper.ParseSalaryRange(Salary);

            // Build intent
            var intent = new Dtos.JobSearchIntent
            {
                JobTitle = JobTitle,
                City = City,
                ExperienceLevel = ExperienceLevel,
                MinSalary = min,
                MaxSalary = max,
                IncludeSkills = Skills ?? [],
                ExcludeSkills = [],
                JobType = null,
                StrictSearch = StrictSearch
            };
            // If there is AI Query → enrich intent
            if(!string.IsNullOrWhiteSpace(AiSearchQuery))
            {
                var aiIntent = await textCompletionAI.ExtractJobSearchIntent(AiSearchQuery);
                intent.JobTitle ??= aiIntent.JobTitle;
                intent.City ??= aiIntent.City;
                intent.ExperienceLevel ??= aiIntent.ExperienceLevel;
                intent.MinSalary = aiIntent.MinSalary > 0 ? aiIntent.MinSalary : intent.MinSalary;
                intent.MaxSalary = aiIntent.MaxSalary > 0 ? aiIntent.MaxSalary : intent.MaxSalary;
                intent.IncludeSkills = [.. intent.IncludeSkills.Union(aiIntent.IncludeSkills)];
                intent.StrictSearch = aiIntent.StrictSearch || intent.StrictSearch;
            }

            Console.WriteLine(JsonConvert.SerializeObject(intent));
            // Get jobs
            var jobs = await jobRepository.SearchJobsByIntentAsync(intent);

            // Get embeddings (optional if you want AI ranking)
            var jobEmbeddings = await jobEmbeddingRepository.GetAllAsync();
            var vector = string.IsNullOrWhiteSpace(AiSearchQuery) ? null : await embeddingAI.GetTextEmbbeding(AiSearchQuery);

            var rankedJobs = jobs.Select(j =>
            {
                var embeddingRow = jobEmbeddings.FirstOrDefault(e => e.JobId == j.JobId);
                var embeddingVector = embeddingRow != null ? JsonConvert.DeserializeObject<float[]>(embeddingRow.Embedding) ?? [] : [];
                var similarity = vector == null ? 1.0 : embeddingAI.CosineSimilarity(vector,embeddingVector);

                return new { Job = j,Similarity = similarity };
            })
            .OrderByDescending(j => j.Similarity)
            .Skip((pagingModel.CurrentPage - 1) * pagingModel.PageSize)
            .Take(pagingModel.PageSize)
            .ToList();

            var jobListViewModel = new JobListViewModel
            {
                Jobs = rankedJobs.Select(j => new JobViewModel
                {
                    Id = j.Job.JobId,
                    JobTitle = j.Job.JobTitle,
                    City = j.Job.City,
                    Open = j.Job.PostDate,
                    Close = j.Job.CloseDate,
                    AppliedCount = j.Job.Applications?.Count ?? 0,
                    Status = j.Job.Status,
                    Skills = [.. j.Job.Skills],
                    ExperienceLevel = j.Job.ExperienceLevel,
                    JobType = j.Job.JobType,
                    LocationRequirement = j.Job.LocationRequirement,
                    Salary = j.Job.Salary,
                    SalaryType = j.Job.SalaryType,
                    AvatarUrl = j.Job.Employer?.User?.Avatar,
                    Slug = j.Job.Slug
                }).ToList(),
                Paging = pagingModel,
                AiSearchQuery = AiSearchQuery,
                City = City,
                ExperienceLevel = ExperienceLevel,
                JobTitle = JobTitle,
                Salary = Salary,
                Skills = Skills ?? []
            };
            pagingModel.TotalItems = jobs.Count;

            return PartialView("_JobList",jobListViewModel);
        }

    }
}
