using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;

namespace RoboRuckus
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            IWebHost host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls("http://*:8082")
                .UseIISIntegration()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    IWebHostEnvironment env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.SetBasePath(env.ContentRootPath);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();
            return host;
        }
    }
}