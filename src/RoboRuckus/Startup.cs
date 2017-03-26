using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoboRuckus.RuckusCode;
using System;
using System.IO;

namespace RoboRuckus
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls("http://localhost:8082")
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();
            
            // Used to run game without physical bots.
            if (args.Length >0 && args[0] == "botless")
            {
                gameStatus.noBots = true;
                Console.WriteLine("Botless Mode");
            }

            host.Run();
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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Setup configuration sources. May be unnecessary
            var builder = new ConfigurationBuilder().SetBasePath(env.ContentRootPath).AddJsonFile("appsettings.json").AddEnvironmentVariables(); ;
            Configuration = builder.Build();
            loggerFactory.AddConsole();

            serviceHelpers.rootPath = env.ContentRootPath;

            // Add Error handling middleware which catches all application specific errors and
            // sends the request to the following path or controller action.
            app.UseExceptionHandler("/Home/Error");
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

            app.UseFileServer();
            app.UseSignalR();
        }
    }
}
