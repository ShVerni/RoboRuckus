using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoboRuckus.RuckusCode;
using System;
using RoboRuckus.Hubs;

namespace RoboRuckus
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        // Construction for DI of configuration.
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            // Add all SignalR related services.
            services.AddSignalR();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IConfiguration configuration)
        {
            // Add console to log
            loggerFactory.AddConsole();

            serviceHelpers.rootPath = env.ContentRootPath;

            // Add Error handling middleware which catches all application specific errors and
            // sends the request to the following path or controller action.
            app.UseExceptionHandler("/Shared/Error");
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{player?}",
                    defaults: new { controller = "Player", action = "Index" }
                );

                routes.MapRoute(
                    name: "Robot",
                    template: "{controller}/{action}/{bot?}"
                );
            });

            // Add fileserver to app.
            app.UseFileServer();

            // Add SignalR to app.
            app.UseSignalR(routes =>
            {
                routes.MapHub<playerHub>("/playerHub");
            });

            // Parse command line arguments.
            string options = configuration.GetValue<string>("options");
            if (options != null)
            {
                string[] args = options.Split(",");
                foreach (string arg in args)
                {
                    switch (arg)
                    {
                        case "botless":
                            gameStatus.botless = true;
                            Console.WriteLine("Botless mode enabled");
                            break;
                        case "edgecontrol":
                            gameStatus.edgeControl = true;
                            Console.WriteLine("Edge control enabled.");
                            break;
                    }
                }
            }
        }
    }
}
