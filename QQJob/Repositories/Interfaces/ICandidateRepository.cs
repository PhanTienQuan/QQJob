using QQJob.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QQJob.Repositories.Interfaces
{
    public interface ICandidateRepository : IGenericRepository<Candidate>
    {
        Task<Candidate?> GetCandidateWithDetailsAsync(string candidateId);
        Task<Candidate?> GetCandidateWithDetailsByUserIdAsync(string userId);
        Task<List<Skill>> GetCandidateSkillsAsync(string candidateId);
    }
}
