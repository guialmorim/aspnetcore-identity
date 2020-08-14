using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApp.Identity.Claims;
using WebApp.Identity.Helpers;
using WebApp.Identity.Models;

namespace WebApp.Identity
{
    public class Startup
    {

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<UserDbContext>(opt => opt.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), 
                sql => sql.MigrationsAssembly(migrationAssembly)));

            services.AddIdentity<User, IdentityRole>(options =>
            {
                // user needs to confirm email to access application
                options.SignIn.RequireConfirmedEmail = true;

                // options for password validation..
                // options.Password.RequireDigit = false;
                // options.Password.RequireNonAlphanumeric = false;
                // options.Password.RequireUppercase = false;
                // and more...

                // blocks the user after 4 login attempts
                options.Lockout.MaxFailedAccessAttempts = 4;

                // a new user can be locked out
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<UserDbContext>()
            .AddDefaultTokenProviders()
            .AddPasswordValidator<MyPasswordValidator<User>>();

            services.AddScoped<IUserClaimsPrincipalFactory<User>, MyUserClaimsPrincipalFactory>();

            services.Configure<DataProtectionTokenProviderOptions>(
                    options => options.TokenLifespan = TimeSpan.FromHours(3));

            services.ConfigureApplicationCookie(options => options.LoginPath = "/User/Login");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/User/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=User}/{action=Index}/{id?}");
            });
        }
    }
}
