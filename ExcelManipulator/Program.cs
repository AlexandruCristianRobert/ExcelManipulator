using ExcelManipulator.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExcelManipulator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddDbContext<ApplicationDbContext>(opt =>
                opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services
                .AddIdentity<User, IdentityRole>(opts =>
                {
                    opts.Password.RequiredLength = 8;
                    opts.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddIdentityServer()
                .AddAspNetIdentity<User>()       
                .AddInMemoryIdentityResources(Config.Identity) 
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryClients(Config.Clients)
                .AddDeveloperSigningCredential();           

            builder.Services.AddAuthentication()
                .AddJwtBearer("Bearer", opts =>
                {
                    opts.Authority = builder.Configuration["ISAuthority"];   
                    opts.TokenValidationParameters.ValidateAudience = false; 
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("ExcelAccess",
                    policy => policy.RequireClaim("permission", "excel"));
                options.AddPolicy("AdminCreation",
                    p => p.RequireClaim("permission", "admin-creation"));
            });
            builder.Services.AddRazorPages();

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();
            app.Run();

        }
    }
}
