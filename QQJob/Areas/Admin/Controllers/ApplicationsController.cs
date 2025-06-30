using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QQJob.AIs;
using QQJob.Data;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ApplicationsController:Controller
    {
        private readonly QQJobContext _context;

        public ApplicationsController(QQJobContext context)
        {
            _context = context;
        }

        // GET: Admin/Applications
        public async Task<IActionResult> Index()
        {
            var qQJobContext = _context.Applications.Include(a => a.Candidate).Include(a => a.Job);
            return View(await qQJobContext.ToListAsync());
        }

        // GET: Admin/Applications/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(m => m.ApplicationId == id);
            if(application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // GET: Admin/Applications/Create
        public IActionResult Create()
        {
            ViewData["CandidateId"] = new SelectList(_context.Candidates,"CandidateId","CandidateId");
            ViewData["JobId"] = new SelectList(_context.Jobs,"JobId","JobTitle");
            return View();
        }

        // POST: Admin/Applications/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ApplicationId,JobId,CandidateId,ApplicationDate,CoverLetter,Status")] Application application,
            [FromServices] TextCompletionAI textCompletionAI,
            [FromServices] ICandidateRepository candidateRepository,
            [FromServices] IJobRepository jobRepository
        )
        {
            ModelState.Remove("Job");
            ModelState.Remove("Candidate");
            if(ModelState.IsValid)
            {
                // Get candidate and job details for AI ranking
                var candidate = await candidateRepository.GetCandidateWithDetailsAsync(application.CandidateId);
                var job = await jobRepository.GetJobDetail(j => j.JobId == application.JobId);

                // Prepare job detail for AI
                var jobDetail = new
                {
                    job.JobId,
                    job.JobTitle,
                    job.Description,
                    Skills = job.Skills.Select(s => s.SkillName).ToList(),
                    job.JobType,
                    job.Salary,
                    job.SalaryType,
                    job.City,
                    job.LocationRequirement,
                    job.ExperienceLevel
                };

                // Get candidate resume summary if available
                var aiSummary = candidate?.Resume?.AiSumary ?? "";

                // Compose input for AI ranking
                var aiInput = JsonConvert.SerializeObject(new
                {
                    jobDetail,
                    AiSumary = aiSummary,
                    application.JobId,
                    application.CandidateId
                });

                // Get AI ranking
                application.AIRanking = await textCompletionAI.RankApplication(aiInput);

                _context.Add(application);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Create successfull";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CandidateId"] = new SelectList(_context.Candidates,"CandidateId","CandidateId",application.CandidateId);
            ViewData["JobId"] = new SelectList(_context.Jobs,"JobId","JobTitle",application.JobId);
            return View(application);
        }

        // GET: Admin/Applications/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications.FindAsync(id);
            if(application == null)
            {
                return NotFound();
            }
            ViewData["CandidateId"] = new SelectList(_context.Candidates,"CandidateId","CandidateId",application.CandidateId);
            ViewData["JobId"] = new SelectList(_context.Jobs,"JobId","JobTitle",application.JobId);
            return View(application);
        }

        // POST: Admin/Applications/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,[Bind("ApplicationId,JobId,CandidateId,ApplicationDate,CoverLetter,Status,AIRanking")] Application application)
        {
            if(id != application.ApplicationId)
            {
                return NotFound();
            }

            // Detach navigation properties to avoid model state errors
            ModelState.Remove("Job");
            ModelState.Remove("Candidate");

            if(ModelState.IsValid)
            {
                try
                {
                    // Attach only the foreign key values, not the navigation properties
                    _context.Entry(application).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
                catch(DbUpdateConcurrencyException)
                {
                    if(!ApplicationExists(application.ApplicationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["Message"] = "Update successfull";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CandidateId"] = new SelectList(_context.Candidates,"CandidateId","CandidateId",application.CandidateId);
            ViewData["JobId"] = new SelectList(_context.Jobs,"JobId","JobTitle",application.JobId);
            return View(application);
        }

        // GET: Admin/Applications/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if(id == null)
            {
                return NotFound();
            }

            var application = await _context.Applications
                .Include(a => a.Candidate)
                .Include(a => a.Job)
                .FirstOrDefaultAsync(m => m.ApplicationId == id);
            if(application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // POST: Admin/Applications/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var application = await _context.Applications.FindAsync(id);
            if(application != null)
            {
                _context.Applications.Remove(application);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ApplicationExists(int id)
        {
            return _context.Applications.Any(e => e.ApplicationId == id);
        }
    }
}
