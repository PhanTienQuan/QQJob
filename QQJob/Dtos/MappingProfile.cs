using AutoMapper;
using QQJob.Models;

namespace QQJob.Dtos
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {
            CreateMap<AppUser,UserDto>();
            CreateMap<Skill,SkillDto>()
                .ForMember(dest => dest.SkillName,opt => opt.MapFrom(src => src.SkillName));
            CreateMap<Job,JobDto>();
            CreateMap<Candidate,CandidateDto>();
            CreateMap<Employer,EmployerDto>();
            CreateMap<Application,ApplicationDto>()
                .ForMember(dest => dest.JobTitle,opt => opt.MapFrom(src => src.Job.JobTitle))
                .ForMember(dest => dest.AppliedAt,opt => opt.MapFrom(src => src.ApplicationDate));
        }
    }
}
