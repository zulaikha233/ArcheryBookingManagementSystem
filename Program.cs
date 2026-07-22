using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace ArcheryAlley
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ArcheryAlley.Models.ArcheryAlleyDBContext>();
                    // Database is fresh & empty - Migrate() will create ALL tables cleanly from scratch
                    context.Database.Migrate();

                    try
                    {
                        context.Database.ExecuteSqlRaw(@"
                            IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ClassRegistrations_Students_StudentId')
                            BEGIN
                                ALTER TABLE archerysystem.ClassRegistrations DROP CONSTRAINT FK_ClassRegistrations_Students_StudentId;
                                ALTER TABLE archerysystem.ClassRegistrations ADD CONSTRAINT FK_ClassRegistrations_Archers_StudentId FOREIGN KEY (StudentId) REFERENCES archerysystem.Archers(StudentId);
                            END
                        ");
                    }
                    catch (Exception sqlEx)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(sqlEx, "Failed to execute raw SQL for FK correction.");
                    }
                    try
                    {
                        if (!context.Roles.Any())
                        {
                            context.Roles.Add(new ArcheryAlley.Models.Roles
                            {
                                EmpId = "ADMIN001",
                                EmpName = "Super Admin",
                                RoleType = true, // Admin
                                Password = "AdminPassword123!",
                                Gender = "M",
                                Email = "admin@archeryalley.com",
                                PhoneNumber = "-",
                                EContactName = "-",
                                EContactNumber = "-",
                                ProfilePicture = null
                            });
                            context.SaveChanges();
                        }
                    }
                    catch (Exception seedEx)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(seedEx, "Failed to seed admin.");
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                    try { System.IO.File.WriteAllText(System.IO.Path.Combine(host.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath, "migration_error.txt"), ex.ToString()); } catch { }
                }
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>

            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders(); // Clear default providers if you want to customize
                    logging.AddConsole(); // Add console logging
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
