using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
