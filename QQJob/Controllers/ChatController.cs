//using Microsoft.AspNetCore.Mvc;
//using Microsoft.SemanticKernel;
//using QQJob.Repositories.Interfaces;

//namespace QQJob.Controllers
//{
//    public class ChatController(IJobRepository jobRepository) : Controller
//    {
//        private readonly IJobRepository _jobRepository = jobRepository;
//        private readonly Kernel kernel;
//        public async Task<string> Chat(string userQuery)
//        {
//            // Define intents and their corresponding methods
//            var intents = new Dictionary<string, Func<string, Task<object>>>
//            {
//                { "find jobs", async query => await _jobRepository.FindJobs(query) },
//                //{ "popular jobs", async query => await GetPopularJobs(query) },
//                //{ "post jobs", async query => await PostJobsHelp(query) },
//                //{ "recommend jobs", async query => await RecommendJobs(query) },
//                //{ "faq", async query => await FAQHelp(query) }
//            };

//            // Detect the intent based on userQuery (basic keyword matching)
//            string detectedIntent = intents.Keys.FirstOrDefault(
//                intent => userQuery.Contains(intent, StringComparison.OrdinalIgnoreCase)
//            );

//            if(detectedIntent == null)
//            {
//                return "Sorry, I couldn't understand your query.";
//            }

//            // Invoke the function for the detected intent
//            var response = await intents[detectedIntent].Invoke(userQuery);

//            // Use the response to build a prompt for OpenAI
//            string prompt = $"User asked: {userQuery}\nSystem response:\n{response}\n\nRefine and elaborate on this response.";

//            // Invoke OpenAI to refine the response
//            var aiResponse = await kernel.InvokePromptAsync(prompt);

//            // Return the final AI-enhanced response
//            return Results.Ok(new
//            {
//                Intent = detectedIntent,
//                OriginalResponse = response,
//                AIResponse = aiResponse
//            });
//        }
//    }
//}
