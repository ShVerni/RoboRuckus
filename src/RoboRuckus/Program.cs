using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoboRuckus.Hubs;
using RoboRuckus.RuckusCode;
using System;

namespace RoboRuckus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create application builder.
            WebApplicationBuilder builder = builder = WebApplication.CreateBuilder(args);

            // Store application path for reference.
            serviceHelpers.rootPath = builder.Environment.ContentRootPath;

            // Add services to the container.
            builder.Services.AddControllersWithViews(options => options.EnableEndpointRouting = false); // Endpoint routing is disabled to enable MVC routing
            builder.Services.AddSignalR();
            builder.Services.AddSignalR().AddNewtonsoftJsonProtocol();
           

            // Enable logging
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();

            // Get any command line arguments.
            string options = builder.Configuration.GetValue<string>("options");

            // Build application.
            WebApplication app = builder.Build();

            // Add error handling.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.Urls.Add("http://*:8082");
            // Add MVC to app.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{player?}",
                    defaults: new { controller = "Player", action = "Index" }
                );

                routes.MapRoute(
                    name: "Robot",
                    template: "Bot/{action}/{bot?}"
                );
            });

            // Enable Static files and routing.
            app.UseStaticFiles();
            app.UseRouting();

            // Add fileserver to app.
            app.UseFileServer();

            // Add SignalR to app.
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<playerHub>("/playerHub");
            });

            // Parse command line arguments.
            if (options != null)
            {
                string[] option = options.Split(",");
                foreach (string arg in option)
                {
                    switch (arg)
                    {
                        case "botless":
                            gameStatus.botless = true;
                            Console.WriteLine("Botless mode enabled");
                            break;
                    }
                }
            }
            // Run application.
            app.Run();
        }
    }
}