using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using QQJob.Dtos;
using QQJob.Repositories.Interfaces;
using ChatMessageContent = Microsoft.SemanticKernel.ChatMessageContent;

namespace QQJob.AIs
{
    public class TextCompletionAI
    {
        private readonly Kernel kernel;
        private readonly ISkillRepository skillRepository;
        private static ChatCompletionAgent? JobSearchAgent { get; set; }
        private static ChatCompletionAgent? SummarizeResumeAgent { get; set; }
        private static ChatCompletionAgent? ChatBotIntentAgent { get; set; }
        private static ChatCompletionAgent? PostJobAgent { get; set; }
        private static ChatCompletionAgent? RankingApplicationAgent { get; set; }
        private readonly IChatCompletionService chatCompletionService;
        private readonly ChatHistorySummarizationReducer chatHistorySummarizationReducer;

        public TextCompletionAI(Kernel kernel,ISkillRepository skillRepository)
        {
            this.kernel = kernel;
            this.skillRepository = skillRepository;
            this.chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
            this.chatHistorySummarizationReducer = new ChatHistorySummarizationReducer(chatCompletionService,targetCount: 2);
        }

        public async Task<ChatBoxSearchIntent> GetChatIntent(List<ChatMessageContent> chatHistory)
        {
            if(ChatBotIntentAgent == null)
            {
                var skills = await skillRepository.GetAllAsync();
                var skillsList = string.Join(",",skills.Select(s => s.SkillName));
                var instructions = $$"""
                        You are a job search AI.
                        Given a user query, extract structured filters.
                        Return JSON with these fields:
                        {   "IntentType": "GREETING" | "THANK_YOU" | "ACKNOWLEDGMENT" | "ACTION" | "UNRELATED" | "INSTRUCTION" | "JOB_SEARCH" | "EMPLOYER_SEARCH",
                            "ActionType" : string, // "POST_JOB" // Only have this field if IntentType is "ACTION".
                            "JobTitle": string,
                            "EmployerName": string,
                            "City": string,
                            "MinSalary": number,
                            "MaxSalary": number,
                            "IncludeSkills": [ string ],
                            "ExcludeSkills": [ string ],
                            "DescriptionKeywords": [ string ],
                            "JobType": string, // Fulltime, Part-time, Part-time, Contract, Temporary, Internship, Volunteer, Freelance, Seasonal, Per Diem, Commission.
                            "ExperienceLevel": string, // Intern, Junior, Middle, Senior, Lead, Manager, Director, Executive.
                            "StrictSearch": boolean, Default is false.
                            "OriginalQuery": string, // The original user query
                            "TopN": number // Number of results to return. Default is 5. 
                        }
                        Normalize the following city name to its official English name if possible.
                        Only consider the following as valid skills: {{skillsList}}. Chose skill that user may likely refer to in that list.
                        If the user mentions any keywords or phrases that should appear in the job description, add them to DescriptionKeywords
                        If the value is not specified, return null or an empty array or specified default value.
                        ONLY return exact JSON. No explanation.
                    """;
                ChatBotIntentAgent = CreateAgent("chat-bot-intent",instructions);
            }

            var response = await ChatBotIntentAgent.InvokeAsync(chatHistory).FirstAsync();

            if((response.Message.Content == null) || response.Message.Content.Trim() == "")
            {
                return new ChatBoxSearchIntent()
                {
                    OriginalQuery = chatHistory.Last().Content // Fix: Ensure the required member 'OriginalQuery' is set
                };
            }

            // Parse JSON to JobSearchIntent
            var intent = JsonConvert.DeserializeObject<ChatBoxSearchIntent>(response.Message.Content);

            return intent ?? new ChatBoxSearchIntent() { OriginalQuery = chatHistory.Last().Content }; // Fix: Ensure fallback also sets 'OriginalQuery'
        }
        public async Task<string> SummarizeResume(string resumeText)
        {
            if(SummarizeResumeAgent == null)
            {
                var instructions = """
                    Summarize the given resume and extract all key skills, experience, and qualifications.
                    The summary should include all the informarion that a  hiring manager would need to know
                    about the candidate in order to determine if they are a good fit for the job.
                    This summary should be formetted as markdown. Do not return any other text.
                    If the resume does not look like a resume return the text 'N/A'.
                """;
                SummarizeResumeAgent = CreateAgent("resume-sumary-ai",instructions);
            }

            var aiSummary = await SummarizeResumeAgent.InvokeAsync(resumeText).FirstAsync();
            return aiSummary.Message.Content ?? "N/A";
        }
        public async Task<JobSearchIntent> ExtractJobSearchIntent(string keyword)
        {
            if(JobSearchAgent == null)
            {
                var skills = await skillRepository.GetAllAsync();
                var skillsList = string.Join(",",skills.Select(s => s.SkillName));
                string prompt = $$"""
                You are a job search AI.
                Given a user query, extract structured filters.
                Return JSON with these fields:
                {
                    "JobTitle": string,
                    "City": string,
                    "MinSalary": number,
                    "MaxSalary": number,
                    "IncludeSkills": [ string ],
                    "ExcludeSkills": [ string ],
                    "JobType": string, // Full-time, Part-time, Contract, Temporary, Internship, Freelance, Volunteer, Seasonal, Per Diem, Commission
                    "ExperienceLevel": string, // Intern, Junior, Middle, Senior, Lead, Manager, Director, Executive. 
                    "StrictSearch": boolean, Default false.  true if the user specifies that only jobs with the exact title, all listed skills. false if partial or loose matches are allowed.
                    "LocationRequirement": string // Remote, Hybrid, Onsite, Flexible, Client Site
                }
                Normalize the following city name to its official English name if possible.
                Only consider the following as valid skills: {{skillsList}}. Chose skill that user may likely refer to in that list.
                Consider user query to use StrictSearch or not.
                If the value is not specified, return null or an empty array.
                ONLY return exact JSON. No explanation.
                """;
                JobSearchAgent = CreateAgent("job-search",prompt);
            }
            var response = await JobSearchAgent.InvokeAsync(keyword).FirstAsync();

            //if(response.Message.Metadata.TryGetValue("Usage",out var usageObj))
            //{
            //    if(usageObj is ChatTokenUsage usage)
            //    {
            //        Console.WriteLine($"Prompt tokens: {usage.InputTokenCount}");
            //        Console.WriteLine($"Completion tokens: {usage.OutputTokenCount}");
            //        Console.WriteLine($"Total tokens: {usage.TotalTokenCount}");
            //    }
            //}

            if((response.Message.Content == null) || response.Message.Content.Trim() == "")
            {
                return new JobSearchIntent();
            }

            // Parse JSON to JobSearchIntent
            var intent = JsonConvert.DeserializeObject<JobSearchIntent>(response.Message.Content);

            return intent ?? new JobSearchIntent();  // fallback
        }
        public async Task<string> ExtractPostJobSession(List<ChatMessageContent> chatHistory,PostJobSession currentSession,List<Delegate> method)
        {
            if(PostJobAgent == null)
            {
                var skills = await skillRepository.GetAllAsync();
                var skillsList = string.Join(",",skills.Select(s => s.SkillName));
                var instruction = $$"""
                        You are a AI assistant to help users post jobs on a recruitment platform.
                        There will be a conversation between the user and you.
                        The user will provide job details step by step.
                        You will ask for missing information if needed.
                        Prompt the user for the next required field.
                        Provide a clear and concise question.
                        Prompt the user to type "Done" when they finish providing all information. and invoke the savePost function.
                        If user say edit or change, you will ask which field they want to edit.
                        If user say cancel, you will end the session and discard all information.
                        JSON: 
                        {
                            "JobTitle": string,
                            "Description": string,
                            "City": string,
                            "ExperienceLevel": string, // Intern, Junior, Middle, Senior, Lead, Manager, Director, Executive.
                            "JobType": string, // Fulltime, Part-time, Contract, Temporary, Internship, Volunteer, Freelance, Seasonal, Per Diem, Commission.
                            "Salary": string, // e.g. "1000-2000 CNY/month"
                            "SalaryType": string, // Salary, Hourly, Project, Commission, Salary + Commission, Hourly / Project, Unpaid / Volunteer.
                            "Skills": string, // Comma separated list of skills.
                            "Opening": number,
                            "LocationRequirement": string, // Remote, Onsite, Hybrid, Flexible, Client Site.
                            "CloseDate": DateTime
                        }
                        Invoke saveProgress function after each field is provided.
                        Only consider the following as valid skills: {{skillsList}}. Chose skill that user may likely refer to in that list.
                        Once all required information is provided, you will return a JSON object with the job details.
                    """;
                PostJobAgent = CreateAgent("post-job",instruction,method,"PrintPostJobSession");
            }

            if(chatHistory.Count > 2)
            {
                var reducedChatHistory = await chatHistorySummarizationReducer.ReduceAsync(chatHistory); // Reduce chat history to avoid too long context
                chatHistory = [.. reducedChatHistory];
            }

            chatHistory.Add(new ChatMessageContent(AuthorRole.System,"Current progress: " + JsonConvert.SerializeObject(currentSession)));
            var response = await PostJobAgent.InvokeAsync(chatHistory).FirstAsync();
            return response.Message.Content;
        }
        public async Task<float> RankApplication(string summary)
        {
            if(RankingApplicationAgent == null)
            {
                var instructions = $"""
                        You are a expert at ranking job applications for specific jobs based on their resume and cover letter.
                        You will be provided with a user prompt that includes a user's id, resume and cover letter
                        as well as the job listing they are applying for in JSON. Your task is to compare the job 
                        listing with the applicant's resume and cover letter and provide a rating for the applicant on how well they 
                        fit that specific job listing. The rating should be a number between 1 and 5, where 5 is the highest rating indicating a perfect or 
                        near perfect match. A rating 3 should be used for applicants that barely meet the requirements of 
                        the job listing, while rating of 1 should be used for applicants that do not meet the requirements at all.
                        Return only the rating number as a float. Do not include any extra text.
                    """;
                RankingApplicationAgent = CreateAgent("application-ranking-agent",instructions);
            }
            var response = await RankingApplicationAgent.InvokeAsync(summary).FirstAsync();
            var content = response.Message.Content?.Trim();
            if(!float.TryParse(content,out var rank))
            {
                // Try to extract a number from a mixed string (fallback)
                var match = System.Text.RegularExpressions.Regex.Match(content ?? "",@"\d+(\.\d+)?");
                if(match.Success)
                    rank = float.Parse(match.Value);
                else
                    throw new Exception($"Invalid ranking result from agent: {content}");
            }
            return rank;
        }
        public ChatCompletionAgent CreateAgent(string agentName,string instructions,List<Delegate>? method = null,string? pluginName = null)
        {
            Console.WriteLine("[Warning] Creating " + agentName);
            var agentKernel = kernel.Clone();
            if(method != null && pluginName != null)
            {
                List<KernelFunction> functions = [];
                foreach(var m in method)
                {
                    functions.Add(agentKernel.CreateFunctionFromMethod(m));
                }
                agentKernel.ImportPluginFromFunctions(pluginName,functions);
            }

            return
                new ChatCompletionAgent()
                {
                    Name = agentName,
                    Instructions = instructions,
                    Kernel = agentKernel,
                    Arguments = new KernelArguments(
                        new OpenAIPromptExecutionSettings()
                        {
                            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                        }),
                };
        }
    }
}
