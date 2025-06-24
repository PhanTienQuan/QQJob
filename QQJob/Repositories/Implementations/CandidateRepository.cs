using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Repositories.Implementations
{
    public class CandidateRepository : GenericRepository<Candidate>, ICandidateRepository
    {
        public CandidateRepository(QQJobContext context) : base(context)
        {
        }

        public async Task<Candidate?> GetCandidateWithDetailsAsync(string candidateId)
        {
            return await _context.Set<Candidate>()
                .Include(c => c.User)
                .Include(c => c.Educations)
                .Include(c => c.Awards)
                .Include(c => c.Skills)
                .Include(c => c.CandidateExps)
                .Include(c => c.Resume)
                .Include(c => c.SavedJobs)
                .FirstOrDefaultAsync(c => c.CandidateId == candidateId);
        }

        public async Task<Candidate?> GetCandidateWithDetailsByUserIdAsync(string userId)
        {
            return await _context.Set<Candidate>()
                .Include(c => c.User)
                .Include(c => c.Educations)
                .Include(c => c.Awards)
                .Include(c => c.Skills)
                .Include(c => c.CandidateExps)
                .Include(c => c.Resume)
                .FirstOrDefaultAsync(c => c.User.Id == userId);
        }
    }
}
