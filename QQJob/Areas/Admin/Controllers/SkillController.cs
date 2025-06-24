using Microsoft.AspNetCore.Mvc;
using QQJob.Models;
using QQJob.Repositories.Interfaces;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SkillController(ISkillRepository skillRepository):Controller
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
            skill.SkillName = skill.SkillName.Trim();
            var s = await _skillRepository.FindAsync(s => s.SkillName.ToLower() == skill.SkillName.ToLower());
            if(s != null && s.Any())
            {
                ModelState.AddModelError("SkillName","This skill already exists.");
                return View(skill);
            }

            await _skillRepository.AddAsync(skill);
            await _skillRepository.SaveChangesAsync();


            return RedirectToAction("Create");
        }

        [HttpPost]
        public async Task<IActionResult> BulkCreate(string skills)
        {
            if(!ModelState.IsValid)
            {
                return View(skills);
            }

            var skillList = skills.Split(',').Select(s => s.Trim()).ToList();
            foreach(var skillName in skillList)
            {
                var skill = new Skill { SkillName = skillName };
                skill.SkillName = skill.SkillName.Trim();

                // Check if the skill already exists
                var s = await _skillRepository.FindAsync(s => s.SkillName.ToLower() == skill.SkillName.ToLower());
                if(s != null && s.Any())
                {
                    ModelState.AddModelError("SkillName","This skill already exists: " + skill.SkillName);
                    continue; // Skip this skill and continue with the next one
                }
                await _skillRepository.AddAsync(skill);
            }
            await _skillRepository.SaveChangesAsync();

            return RedirectToAction("Create");
        }
        [HttpPost]
        public async Task<JsonResult> Delete(int id)
        {
            var skill = await _skillRepository.GetByIdAsync(id);
            if(skill == null)
            {
                return new JsonResult(new { success = false,message = "Skill not found." });
            }
            _skillRepository.Delete(skill);
            await _skillRepository.SaveChangesAsync();
            return new JsonResult(new { success = true,message = "Success" });
        }
    }
}
