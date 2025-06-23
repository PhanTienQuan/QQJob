using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using QQJob.Dtos;
using QQJob.Repositories.Interfaces;

namespace QQJob.AIs
{
    public class TextCompletionAI(Kernel kernel,ISkillRepository skillRepository)
    {
        private ChatCompletionAgent? JobSearchAgent { get; set; }
        private ChatCompletionAgent? SummarizeResumeAgent { get; set; }
        private ChatCompletionAgent? ChatBotIntentAgent { get; set; }
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
                    {"IntentType": "GREETING" | "THANK_YOU" | "ACKNOWLEDGMENT" | "ACTION" | "UNRELATED" | "INSTRUCTION" | "JOB_SEARCH" | "EMPLOYER_SEARCH",
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
                        "StrictSearch": boolean, Default is false. true if the user specifies ALL listed skills are required (AND). false if ANY skill is sufficient (OR).
                        "OriginalQuery": string, // The original user query
                        "TopN": number // Number of results to return. Default is 5. 
                    }
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
                    "JobType": string,
                    "ExperienceLevel": string,
                    "StrictSearch": boolean // true if the user specifies ALL listed skills are required (AND). false if ANY skill is sufficient (OR).
                }
                For ExperienceLevel use value: Intern, Junior, Middle, Senior, Lead, Manager, Director, Executive. 
                Only consider the following as valid skills: {{skillsList}}. Chose skill that user may likely refer to in that list.
                If the value is not specified, return null or an empty array.
                ONLY return exact JSON. No explanation.
                """;
                JobSearchAgent = CreateAgent("job-search",prompt);
            }
            var response = await JobSearchAgent.InvokeAsync(keyword).FirstAsync();

            if((response.Message.Content == null) || response.Message.Content.Trim() == "")
            {
                return new JobSearchIntent();
            }

            // Parse JSON to JobSearchIntent
            var intent = JsonConvert.DeserializeObject<JobSearchIntent>(response.Message.Content);

            return intent ?? new JobSearchIntent();  // fallback
        }
        public ChatCompletionAgent CreateAgent(string agentName,string instructions,List<Delegate>? method = null)
        {
            var agentKernel = kernel.Clone();
            if(method != null)
            {
                List<KernelFunction> functions = [];
                foreach(var m in method)
                {
                    functions.Add(agentKernel.CreateFunctionFromMethod(m));
                }
                agentKernel.ImportPluginFromFunctions(agentName,functions);
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
