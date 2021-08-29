using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoboRuckus.RuckusCode;
using System;
using RoboRuckus.Hubs;

namespace RoboRuckus
{
    public class Startup
    {
        // private readonly IConfiguration _configuration;

        // Construction for DI of configuration.
        public Startup()//IConfiguration configuration)
        {
           // _configuration = configuration;
        }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable MVC routing by disabling Endpoint routing.
            services.AddControllers(options => options.EnableEndpointRouting = false);

            // Add MVC services to the services container.
            services.AddMvc();

            // Add all SignalR related services.
            services.AddSignalR();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
        {
            serviceHelpers.rootPath = env.ContentRootPath;

            // Enable routing.
            app.UseRouting();

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
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<playerHub>("/playerHub");
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
