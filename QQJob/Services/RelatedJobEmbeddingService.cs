using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using QQJob.AIs;
namespace QQJob.Services
{
    public class RelatedJobEmbeddingService(Kernel kernel,IServiceProvider serviceProvider):BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                using var scope = serviceProvider.CreateScope();
                var embeddingAI = scope.ServiceProvider.GetRequiredService<EmbeddingAI>();

                await embeddingAI.GenerateEmbeddings();
                await embeddingAI.ComputeSimilarityMatrix();

                await Task.Delay(TimeSpan.FromHours(24),stoppingToken);
            }
        }
    }
}
