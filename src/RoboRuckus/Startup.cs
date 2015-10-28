using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

using RoboRuckus.RuckusCode;

namespace RoboRuckus
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnvironment)
        {
            // Setup configuration sources.
            var configBuilder = new ConfigurationBuilder().SetBasePath(appEnvironment.ApplicationBasePath).AddJsonFile("config.json");
            configBuilder.AddEnvironmentVariables();
            Configuration = configBuilder.Build();

            serviceHelpers.appEnvironment = appEnvironment;
        }

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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerfactory)
        {
            // Configure the HTTP request pipeline.
            // Add the console logger.
            loggerfactory.AddConsole();

            // Add the following to the request pipeline only in development environment.
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Add Error handling middleware which catches all application specific errors and
                // sends the request to the following path or controller action.
                app.UseExceptionHandler("/Home/Error");
            }

            // Add static files to the request pipeline.
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
            // Set up SignalR
            app.UseFileServer();
            app.UseSignalR();
        }
    }
}
