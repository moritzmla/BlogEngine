using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BlogCoreEngine.Data.AccountData;
using BlogCoreEngine.Data.ApplicationData;
using BlogCoreEngine.Models.DataModels;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlogCoreEngine
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();
            try
            {
                using(var scope = host.Services.CreateScope())
                {
                    using(var accountContext = scope.ServiceProvider.GetService<AccountDbContext>())
                    {
                        await accountContext.Database.EnsureCreatedAsync();
                        using(var roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>())
                        {
                            if (!await roleManager.RoleExistsAsync("Administrator"))
                            {
                                await roleManager.CreateAsync(new IdentityRole("Administrator"));
                            }

                            if (!await roleManager.RoleExistsAsync("Writer"))
                            {
                                await roleManager.CreateAsync(new IdentityRole("Writer"));
                            }
                        }

                        using(var userManager = scope.ServiceProvider.GetService<UserManager<ApplicationUser>>())
                        {
                            if (accountContext.Users.Count() <= 0)
                            {
                                ApplicationUser adminUser = new ApplicationUser();
                                adminUser.UserName = "Admin";
                                adminUser.Email = "default@default.com";

                                await userManager.CreateAsync(adminUser, "adminPassword");
                                await userManager.AddToRoleAsync(adminUser, "Administrator");
                            }
                        }
                        await accountContext.SaveChangesAsync();
                    }

                    using (var applicationContext = scope.ServiceProvider.GetService<ApplicationDbContext>())
                    {
                        await applicationContext.Database.EnsureCreatedAsync();
                        if (applicationContext.Settings.Count() <= 0)
                        {
                            applicationContext.Settings.Add(new SettingDataModel
                            {
                                Title = "BlogCoreEngine",
                                Logo = System.IO.File.ReadAllBytes(".//wwwroot//images//Logo.png")
                            });
                        }
                        await applicationContext.SaveChangesAsync();
                    }
                }
            } catch { }

             await host.RunAsync();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
