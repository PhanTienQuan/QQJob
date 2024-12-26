using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class JobRepository : GenericRepository<Job>, IJobRepository
    {
        public JobRepository(QQJobContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Job>> GetJobsAsync(int page = 1, int pageSize = 10)
        {
            int skip = (page - 1) * pageSize;
            return await _context.Set<Job>()
                .OrderBy(j => j.Title)
                .Skip(skip)
                .Take(pageSize)
                .Include(j => j.Skills)
                .ToListAsync();
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
                .Where(j => j.Title.Contains(keyword) || j.JobDescription.Contains(keyword))
                .ToListAsync();
        }
        public async Task<Job> GetByIdAsync(int id)
        {
            return await _context.Set<Job>().Include(j => j.Skills).Include(j => j.Applications).FirstOrDefaultAsync(j => j.JobId == id);
        }

        public async Task<IEnumerable<Job>> FindJobs(string query)
        {
            var skills = query.Replace("find jobs", "").Trim().Split(',');

            // Query the database for jobs with matching skills
            var jobs = await _context.Jobs
                .Include(j => j.Skills) // Ensure skills are included in the query
                .Where(j => j.Skills.Any(s => skills.Any(skill => s.SkillName.Contains(skill.Trim()))))
                .ToListAsync();

            return jobs;
        }
    }
}
