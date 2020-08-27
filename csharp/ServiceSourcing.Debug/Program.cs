using System.IO;
using System.Reflection;
using ServiceSourcing.Controllers;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ServiceSourcing.Debug
{
    class Program
    {
        public class Startup : ServiceSourcing.Startup
        {
            public Startup(IConfiguration configuration, IHostingEnvironment environment) : base(configuration, environment) {}

            public override void ConfigureServices(IServiceCollection services)
            {
                services
                    .AddMvcCore()
                    .AddApplicationPart(typeof(NewCustomerController).GetTypeInfo().Assembly) // ATTACH CONTROLLERS HERE
                    .AddControllersAsServices()
                    .SetCompatibilityVersion(CompatibilityVersion.Latest);
                // ATTACH SERVICES HERE
            }
        }

        static IWebHost GetDependencyInjectionContainer(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables("ServiceSourcing_")
                .Build(); 
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();
            Log.Logger = logger;
            var webHost = WebHost
                .CreateDefaultBuilder(args)
                .UseConfiguration(config)
                .UseStartup<Startup>()
                .Build();

            return webHost;
        }

        static void Main(string[] args)
        {
            var container = GetDependencyInjectionContainer(args);

            var demoController = container.Services.GetService<NewCustomerController>();
            
            var result = demoController.Create(null);
        }
    }
}
