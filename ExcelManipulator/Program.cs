using Duende.IdentityModel;
using ExcelManipulator.Data;
using ExcelManipulator.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ExcelManipulator
{
    public class Program
    {
        public static async Task Main(string[] args)
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

            builder.Services.AddScoped<IUserService, UserService>();

            builder.Services.AddIdentityServer()
                .AddAspNetIdentity<User>()       
                .AddInMemoryIdentityResources(Config.Identity) 
                .AddInMemoryApiScopes(Config.ApiScopes)
                .AddInMemoryClients(Config.Clients)
                .AddProfileService<ProfileService>();

            builder.Services.AddAuthentication()
                .AddJwtBearer("Bearer", opts =>
                {
                    opts.Authority = builder.Configuration["ISAuthority"];
                    opts.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        RoleClaimType = JwtClaimTypes.Role,   
                        NameClaimType = JwtClaimTypes.Name    
                    };
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

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();           
            }
            await IdentitySeeder.SeedAsync(app.Services);

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
