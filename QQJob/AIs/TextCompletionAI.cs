using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using QQJob.Dtos;

namespace QQJob.AIs
{
    public class TextCompletionAI(Kernel kernel)
    {
        public async Task<string> SummarizeResume(string resumeText)
        {
            var instructions = """
                    Summarize the given resume and extract all key skills, experience, and qualifications.
                    The summary should include all the informarion that a  hiring manager would need to know
                    about the candidate in order to determine if they are a good fit for the job.
                    This summary should be formetted as markdown. Do not return any other text.
                    If the resume does not look like a resume return the text 'N/A'.
                """;
            var agent = CreateAgent("resume-sumary-ai",instructions);
            var aiSummary = await agent.InvokeAsync(resumeText).FirstAsync();
            return aiSummary.Message.Content ?? "N/A";
        }
        public async Task<JobSearchIntent> ExtractJobSearchIntent(string keyword)
        {
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
                    "ExperienceLevel": string
                }

                If the value is not specified, return null or an empty array.
                ONLY return exact JSON. No explanation.
                """;

            var agent = CreateAgent("resume-sumary-ai",prompt);
            var response = await agent.InvokeAsync(keyword).FirstAsync();

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
                        })
                };
        }
    }
}
