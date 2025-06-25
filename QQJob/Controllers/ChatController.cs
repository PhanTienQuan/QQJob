using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using QQJob.AIs;
using QQJob.Dtos;
using QQJob.Models;
using QQJob.Repositories.Interfaces;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Text;

namespace QQJob.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController(
        Kernel kernel,
        UserManager<AppUser> userManager,
        IJobEmbeddingRepository jobEmbeddingRepository,
        EmbeddingAI embeddingAI,
        TextCompletionAI textCompletionAI,
        IJobRepository jobRepository,
        IEmployerRepository employerRepository):ControllerBase
    {
        const int MAX_MESSAGE_LENGTH = 500;
        const int MAX_HISTORY = 20;

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] Message request)
        {
            if(request == null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { Error = "Empty message content." });

            if(request.Content.Length > MAX_MESSAGE_LENGTH)
                return BadRequest(new { Error = "Message too long." });

            var logined = await userManager.GetUserAsync(User);
            if(request.Sender == null || string.IsNullOrWhiteSpace(request.Sender) || logined == null)
                return Ok(new
                {
                    Message = "Sorry,You have to login before using our chat bot."
                });

            request.Content = SafeHtml(request.Content.Trim());

            var userMessage = request.Content;
            var sessionHistory = request.History ?? [];
            List<ChatMessageContent> chatHistory = [];
            if(sessionHistory.Count != 0)
            {
                if(sessionHistory.Count > MAX_HISTORY)
                    sessionHistory = [.. sessionHistory.TakeLast(MAX_HISTORY)];
                foreach(var content in sessionHistory)
                {
                    var role = content.Role == "Bot" ? AuthorRole.Assistant : AuthorRole.User;
                    chatHistory.Add(new(role,content.Content));
                }

            }

            var user = await userManager.FindByIdAsync(request.Sender);
            IList<string>? roles = null;
            if(user != null) roles = await userManager.GetRolesAsync(user);

            try
            {
                var chatIntent = await textCompletionAI.GetChatIntent(chatHistory);
                Console.WriteLine($"[Chat Intent] {JsonConvert.SerializeObject(chatIntent)}");
                switch(chatIntent.IntentType)
                {
                    case "GREETING":
                    case "THANK_YOU":
                    case "ACKNOWLEDGMENT":
                    case "UNRELATED":
                    case "ACTION":
                    //switch(chatIntent.ActionType)
                    //{
                    //    case "POST_JOB":
                    //        var response = await textCompletionAI.ExtractPostJobSession(chatHistory,session,[PrintPostJobSession,SaveProgress]);
                    //        return Ok(new { Message = response });
                    //    default:
                    //        return Ok(new { Message = "Sorry, that action is not supported in chat yet." });
                    //}
                    default:
                        return Ok(new { Message = await GenerateFriendlyReplyAsync(chatIntent.IntentType,userMessage) });

                    case "INSTRUCTION":
                        return Ok(new { Message = await GenerateInstructions(chatIntent.IntentType,userMessage,roles == null ? "anonymous" : roles.First()) });
                    case "JOB_SEARCH":
                        return Ok(new
                        {
                            Message = await SearchJobs(chatIntent)
                        });
                    case "EMPLOYER_SEARCH":
                        return Ok(new
                        {
                            Message = await SearchEmployer(chatIntent)
                        });
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[Chat Error] {ex.Message}, {ex.StackTrace}");
                return StatusCode(500,new { Error = "Something went wrong while processing your request." });
            }
        }
        public async Task<string?> SearchJobs(ChatBoxSearchIntent intent)
        {
            var jobs = await jobRepository.ChatBoxJobsSearchAsync(intent);
            if(jobs == null || jobs.Count == 0)
            {
                return "Sorry, I couldn’t find any matching jobs.";
            }

            var jobEmbeddings = await jobEmbeddingRepository.GetAllAsync();
            var vector = await embeddingAI.GetTextEmbbeding(intent.OriginalQuery);
            var rankedJobs = jobs.Select(j =>
            {
                var embeddingRow = jobEmbeddings.FirstOrDefault(e => e.JobId == j.JobId);
                var embeddingVector = embeddingRow != null ? JsonConvert.DeserializeObject<float[]>(embeddingRow.Embedding) ?? [] : [];
                var similarity = vector == null ? 1.0f : embeddingAI.CosineSimilarity(vector,embeddingVector);

                return new { Job = j,Similarity = similarity };
            })
            .OrderByDescending(j => j.Similarity)
            .Take(intent.TopN)
            .ToList();

            return GenerateJobHtml(rankedJobs.Select(r => r.Job).ToList());
        }
        public string GenerateJobHtml(List<Job> jobs)
        {
            if(jobs == null || jobs.Count == 0)
                return "<div class='chat-card'><p>No jobs found for your search.</p></div>";

            var sb = new StringBuilder();
            sb.Append($"<div class='chat-card'><p>I found {jobs.Count} job{(jobs.Count > 1 ? "s" : "")} based on your query:</p></div>");
            foreach(var job in jobs)
            {
                sb.Append($@"
                    <div class='chat-card'>
                        <p>
                            <strong>Title: <a href='/Jobs/Detail/{job.JobId}/{job.Slug}' target='_blank'>{job.JobTitle}</a></strong><br>
                            <a href='/Home/EmployerDetail/{job.Employer.EmployerId}/{job.Employer.User.Slug}'>Posted by {job.Employer?.User.FullName}</a><br>
                            City: {job.City} <br>Salary: {job.Salary}<br>Job Type: {job.JobType}<br>
                        </p>
                    </div>");
            }
            return sb.ToString();
        }
        public async Task<string?> SearchEmployer(ChatBoxSearchIntent intent)
        {
            var employers = await employerRepository.ChatBoxSearchEmployersAsync(intent);

            return GenerateEmployerHtml(employers.FirstOrDefault());
        }
        public string GenerateEmployerHtml(Employer employer)
        {
            if(employer == null)
                return "<div class='chat-card'><p>Employer not found.</p></div>";

            var sb = new StringBuilder();
            sb.Append($@"
                <div class='chat-card'>
                    <p>
                        <strong>
                            <a href='/Home/EmployerDetail/{employer.EmployerId}?{employer.User?.Slug}' target='_blank'>
                                {employer.User?.FullName ?? "Unknown Employer"}
                            </a>
                        </strong><br>
                        Field: {(employer.CompanyField ?? "Not specified")}<br>
                        Jobs posted: {employer.Jobs?.Count ?? 0}<br>
                        Email: {employer.User?.Email ?? "N/A"}
                    </p>
                </div>
            ");
            return sb.ToString();
        }
        private async Task<string> GenerateFriendlyReplyAsync(string? intent,string userMessage)
        {
            var prompt = $@"
            You're a friendly chatbot that assists users on a job recruitment platform.

            User said:
            ""{userMessage}""

            The user's intent is categorized as **{intent}**.

            Write a short, natural response:
            - Be warm and conversational.
            - Respond appropriately to the intent.
            - Do NOT make up actions or fake data.
            - Keep the tone casual but helpful.
            - Keep it under 2 short sentences.
            - Return only the reply text, no explanation.";

            var result = await kernel.InvokePromptAsync(prompt);
            return result.GetValue<string>()?.Trim() ?? "Okay.";
        }
        private async Task<string?> GenerateInstructions(string intent,string userMessage,string role)
        {
            Dictionary<string,string> InstructionResponses = new(StringComparer.OrdinalIgnoreCase)
            {
                // Candidate
                ["search jobs"] = "You can search for jobs by going to the Jobs page. Use the search bar and filters like location, experience, and skills.",
                ["view jobs"] = "Visit the Jobs section to view all available listings. You can apply filters to narrow your search.",
                ["apply for jobs"] = "To apply, click on a job listing, then click 'Apply'. Upload your resume and fill out any required fields.",
                ["save jobs"] = "Click the bookmark icon next to any job listing to save it for later in your Saved Jobs.",
                ["follow employers"] = "Click 'Follow' on an employer profile to receive updates about their new postings.",
                ["edit resume"] = "Go to your Profile > Resume to upload or edit your resume.",
                ["edit profile"] = "Click your avatar > Profile > Edit to update your personal information.",

                // Employer
                ["post new jobs"] = "Go to Dashboard > Post a New Job. Fill in job details and publish.",
                ["manage existing jobs"] = "In your dashboard, go to Manage Jobs to edit, close, or view stats on your postings.",
                ["view applicants"] = "Navigate to Applicants tab under each job to see who has applied.",
                ["view candidate profiles"] = "Click on a candidate from the applicants list to view their profile.",
                ["edit company profile"] = "Go to Company Profile in your dashboard to update your company information.",

                // Anonymous
                ["browse jobs"] = "Visit the Jobs page to explore current job listings. Register or log in to apply.",
                ["browse employers"] = "Check out employer profiles under the Employers tab.",
                ["register"] = "Click 'Sign Up' in the top-right corner to register as a candidate or employer.",
                ["log in"] = "Click 'Log In' in the top-right corner and enter your credentials.",
            };

            var roleCapabilities = role.ToLower() switch
            {
                "candidate" => [
                    "search jobs", "view jobs", "apply for jobs", "save jobs", "follow employers",
                    "edit resume", "edit profile"
                ],
                "employer" => [
                    "post new jobs", "manage existing jobs", "view applicants",
                    "view candidate profiles", "edit company profile", "search jobs",
                    "view jobs"
                ],
                "anonymous" => [
                    "browse jobs", "browse employers", "register", "log in"
                ],
                _ => Array.Empty<string>()
            };

            var filtered = InstructionResponses.Where(kvp => roleCapabilities.Contains(kvp.Key,StringComparer.OrdinalIgnoreCase))
                        .ToDictionary(kvp => kvp.Key,kvp => kvp.Value,StringComparer.OrdinalIgnoreCase);

            var capabilityList = string.Join("\n",filtered.Select(kvp => $"- {kvp.Key}: {kvp.Value}"));

            var systemPrompt = $"""
                You are an assistant for a recruitment website.
                The user's message is:
                "{userMessage}"

                User role: {role}
                User intent: {intent}
                The following are possible actions and instructions for this role:
                {capabilityList}

                1. Identify the most relevant capability based on the user’s message and intent.
                2. Return a single, concise, natural-sounding instruction for the user, customized to their query.
                3. If no suitable action is found, reply: "You can't do that action as a {role}."

                IMPORTANT: Output only the final instruction.
                """;

            var response = await kernel.InvokePromptAsync(systemPrompt);
            var result = response.GetValue<string>()?.Trim();

            return result;
        }
        private static string SafeHtml(string input)
        {
            return WebUtility.HtmlEncode(input);
        }
        public class Message
        {
            public string? Sender { get; set; }
            public required string Content { get; set; }
            public List<ChatLine>? History { get; set; }
        }
        public class ChatLine
        {
            public required string Role { get; set; }
            public required string Content { get; set; }
        }
    }
}
