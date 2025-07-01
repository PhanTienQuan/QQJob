using QQJob.Models;
using QQJob.Models.Enum;
using System.Linq.Expressions;

namespace QQJob.Repositories.Interfaces
{
    public interface IFollowRepository:IGenericRepository<Follow>
    {
        Task<bool> IsFollowedAsync(string employerId,string candidateId);
        Task<(List<Follow> follows, PagingModel pagingModel)> GetFollowsAsync(int currentPage,int pageSize,Expression<Func<Follow,bool>>? predicate);
    }
}
