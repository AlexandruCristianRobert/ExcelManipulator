using Duende.IdentityModel;
using ExcelManipulator.Data;
using ExcelManipulator.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Duende.IdentityServer;
using Microsoft.AspNetCore.Authentication.Google;

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
                })
               .AddGoogle("Google", options =>
               {
                   options.SignInScheme = IdentityConstants.ExternalScheme; 
                   options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
                   options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
                   options.Scope.Add("email");
                   options.Scope.Add("profile");
                   options.SaveTokens = true;
               });

            builder.Services.ConfigureApplicationCookie(o =>
            {
                o.Cookie.SameSite = SameSiteMode.None; 
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

            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseAuthorization();

            app.MapRazorPages();
            app.Run();

        }
    }
}
