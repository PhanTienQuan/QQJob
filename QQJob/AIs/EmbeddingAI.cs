using Microsoft.Extensions.AI;
using Newtonsoft.Json;
using QQJob.Models;
using QQJob.Models.Enum;
using QQJob.Repositories.Interfaces;
namespace QQJob.AIs
{
    public class EmbeddingAI(IJobEmbeddingRepository jobEmbeddingRepository,IJobSimilarityMatrixRepository jobSimilarityMatrixRepository,IJobRepository jobRepository)
    {
        public async Task GenerateEmbeddings(IEmbeddingGenerator<string,Embedding<float>> embeddingGen)
        {
            var jobs = await jobRepository.GetAllAsync();
            var embeddings = await jobEmbeddingRepository.GetAllAsync();
            var jobsWithoutEmbedding = jobs
                .Where(j =>
                {
                    var jobUpdatedDate = (j.UpdateAt == DateTime.MinValue ? j.PostDate : j.UpdateAt);

                    var embedding = embeddings.FirstOrDefault(e => e.JobId == j.JobId);

                    return j.Status == Status.Approved
                           && (embedding == null || embedding.UpdatedAt < jobUpdatedDate);
                })
                .ToList();



            foreach(var job in jobsWithoutEmbedding)
            {
                var skillsText = job.Skills != null
                    ? string.Join(",",job.Skills.Select(s => s.SkillName))
                    : string.Empty;
                var text = $"{job.JobTitle}. {job.Description}. Skills: {skillsText}.";
                var vector = await embeddingGen.GenerateVectorAsync(text);

                var embeddingJson = JsonConvert.SerializeObject(vector.ToArray());

                await jobEmbeddingRepository.AddAsync(new JobEmbedding
                {
                    JobId = job.JobId,
                    Embedding = embeddingJson,
                    UpdatedAt = DateTime.Now
                });
            }

            await jobEmbeddingRepository.SaveChangesAsync();
        }
        public async Task ComputeSimilarityMatrix()
        {
            var allEmbeddings = await jobEmbeddingRepository.GetAllAsync();
            await jobSimilarityMatrixRepository.DeleteAllAsync();
            await jobSimilarityMatrixRepository.SaveChangesAsync();

            foreach(var jobA in allEmbeddings)
            {
                var vecA = JsonConvert.DeserializeObject<float[]>(jobA.Embedding);

                var similarities = new List<(int JobId2, double Score)>();

                foreach(var jobB in allEmbeddings)
                {
                    if(jobA.JobId == jobB.JobId) continue;

                    var vecB = JsonConvert.DeserializeObject<float[]>(jobB.Embedding);
                    var score = CosineSimilarity(vecA,vecB);

                    similarities.Add((jobB.JobId, score));
                }

                var topSimilar = similarities.OrderByDescending(x => x.Score).Take(10);

                foreach(var sim in topSimilar)
                {
                    await jobSimilarityMatrixRepository.AddAsync(new JobSimilarityMatrix
                    {
                        JobId1 = jobA.JobId,
                        JobId2 = sim.JobId2,
                        SimilarityScore = sim.Score
                    });
                }

                await jobSimilarityMatrixRepository.SaveChangesAsync();
            }
        }
        public double CosineSimilarity(float[] vecA,float[] vecB)
        {
            double dot = 0.0, magA = 0.0, magB = 0.0;
            for(int i = 0;i < vecA.Length;i++)
            {
                dot += vecA[i] * vecB[i];
                magA += vecA[i] * vecA[i];
                magB += vecB[i] * vecB[i];
            }
            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }
    }
}
