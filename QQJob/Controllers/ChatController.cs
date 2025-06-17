using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using QQJob.Dtos;
using QQJob.Models;
using QQJob.Repositories.Implementations;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace QQJob.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController(Kernel kernel,IWebHostEnvironment env,CustomRepository customRepository,IMapper mapper,UserManager<AppUser> userManager):ControllerBase
    {
        private readonly Kernel _kernel = kernel;
        private readonly string _schemaPath = Path.Combine(env.ContentRootPath,"wwwroot","prompts","schema.json");
        private readonly CustomRepository _customRepository = customRepository;
        private Dictionary<string,Dictionary<string,string>>? _cachedSchema;
        private readonly IMapper _mapper = mapper;
        private static ChatCompletionAgent? generatePredicateAgent = null;

        private static readonly Dictionary<string,(Type Model, Type Dto)> TableDtoMap = new()
        {
            ["Job"] = (typeof(Job), typeof(JobDto)),
            ["Candidate"] = (typeof(Candidate), typeof(CandidateDto)),
            ["Employer"] = (typeof(Employer), typeof(EmployerDto)),
            ["Application"] = (typeof(Application), typeof(ApplicationDto)),
            ["Skill"] = (typeof(Skill), typeof(SkillDto))
        };

        public ChatCompletionAgent CreateAgent(string agentName,string instructions,Delegate? method = null)
        {
            Kernel agentKernel = _kernel.Clone();
            // Create plug-in from a static function
            if(method != null)
            {
                var functionFromMethod = agentKernel.CreateFunctionFromMethod(method);
                agentKernel.ImportPluginFromFunctions(agentName,[functionFromMethod]);
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

        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] Message request)
        {
            if(request == null || string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { Error = "Empty message content." });

            var userMessage = request.Content;

            var sessionHistory = request.History ?? new();

            List<ChatMessageContent> chatHistory = new List<ChatMessageContent>();
            if(sessionHistory.Any())
            {
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
                var intentResult = await ClassifyUserIntentAsync(userMessage);
                switch(intentResult)
                {
                    case "GREETING":
                    case "THANK_YOU":
                    case "ACKNOWLEDGMENT":
                    case "ACTION":
                    case "UNRELATED":
                    default:
                        return Ok(new { Message = await GenerateFriendlyReplyAsync(intentResult,userMessage),Intent = intentResult });

                    case "INSTRUCTION":
                        return Ok(new { Message = await GenerateInstructions(intentResult,userMessage,roles == null ? "anonymous" : roles.First()) });
                    case "QUERY":
                        break;
                }
                //get posible tables used in the user query
                var tableNames = await PredictTablesAsync(userMessage);
                var firstTable = tableNames.FirstOrDefault();

                //generate limit from user query if stated, if not default to 3
                var limitResult = await ExtractLimitAsync(userMessage);

                //generate predicate
                var predicateResult = await GeneratePredicateAsync(tableNames,chatHistory);

                //get data from the first table with the generated predicate
                var result = await GetDataFromPredicate(predicateResult,firstTable,limitResult);

                if(result == null)
                {
                    return Ok(new
                    {
                        Message = "Sorry, I couldn't find anything that matches your request at the moment."
                    });
                }

                var html = await GenerateHtmlResponse(userMessage,result);
                return Ok(new { Message = html,Result = result,Limit = limitResult,Predicate = predicateResult.ToString() });
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[Chat Error] {ex.Message}, {ex.StackTrace}");
                return StatusCode(500,new { Error = "Something went wrong while processing your request." });
            }
        }

        private async Task<List<string>> PredictTablesAsync(string userMessage)
        {
            var prompt = $@"
                Analyze the following user query and identify relevant database tables.
                Available tables: Job, Candidate, Employer, Application, Skill, Award, Education, Follow, SavedJob, ViewJobHistory, CandidateExp.

                User Query:
                ""{userMessage}""

                Return only the relevant table names as a comma-separated list, without any explanation.";

            var result = await _kernel.InvokePromptAsync(prompt);
            var tableList = result.GetValue<string>()?.Trim();


            var knownTables = new Dictionary<string,Type>(StringComparer.OrdinalIgnoreCase)
            {
                { "Application", typeof(Application) },
                { "AppUser", typeof(AppUser) },
                { "Award", typeof(Award) },
                { "Candidate", typeof(Candidate) },
                { "CandidateExp", typeof(CandidateExp) },
                { "Education", typeof(Education) },
                { "Employer", typeof(Employer) },
                { "Follow", typeof(Follow) },
                { "Job", typeof(Job) },
                { "SavedJob", typeof(SavedJob) },
                { "Skill", typeof(Skill) },
                { "ViewJobHistory", typeof(ViewJobHistory) }
            };

            var selectedTables = tableList?
                .Split(',',StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .Where(knownTables.ContainsKey)
                .ToList() ?? new List<string>();

            if(!selectedTables.Any())
            {
                Console.WriteLine("[Warning] No valid tables found. Defaulting to 'Job'.");
                selectedTables.Add(nameof(Job));
            }

            return selectedTables;
        }
        private async Task<string?> ClassifyUserIntentAsync(string message)
        {
            var prompt = $@"
            You are a classifier for a job recruitment assistant chatbot. Given a user message, classify the intent into one of these categories:

            1. GREETING — The user is saying hello, hi, hey, good morning, etc.
            2. THANK_YOU — The user is saying thank you or showing appreciation.
            3. ACKNOWLEDGMENT — The user is saying ok, got it, cool, sure, fine, etc.
            4. QUERY — The user wants to view or filter data (e.g. jobs, candidates).
            5. INSTRUCTION — The user asks how to do something (e.g. post a job).
            6. ACTION — The user asks to perform a change (e.g. delete saved jobs).
            7. UNRELATED — The message is not about recruitment or polite interaction.

            Examples:
            - ""hi there"" → GREETING
            - ""thanks a lot"" → THANK_YOU
            - ""ok"" → ACKNOWLEDGMENT
            - ""got it, thanks"" → ACKNOWLEDGMENT
            - ""list React jobs in Hanoi"" → QUERY
            - ""how do I post a job?"" → INSTRUCTION
            - ""apply to this job for me"" → ACTION
            - ""tell me a joke"" → UNRELATED

            Message:
            {message}

            Return only one of the following:
            GREETING, THANK_YOU, ACKNOWLEDGMENT, QUERY, INSTRUCTION, ACTION, UNRELATED
            ";

            var result = await _kernel.InvokePromptAsync(prompt);
            return result.GetValue<string>()?.Trim().ToUpperInvariant();
        }
        private async Task<int> ExtractLimitAsync(string userMessage)
        {
            var prompt = $@"
                From the following user request:
                ""{userMessage}""

                Extract the number of desired results.
                - If unspecified, return 3 by default.
                - Only return the number (integer). Do not include any explanation.";

            var result = await _kernel.InvokePromptAsync(prompt);
            return int.TryParse(result.GetValue<string>()?.Trim(),out var limit) ? limit : 3;
        }
        private async Task<LambdaExpression> GeneratePredicateAsync(List<string> tableNames,List<ChatMessageContent> chatHistory)
        {
            try
            {
                var initPrompt = await BuildInitPromptAsync(tableNames);
                var primaryTable = tableNames.First();
                tableNames.Remove(primaryTable);

                var finalPrompt = $@"
                    {initPrompt}

                    Now, based on the user's message, **generate a C# LINQ predicate (lambda expression) to filter the records** (read-only query).
                    Follow these guidelines:
                    - **Use only filtering logic.** Do NOT produce code that updates, deletes, or modifies data.
                    - **Prioritize the user's query and context** (including chat history) when forming the predicate.
                    - **Use navigation properties if relevant** (e.g., `Job.Employer.CompanySize`) to filter by related data.
                    - **Return only the C# predicate string** (the lambda expression) **with no extra explanation** or commentary.

                    **Example format:**  
                    `Application => Application.Job.Title.Contains(""Java"") && Application.Candidate.Description.Contains(""Remote"")`
                    - Return the LINQ predicate for the `{primaryTable}` table only.
                    - Secondary table needed to include in `{primaryTable}`: `{tableNames.ToString()}`
                    - If the message asks about related data (like Employer of a Job), return a filter that narrows the base table (e.g., Job).
                    - Do not return navigation entity as the predicate root — always filter the main entity.
                    - If the message is asking for a specific item (""the first"" or ""top""), generate a predicate that would match that item.
                    Return the LINQ predicate for the `{primaryTable}` table only.";


                generatePredicateAgent ??= CreateAgent("generatePredicateAgent","You are an assistant that generates LINQ predicates for a given table using schema and user context.");
                chatHistory.Insert(0,new ChatMessageContent(AuthorRole.System,finalPrompt));
                var response = await generatePredicateAgent.InvokeAsync(chatHistory).FirstAsync();
                var predicateString = response.Message.Content ?? "";

                var primaryType = Type.GetType($"QQJob.Models.{primaryTable}");
                if(primaryType == null)
                    throw new InvalidOperationException($"Entity type QQJob.Models.{primaryTable} not found.");

                var parameter = Expression.Parameter(primaryType,primaryType.Name);
                return DynamicExpressionParser.ParseLambda(new ParsingConfig(),[parameter],typeof(bool),predicateString);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[Predicate Error] {ex.Message}");
                var fallbackParam = Expression.Parameter(typeof(Job),"job");
                return Expression.Lambda<Func<Job,bool>>(Expression.Constant(true),fallbackParam);
            }
        }
        private async Task<string> BuildInitPromptAsync(List<string> tableNames)
        {
            // Intro and purpose
            string intro = "You are an AI assistant that helps users filter job-related data from a relational database.\n";
            intro += "Here are the schemas of the referenced tables:\n";

            // Include each table's schema in a formatted way
            foreach(var table in tableNames)
            {
                var schema = await GetTableSchemaAsync(table);
                intro += $"### Table: `{table}`\n{schema}\n\n";  // Provide table schema in markdown format
            }

            return intro.Trim();
        }
        private async Task<string> GetTableSchemaAsync(string tableName)
        {
            try
            {
                var schemaDictionary = await GetSchemaAsync();

                if(schemaDictionary != null && schemaDictionary.TryGetValue(tableName,out var tableSchema))
                {
                    return string.Join("\n",tableSchema.Select(kv => $"- `{kv.Key}` ({kv.Value})"));
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[Schema Error] {ex.Message}");
            }

            return $"Table `{tableName}` not found in schema.";
        }
        public async Task<Dictionary<string,Dictionary<string,string>>?> GetSchemaAsync()
        {
            if(_cachedSchema != null) return _cachedSchema;
            if(!System.IO.File.Exists(_schemaPath))
                throw new FileNotFoundException("Schema file not found.");

            var json = await System.IO.File.ReadAllTextAsync(_schemaPath);
            _cachedSchema = JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,string>>?>(json);
            return _cachedSchema;
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

            var result = await _kernel.InvokePromptAsync(prompt);
            return result.GetValue<string>()?.Trim() ?? "Okay.";
        }
        private async Task<string> GenerateHtmlResponse(string userMessage,object queryResult)
        {
            var json = JsonConvert.SerializeObject(queryResult,new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            if(json == "[]" || string.IsNullOrWhiteSpace(json) || json == "{}")
                return "<p>Sorry, I couldn’t find any matching results.</p>";

            string prompt = $@"
                You're a helpful assistant on a recruitment platform. Convert this JSON data into a compact, readable HTML response **for a small chat window UI**.

                User asked:
                {userMessage}

                Here is the structured data (JSON):
                {json}

                Instructions:
                - Start the response with a friendly phrase like ""Here’s what I found for you:"".
                - If the data is about a Candidate, include only name, experience, skills, and slug.
                - If about a Job, show job title, location, salary, required experience, slug.
                - If about an Application, show candidate name, job title, and apply date.
                - Format results using <div> blocks for each item (job, candidate, etc.).
                - Use short labels: <strong> for field names, all inline.
                - Use <strong> only — do not use <ul>, <li>, <p> tags.
                - Always include clickable links if 'slug' is present for that data set, don't add slug to the html if slug field is null or empty.
                - Do NOT return broken links (e.g., <a href='/job/'>) or placeholder slugs.
                - Do not add social/profile links.
                - Do not add Employer website link.
                - Omit null, empty, or system/system-generated fields.
                - Never fabricate fields or values not found in the JSON input.
                - Style for clarity: group related information in clean visual sections.
                - Format all date fields as dd/mm/yyyy.
                - Return ONLY HTML (no explanations, no comments, no backticks).
                ";

            var result = await _kernel.InvokePromptAsync(prompt);
            return result.GetValue<string>()?.Trim() ?? "<p>Sorry, I couldn't find any useful information to display.</p>";
        }
        private async Task<object?> GetDataFromPredicate(LambdaExpression lambdaExpression,string? firstTable,int limitResult)
        {
            if(firstTable == null || !TableDtoMap.TryGetValue(firstTable,out var types))
            {
                return null;
            }

            var genericPredicateType = typeof(Expression<>).MakeGenericType(
                typeof(Func<,>).MakeGenericType(types.Model,typeof(bool))
            );
            var delegateType = typeof(Func<,>).MakeGenericType(types.Model,typeof(bool));

            var lambda = Expression.Lambda(delegateType,lambdaExpression.Body,lambdaExpression.Parameters);

            var method = typeof(CustomRepository).GetMethod("QueryDatabase")!.MakeGenericMethod(types.Model,types.Dto);

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            var task = (Task<object>)method.Invoke(_customRepository,
            [
                lambda,
                limitResult,
                _mapper
            ]);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var dtoResult = await task;
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return dtoResult;
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
                "anonymous" => new[] {
                    "browse jobs", "browse employers", "register", "log in"
                },
                _ => Array.Empty<string>()
            };

            var filtered = InstructionResponses
                .Where(kvp => roleCapabilities.Contains(kvp.Key,StringComparer.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key,kvp => kvp.Value,StringComparer.OrdinalIgnoreCase);

            var capabilityList = string.Join("\n",filtered.Select(kvp => $"- {kvp.Key}"));

            var systemPrompt = $"""
            You are an assistant for a recruitment website.
            User said:
            "{userMessage}"
            
            The user's intent is categorized as **{intent}**
            Based on the user’s question, return the best-matching capability from the list below:
            {capabilityList}

            Only return the capability key that best fits the user's intent.
            If nothing matches, return NONE.
            """;

            var response = await _kernel.InvokePromptAsync(systemPrompt);
            var result = response.GetValue<string>()?.Trim();
            if(result != null && InstructionResponses.TryGetValue(result,out var finalResult))
            {
                return finalResult;
            }

            return "Sorry, you have to login first before you can do that action.";
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
