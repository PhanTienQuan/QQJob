using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QQJob.Areas.Admin.ViewModels;

namespace QQJob.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoleController(RoleManager<IdentityRole> roleManager):Controller
    {
        public async Task<IActionResult> Index()
        {
            List<IdentityRole> roles = await roleManager.Roles.ToListAsync();
            return View(roles);
        }

        [HttpGet]
        public IActionResult CreateRole()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(CreateRoleViewModel roleModel)
        {
            if(ModelState.IsValid)
            {
                // Check if the role already exists
                bool roleExists = await roleManager.RoleExistsAsync(roleModel?.RoleName);
                if(roleExists)
                {
                    ModelState.AddModelError("","Role Already Exists");
                }
                else
                {
                    // Create the role
                    // We just need to specify a unique role name to create a new role
                    IdentityRole identityRole = new IdentityRole
                    {
                        Name = roleModel?.RoleName
                    };

                    // Saves the role in the underlying AspNetRoles table
                    IdentityResult result = await roleManager.CreateAsync(identityRole);

                    if(result.Succeeded)
                    {
                        TempData["Message"] = "Create role Successful";
                        return RedirectToAction("Index","Role");
                    }

                    foreach(IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("",error.Description);
                    }
                }
            }
            return View(roleModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(string roleId)
        {
            //First Get the role information from the database
            IdentityRole role = await roleManager.FindByIdAsync(roleId);
            if(role == null)
            {
                // Handle the scenario when the role is not found
                return View("Error");
            }

            //Populate the EditRoleViewModel from the data retrived from the database
            var model = new EditRoleViewModel
            {
                Id = role.Id,
                RoleName = role.Name
                // You can add other properties here if needed
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditRole(EditRoleViewModel model)
        {
            if(ModelState.IsValid)
            {
                var role = await roleManager.FindByIdAsync(model.Id);
                if(role == null)
                {
                    // Handle the scenario when the role is not found
                    ViewBag.ErrorMessage = $"Role with Id = {model.Id} cannot be found";
                    return View("NotFound");
                }
                else
                {
                    role.Name = model.RoleName;
                    // Update other properties if needed

                    var result = await roleManager.UpdateAsync(role);
                    if(result.Succeeded)
                    {
                        TempData["Message"] = "Update role successful";
                        return RedirectToAction("Index"); // Redirect to the roles list
                    }

                    foreach(var error in result.Errors)
                    {
                        ModelState.AddModelError("",error.Description);
                    }

                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(string roleId)
        {
            var role = await roleManager.FindByIdAsync(roleId);
            if(role == null)
            {
                // Role not found, handle accordingly
                ViewBag.ErrorMessage = $"Role with Id = {roleId} cannot be found";
                return View("NotFound");
            }

            var result = await roleManager.DeleteAsync(role);
            if(result.Succeeded)
            {
                TempData["Message"] = "Delete role successful!";
                // Role deletion successful
                return RedirectToAction("Index"); // Redirect to the roles list page
            }

            foreach(var error in result.Errors)
            {
                ModelState.AddModelError("",error.Description);
            }

            // If we reach here, something went wrong, return to the view
            return View("Index",await roleManager.Roles.ToListAsync());
        }
    }
}
