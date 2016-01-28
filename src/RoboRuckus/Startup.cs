using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using RoboRuckus.RuckusCode;

namespace RoboRuckus
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            // Add all SignalR related services to IoC.
            services.AddSignalR();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationEnvironment appEnvironment)
        {
            // Setup configuration sources. May be unnecessary
            var configBuilder = new ConfigurationBuilder().SetBasePath(appEnvironment.ApplicationBasePath).AddJsonFile("config.json");
            configBuilder.AddEnvironmentVariables();
            Configuration = configBuilder.Build();

            serviceHelpers.appEnvironment = appEnvironment;
            loggerFactory.AddConsole();

            // Add Error handling middleware which catches all application specific errors and
            // sends the request to the following path or controller action.
            app.UseExceptionHandler("/Home/Error");
            app.UseStaticFiles();
            app.UseIISPlatformHandler();

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
