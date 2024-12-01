using QQJob.Models;

namespace QQJob.Repositories.Interfaces
{
    public interface ICandidateRepository : IGenericRepository<Candidate>
    {
        Task<Candidate?> GetCandidateWithDetailsAsync(string candidateId);
    }
}
