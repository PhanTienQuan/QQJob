using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using QQJob.Repositories.Interfaces;
using QQJob.ViewModels;
using System.Security.Claims;

namespace QQJob.Controllers
{
    [Authorize]
    public class ApplicationController(IApplicationRepository applicationRepository):Controller
    {
        public async Task<IActionResult> Detail(int id)
        {
            var application = await applicationRepository.GetApplicationById(id);

            string referer = Request.Headers.Referer.ToString();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(currentUserId == null)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Login first!",type = "waring" });
                return Redirect(referer);
            }
            else if(application == null)
            {
                TempData["Message"] = JsonConvert.SerializeObject(new { message = "Application does not exist!",type = "waring" });
                return Redirect(referer);
            }
            else if(application.CandidateId != currentUserId && application.Job.EmployerId != currentUserId)
            {
                return Redirect(referer);
            }

            var applicationDetailViewModel = new ApplicationDetailViewModel
            {
                ApplicationId = application.ApplicationId,
                ApplicationDate = application.ApplicationDate,
                CandidateId = application.CandidateId,
                CandidateName = application.Candidate.User.FullName,
                CoverLetter = application.CoverLetter ?? "",
                EmployerId = application.Job.EmployerId,
                EmployerName = application.Job.Employer.User.FullName,
                JobId = application.JobId,
                JobTitle = application.Job.JobTitle,
                Resume = application.Candidate.Resume,
                Status = application.Status,
                CandidateAvatar = application.Candidate.User.Avatar,
                AIRanking = application.AIRanking ?? 0.0f,
                Phone = application.Candidate.User.PhoneNumber ?? "Not Specify",
                JobSlug = application.Job.Slug,
                CandidateSlug = application.Candidate.User.Slug
            };


            return View(applicationDetailViewModel);
        }
    }
}
