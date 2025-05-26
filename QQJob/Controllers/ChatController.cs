using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using QQJob.Models;
using QQJob.Repositories.Implementations;
using QQJob.Repositories.Interfaces;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace QQJob.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController:ControllerBase
    {
        private readonly IJobRepository _jobRepository;
        private readonly Kernel _kernel;
        private readonly string InitPrompt;
        private readonly string schemaPath;
        private readonly CustomRepository _customRepository;

        public ChatController(IJobRepository jobRepository,Kernel kernel,IWebHostEnvironment env,CustomRepository customRepository)
        {
            _jobRepository = jobRepository;
            _kernel = kernel;
            _customRepository = customRepository;

            var promptPath = Path.Combine(env.ContentRootPath,"wwwroot","prompts","InitPrompt.txt");
            InitPrompt = System.IO.File.Exists(promptPath)
                ? System.IO.File.ReadAllText(promptPath)
                : "Default fallback prompt if file is missing.";
            schemaPath = Path.Combine(env.ContentRootPath,"wwwroot","prompts","schema.json");
        }

        private async Task<string> GetTableSchemaAsync(string tableName)
        {
            try
            {
                if(!System.IO.File.Exists(schemaPath))
                    throw new Exception("Schema file not found.");

                var jsonData = await System.IO.File.ReadAllTextAsync(schemaPath);
                var schemaDictionary = JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,string>>>(jsonData);

                if(schemaDictionary != null && schemaDictionary.TryGetValue(tableName,out var tableSchema))
                {
                    return string.Join("\n",tableSchema.Select(kv => $"- `{kv.Key}` ({kv.Value})"));
                }
            }
            catch(Exception ex)
            {
                throw new Exception("Error when Deserialize Table schema. " + ex.Message);
            }
            return $"Table `{tableName}` not found in schema.";
        }

        private async Task<string> GenerateInitPrompt(List<string> tableNames)
        {
            string initText = "You are an AI that helps filter job-related data from a relational database.\n";
            initText += "Below are the schemas for the referenced tables:\n\n";

            foreach(var table in tableNames)
            {
                string tableSchema = await GetTableSchemaAsync(table);
                initText += $"### Table: `{table}`\n{tableSchema}\n\n";
            }

            initText += @"
            Based on the user's query, generate a valid C# LINQ predicate for filtering across these tables.
            - Use navigation properties where possible (e.g., Job.Employer.CompanySize or Application.Candidate.FullName).
            - ONLY return a C# predicate string without any explanation or comments.
            - Example: Application => Application.Job.Title.Contains(""Java"") && Application.Candidate.Description.Contains(""Remote"")";

            return initText.Trim();
        }

        public async Task<LambdaExpression> GeneratePredicate(List<string> tableNames,string messageContent)
        {
            try
            {
                string initPrompt = await GenerateInitPrompt(tableNames);
                string primaryTable = tableNames.First();

                string predicatePrompt = $@"

                {initPrompt}

                User Message: ""{messageContent}""
                Return only the LINQ predicate targeting the `{primaryTable}` table.";

                var predicateResult = await _kernel.InvokePromptAsync(predicatePrompt);
                string predicateString = predicateResult.GetValue<string>()?.Trim();

                Type primaryEntityType = Type.GetType($"QQJob.Models.{primaryTable}");
                if(primaryEntityType == null)
                    throw new Exception($"Entity type QQJob.Models.{primaryTable} not found.");

                var parameter = Expression.Parameter(primaryEntityType,primaryEntityType.Name);

                return DynamicExpressionParser.ParseLambda(
                    new ParsingConfig(),
                    new[] { parameter },
                    typeof(bool),
                    predicateString
                );
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[Predicate Error] {ex.Message}");
                var fallbackParameter = Expression.Parameter(typeof(Job),"job");
                return Expression.Lambda<Func<Job,bool>>(Expression.Constant(true),fallbackParameter);
            }
        }

        private async Task<List<String>> PredictTableNames(string messageContent)
        {
            string tablePredictionPrompt = $@"
            Analyze the user query and determine which database tables should be used.
            Possible tables: Job, Candidate, Employer, Application, Skill, Award, Education, Follow, SavedJob, ViewJobHistory.

            Message: ""{messageContent}""

            Return ONLY the table names as a comma-separated list (e.g., ""Job, Application""). Do NOT add explanations.";

            // Get table names from the AI model
            var tableResult = await _kernel.InvokePromptAsync(tablePredictionPrompt);
            string tableNamesString = tableResult.GetValue<string>()?.Trim();

            // Split by comma and clean up whitespace
            var tableNames = tableNamesString?.Split(',').Select(name => name.Trim()).ToList() ?? new List<string>();

            var validTables = new Dictionary<string,Type>(StringComparer.OrdinalIgnoreCase)
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

            // Map table names to types
            var selectedTableTypes = new List<String>();
            foreach(var tableName in tableNames)
            {
                if(validTables.TryGetValue(tableName,out Type tableType))
                {
                    selectedTableTypes.Add(tableType.Name);
                }
                else
                {
                    Console.WriteLine($"Unknown table: {tableName}. Ignoring.");
                }
            }

            // If no valid tables are found, default to 'Job'
            if(selectedTableTypes.Count == 0)
            {
                Console.WriteLine("No valid tables found. Defaulting to 'Job'.");
                selectedTableTypes.Add(typeof(Job).Name);
            }

            return selectedTableTypes;
        }


        [HttpPost]
        public async Task<IActionResult> Chat([FromBody] Message request)
        {
            if(request == null)
            {
                return BadRequest(new { Errors = new { General = "Request body is missing." } });
            }

            var errors = new Dictionary<string,string>();
            if(string.IsNullOrWhiteSpace(request.Sender)) errors["Sender"] = "Sender cannot be empty.";
            if(string.IsNullOrWhiteSpace(request.Receiver)) errors["Receiver"] = "Receiver cannot be empty.";
            if(string.IsNullOrWhiteSpace(request.Content)) errors["Content"] = "Content cannot be empty.";

            if(errors.Count > 0)
            {
                return BadRequest(new { Errors = errors });
            }

            string messageContent = request.Content;


            // Step 1: Get table name
            Console.WriteLine("Predict table");
            List<string> tableNames = await PredictTableNames(messageContent);

            Console.WriteLine("Generate Predicate");
            // Step 2: Generate a query predicate for filtering
            var predicate = await GeneratePredicate(tableNames,messageContent);

            // Step 3: Execute query dynamically based on predicted table
            object result = await _customRepository.QueryDatabase(tableNames,predicate);

            return Ok(new { Table = tableNames,Response = result,Predicate = predicate.ToString() });
        }


        public class Message
        {
            public string Sender { get; set; }
            public string Receiver { get; set; }
            public string Content { get; set; }
        }
    }
}
