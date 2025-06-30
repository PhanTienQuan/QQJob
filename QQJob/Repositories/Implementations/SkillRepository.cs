using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QQJob.Repositories.Implementations
{
    public class SkillRepository : GenericRepository<Skill>, ISkillRepository
    {
        public SkillRepository(QQJobContext context) : base(context)
        {
        }

        public async Task<List<Skill>> GetAllSkillsAsync()
        {
            return await _context.Set<Skill>().ToListAsync();
        }
    }
}
