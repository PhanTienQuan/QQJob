using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using QQJob.Data;
using System.Linq.Expressions;
namespace QQJob.Repositories.Implementations
{
    public class CustomRepository
    {
        private QQJobContext _context;
        public CustomRepository(QQJobContext context)
        {
            _context = context;
        }

        public async Task<object> QueryDatabase<TModel, TDto>(Expression<Func<TModel,bool>> predicate,int limit,IMapper mapper)
    where TModel : class
    where TDto : class
        {
            var query = _context.Set<TModel>().AsQueryable();

            var asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m => m.Name == "AsNoTracking" && m.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(TModel));

            query = asNoTrackingMethod.Invoke(null,new object[] { query }) as IQueryable<TModel>;

            var navProps = predicate.ToString()
                .Split(new[] { '.','"' },StringSplitOptions.RemoveEmptyEntries)
                .Where(s => char.IsUpper(s[0]))
                .Distinct();

            var entityType = _context.Model.FindEntityType(typeof(TModel));
            var navigationProps = entityType.GetNavigations().Select(n => n.Name).ToHashSet();

            foreach(var prop in navProps)
            {
                if(navigationProps.Contains(prop))
                {
                    query = query.Include(prop);
                }
            }

            return await query.Where(predicate)
                              .ProjectTo<TDto>(mapper.ConfigurationProvider)
                              .Take(limit)
                              .ToListAsync();
        }
    }
}
