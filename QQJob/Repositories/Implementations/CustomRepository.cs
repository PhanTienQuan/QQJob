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
        public async Task<object> QueryDatabase(List<string> tableNames,LambdaExpression predicate,int limit)
        {
            try
            {
                if(tableNames == null || tableNames.Count == 0)
                {
                    throw new Exception("No table names provided.");
                }

                // Get the first entity type dynamically from the table name
                Type entityType = Type.GetType($"QQJob.Models.{tableNames[0]}");
                if(entityType == null)
                {
                    throw new Exception($"Table type '{tableNames[0]}' not found.");
                }

                // Get the corresponding DbSet property dynamically (Assuming plural naming convention)
                var dbSetProperty = _context.GetType().GetProperty($"{tableNames[0]}s");
                if(dbSetProperty == null)
                {
                    throw new Exception($"DbSet for '{tableNames[0]}' not found in the database context.");
                }

                // Get the DbSet for the entity type dynamically using reflection
                var dbSet = dbSetProperty.GetValue(_context);
                var queryableSet = dbSet as IQueryable;
                if(queryableSet == null)
                {
                    throw new Exception($"DbSet for '{tableNames[0]}' is not IQueryable.");
                }

                // Now use reflection to create the generic Set<T> method call dynamically
                var setMethod = _context.GetType().GetMethods()
                                .Where(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0)
                                .FirstOrDefault();

                if(setMethod == null)
                {
                    throw new Exception("Set method not found.");
                }

                // Make the Set<T> method with the correct entityType
                var genericSetMethod = setMethod.MakeGenericMethod(entityType);

                // Invoke the Set<T> method to get the DbSet
                var dbSetResult = genericSetMethod.Invoke(_context,null) as IQueryable;

                if(dbSetResult == null)
                {
                    throw new Exception($"Failed to invoke Set<T> for '{entityType.Name}'.");
                }

                // Apply the dynamically generated predicate
                var filteredQuery = dbSetResult.Provider.CreateQuery(
                    Expression.Call(
                        typeof(Queryable),
                        "Where",
                        new Type[] { entityType },
                        dbSetResult.Expression,
                        Expression.Quote(predicate)
                    )
                );

                // Execute the query asynchronously
                var result = await Task.Run(() => ((IQueryable<object>)filteredQuery).ToList());

                return result.Take(limit);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Database Query Error: {ex.Message}");
                return new { Error = $"Failed to retrieve data for {string.Join(", ",tableNames)}" };
            }
        }
    }
}
