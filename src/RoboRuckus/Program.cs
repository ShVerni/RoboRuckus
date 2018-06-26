using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
                    var env = hostingContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.SetBasePath(env.ContentRootPath);
                    config.AddEnvironmentVariables();
                    config.AddCommandLine(args);
                })
                .UseStartup<Startup>()
                .Build();
            return host;
        }
    }
}

/*
public static void Main(string[] args)
{
    var host = new WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .UseUrls("http://*:8082")
        .UseIISIntegration()
        .UseStartup<Startup>()
        .Build();

    // Used to parse args.
    if (args.Length > 0)
    {
        foreach (string arg in args)
        {
            switch (arg.ToLower())
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
        }*/