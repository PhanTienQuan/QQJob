using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Dtos;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using System.Linq.Expressions;
using System.Linq;
using System.Threading.Tasks;

namespace QQJob.Repositories.Implementations
{
    public class JobRepository : GenericRepository<Job>, IJobRepository
    {
        private readonly QQJobContext _context;

        public JobRepository(QQJobContext context) : base(context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Job> jobs, PagingModel pagingModel)> GetJobsAsync(int currentPage, int pageSize, Expression<Func<Job, bool>>? predicate = null, string? searchValue = null, Status? searchStatus = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Jobs.AsQueryable();

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            if (!string.IsNullOrEmpty(searchValue))
            {
                searchValue = searchValue.ToLower();
                query = query.Where(j =>
                    j.JobTitle.ToLower().Contains(searchValue) ||
                    j.City.ToLower().Contains(searchValue)
                );
            }

            if (searchStatus.HasValue)
            {
                query = query.Where(j =>
                    j.Status == searchStatus
                );
            }

            if (fromDate.HasValue)
            {
                query = query.Where(j => j.PostDate >= fromDate);
            }

            if (toDate.HasValue)
            {
                query = query.Where(j => j.CloseDate <= toDate.Value.AddDays(1).AddTicks(-1));
            }

            var totalItems = await query.CountAsync();

            var jobs = await query
                .OrderByDescending(j => j.PostDate)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Include(j => j.Skills)
                .Include(j => j.Employer)
                .ThenInclude(e => e.User)
                .ToListAsync();

            var pagingModel = new PagingModel
            {
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return (jobs, pagingModel);
        }

        public async Task<IEnumerable<Job>> GetJobsByEmployerIdAsync(string employerId)
        {
            return await _context.Set<Job>()
                .Where(j => j.EmployerId == employerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Job>> SearchJobsAsync(string keyword)
        {
            return await _context.Set<Job>()
                .Where(j => j.JobTitle.Contains(keyword))
                .ToListAsync();
        }

        public async Task<Job> GetByIdAsync(int id)
        {
            return await _dbSet
                .Include(j => j.Skills)
                .Include(j => j.Applications)
                .Include(j => j.Employer)
                .ThenInclude(e => e.User)
                .FirstOrDefaultAsync(j => j.JobId == id);
        }

        public async Task<IEnumerable<Job>?> FindJobs(Expression<Func<Job, bool>>? predicate = null)
        {
            return predicate == null
                ? null
                : await _context.Jobs.Where(predicate).Include(job => job.Skills).Include(j => j.Employer).ThenInclude(e => e.User).ToListAsync();
        }

        public async Task<Job?> GetJobDetail(Expression<Func<Job, bool>>? predicate = null)
        {
            return predicate == null
                ? null
                : await _context.Jobs.FirstOrDefaultAsync(predicate);
        }

        public async Task<List<Job>> GetJobsByIdsAsync(JobSearchIntent intent)
        {
            var query = _dbSet
                .Include(j => j.Skills)
                .Include(j => j.Applications)
                .Include(j => j.Employer).ThenInclude(e => e.User)
                .Where(j => j.Status == Status.Approved);
            if (!string.IsNullOrWhiteSpace(intent.JobTitle))
                query = query.Where(j => j.JobTitle.Contains(intent.JobTitle) || intent.JobTitle.Contains(j.JobTitle));

            if (!string.IsNullOrWhiteSpace(intent.City))
                query = query.Where(j => j.City.Contains(intent.City) || intent.City.Contains(j.City));

            if (!string.IsNullOrWhiteSpace(intent.JobType))
                query = query.Where(j => j.JobType == intent.JobType);

            if (!string.IsNullOrWhiteSpace(intent.ExperienceLevel))
                query = query.Where(j => j.ExperienceLevel == intent.ExperienceLevel);

            if(intent.IncludeSkills.Count != 0)
            {
                if(intent.StrictSearch)
                    query = query.Where(j =>
                        intent.IncludeSkills.All(skill =>
                            j.Skills.Any(s => s.SkillName == skill)));
                else
                    query = query.Where(j =>
                        j.Skills.Any(s => intent.IncludeSkills.Contains(s.SkillName)));
            }


            if(intent.ExcludeSkills.Count != 0)
                query = query.Where(j => !j.Skills.Any(s => intent.ExcludeSkills.Contains(s.SkillName)));

            // ⚠️ DO NOT put ParseSalaryRange in SQL Where — load to memory first!
            var jobs = await query.ToListAsync();

            var filteredJobs = jobs.Where(j =>
            {
                var (min, max) = Helper.Helper.ParseSalaryRange(j.Salary);

                // If intent.MinSalary is set
                if (intent.MinSalary != null)
                {
                    if ((min == null && max == null) ||
                        ((min != null && min < intent.MinSalary) && (max != null && max < intent.MinSalary)))
                    {
                        return false;
                    }
                }

                // If intent.MaxSalary is set
                if (intent.MaxSalary != null)
                {
                    if ((min == null && max == null) ||
                        ((min != null && min > intent.MaxSalary) && (max != null && max > intent.MaxSalary)))
                    {
                        return false;
                    }
                }

                return true;  // include this job
            }).ToList();

            return filteredJobs;
        }

        public async Task<List<Job>> ChatBoxJobsSearchAsync(ChatBoxSearchIntent intent)
        {
            var query = _dbSet
                .Include(j => j.Skills)
                .Include(j => j.Employer)
                .ThenInclude(e => e.User)
                .Where(j => j.Status == Status.Approved)
                .AsQueryable();

            // Filter by title
            if(!string.IsNullOrWhiteSpace(intent.JobTitle))
                query = query.Where(j => j.JobTitle.Contains(intent.JobTitle));

            // Filter by city
            if(!string.IsNullOrWhiteSpace(intent.City))
                query = query.Where(j => j.City.Contains(intent.City));

            // Filter by skills
            if(intent.IncludeSkills?.Count != 0)
            {
                if(intent.StrictSearch)
                {
                    // All skills required
                    query = query.Where(j => intent.IncludeSkills.All(skill =>
                        j.Skills.Any(s => s.SkillName == skill)));
                }
                else
                {
                    // Any skill match
                    query = query.Where(j => j.Skills.Any(s => intent.IncludeSkills.Contains(s.SkillName)));
                }
            }

            // Filter by job type
            if(!string.IsNullOrWhiteSpace(intent.JobType))
                query = query.Where(j => j.JobType == intent.JobType);

            // Filter by experience level
            if(!string.IsNullOrWhiteSpace(intent.ExperienceLevel))
                query = query.Where(j => j.ExperienceLevel == intent.ExperienceLevel);

            // Filter by description keywords
            if(intent.DescriptionKeywords?.Count > 0)
            {
                var lowerKeywords = intent.DescriptionKeywords.Select(k => k.ToLower()).ToList();
                query = query.Where(j =>
                    lowerKeywords.Any(keyword => j.Description.ToLower().Contains(keyword))
                );
            }


            var jobs = await query.ToListAsync();

            var filteredJobs = jobs.Where(j =>
            {
                var (min, max) = Helper.Helper.ParseSalaryRange(j.Salary);

                // If intent.MinSalary is set
                if(intent.MinSalary != null)
                {
                    if((min == null && max == null) ||
                        ((min != null && min < intent.MinSalary) && (max != null && max < intent.MinSalary)))
                    {
                        return false;
                    }
                }

                // If intent.MaxSalary is set
                if(intent.MaxSalary != null)
                {
                    if((min == null && max == null) ||
                        ((min != null && min > intent.MaxSalary) && (max != null && max > intent.MaxSalary)))
                    {
                        return false;
                    }
                }

                return true;  // include this job
            }).ToList();

            if(jobs.Count == 0)
                return [];

            return filteredJobs;
        }

        public async Task<List<Job>> GetAllWithDetail()
        {
            return await _dbSet
                .Include(j => j.Skills)
                .Include(j => j.Applications)
                .Include(j => j.Employer).ThenInclude(e => e.User)
                .ToListAsync();
        }
        public async Task<JobDetailViewModel?> GetJobDetailViewModelAsync(int jobId)
        {
            var job = await _context.Jobs
                .Include(j => j.Employer)
                    .ThenInclude(e => e.User)
                .Include(j => j.Skills)
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.JobId == jobId);

            if (job == null) return null;

            var employer = job.Employer;
            var viewModel = new JobDetailViewModel
            {
                Id = job.JobId,
                EmployerId = job.EmployerId,
                JobTitle = job.JobTitle,
                City = job.City,
                Description = job.Description,
                PostDate = job.PostDate,
                CloseDate = job.CloseDate,
                AppliedCount = job.Applications?.Count ?? 0,
                Status = job.Status,
                Skills = job.Skills,
                Salary = job.Salary,
                Opening = job.Opening,
                ExperienceLevel = job.ExperienceLevel,
                Website = employer?.Website,
                ImgUrl = employer?.User?.Avatar,
                JobType = job.JobType,
                LocationRequirement = job.LocationRequirement,
                SalaryType = job.SalaryType,
                CandidateSavedJobs = new List<SavedJob>(), // Sẽ gán ở controller
                RelatedJobs = new List<RelatedJobViewModel>(),
                SocialLinks = string.IsNullOrWhiteSpace(employer?.User?.SocialLink)
                    ? new List<SocialLink>()
                    : Newtonsoft.Json.JsonConvert.DeserializeObject<List<SocialLink>>(employer.User.SocialLink)
            };

            return viewModel;
        }
    }
}
