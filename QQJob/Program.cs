using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using QQJob.AIs;
using QQJob.Controllers;
using QQJob.Data;
using QQJob.Models;

using QQJob.Repositories.Implementations;
using QQJob.Repositories.Interfaces;
using QQJob.Services;

namespace QQJob
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // DBcontext
            builder.Services.AddDbContext<QQJobContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                options.UseSqlServer(connectionString);
                options.LogTo(_ => { },LogLevel.None);
            });

            var kernelBuilder = Kernel.CreateBuilder().AddOpenAIChatCompletion("gpt-4.1",builder.Configuration.GetSection("OpenAI")["SecretKey"],serviceId: "openai-chat-completion");

            kernelBuilder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));

#pragma warning disable SKEXP0010
            kernelBuilder.Services.AddOpenAIEmbeddingGenerator(
                modelId: "text-embedding-3-small",
                apiKey: builder.Configuration.GetSection("OpenAI")["SecretKey"],
                serviceId: "embedding-generator",
                dimensions: 1536
            );
#pragma warning restore SKEXP0010

            // Now register Kernel in DI
            builder.Services.AddSingleton(sp =>
            {
                return kernelBuilder.Build();
            });
            // Register repositories 
            builder.Services.AddScoped(typeof(IGenericRepository<>),typeof(GenericRepository<>));
            builder.Services.AddScoped<IJobRepository,JobRepository>();
            builder.Services.AddScoped<IAppUserRepository,AppUserRepository>();
            builder.Services.AddScoped<ICandidateRepository,CandidateRepository>();
            builder.Services.AddScoped<IEmployerRepository,EmployerRepository>();
            builder.Services.AddScoped<ISkillRepository,SkillRepository>();
            builder.Services.AddScoped<IApplicationRepository,ApplicationRepository>();
            builder.Services.AddScoped<IChatSessionRepository,ChatSessionRepository>();
            builder.Services.AddScoped<IChatMessageRepository,ChatMessageRepository>();
            builder.Services.AddScoped<INotificationRepository,NotificationRepository>();
            builder.Services.AddScoped<IJobEmbeddingRepository,JobEmbeddingRepository>();
            builder.Services.AddScoped<IJobSimilarityMatrixRepository,JobSimilarityMatrixRepository>();
            builder.Services.AddScoped<IFollowRepository,FollowRepository>();
            builder.Services.AddScoped<CustomRepository,CustomRepository>();
            builder.Services.AddScoped<EmbeddingAI>();
            builder.Services.AddScoped<TextCompletionAI>();


            builder.Services.AddTransient<ISenderEmail,EmailSender>();
            builder.Services.AddTransient<ICloudinaryService,CloudinaryService>();
            builder.Services.AddControllers().AddJsonOptions(options => { options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve; });

            builder.Services.AddIdentity<AppUser,IdentityRole>().AddEntityFrameworkStores<QQJobContext>().AddDefaultTokenProviders();

            builder.Services.AddAuthentication()
                            .AddGoogle(options =>
                            {
                                options.ClientId = builder.Configuration.GetSection("GoogleAuth")["ClientId"];
                                options.ClientSecret = builder.Configuration.GetSection("GoogleAuth")["ClientSecret"];
                                options.AccessDeniedPath = "/Account/OnExternalLoginDenied";
                            }).AddFacebook(facebookOptions =>
                            {
                                facebookOptions.ClientId = builder.Configuration.GetSection("FacebookAuth")["ClientId"];
                                facebookOptions.ClientSecret = builder.Configuration.GetSection("FacebookAuth")["ClientSecret"];
                                facebookOptions.AccessDeniedPath = "/Account/OnExternalLoginDenied";
                            });
            builder.Services.AddSignalR();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/";
            });
            builder.Services.AddAutoMapper(typeof(Program));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHostedService<BackgroundServices>();
            builder.Services.AddHostedService<RelatedJobEmbeddingService>();
            builder.Services.AddHostedService<CleanupBackgroundService>();
            var app = builder.Build();
            Helper.Helper.Initialize(app.Services);
            // Configure the HTTP request pipeline.
            if(!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
            );
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapHub<ChatHub>("/chathub");
            app.Run();
        }
    }
}
