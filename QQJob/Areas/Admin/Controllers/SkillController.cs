using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SkillController(ISkillRepository skillRepository) : Controller
    {
        private readonly ISkillRepository _skillRepository = skillRepository;
        public async Task<IActionResult> Index()
        {
            var skill = await _skillRepository.GetAllAsync();
            return View(skill);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Skill skill)
        {
            if(!ModelState.IsValid)
            {
                return View(skill);
            }

            await _skillRepository.AddAsync(skill);
            await _skillRepository.SaveChangesAsync();


            return RedirectToAction("Index");
        }
    }
}
