using QQJob.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QQJob.Repositories.Interfaces
{
    public interface ISkillRepository : IGenericRepository<Skill>
    {
        Task<List<Skill>> GetAllSkillsAsync();
    }
}
