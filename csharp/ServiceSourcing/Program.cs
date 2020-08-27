using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.GoogleCloudLogging;

namespace ServiceSourcing
{
    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddEnvironmentVariables("ASPNETCORE_")
            .AddEnvironmentVariables("ServiceSourcing_")
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
        public static ILogger Logger { get; } = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Destructure.ByTransforming<ExpandoObject>(e => new Dictionary<string, object>(e))
            .WriteTo.Async(al =>
            {
                al.Console(LogEventLevel.Debug);
                
                var logFilePath = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "logs.log");
                al.File(logFilePath, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information);

                //if we're on a GCP linux vm, /etc/google-fluentd will exist
                if (Directory.Exists("/etc/google-fluentd"))
                {
                    var config = new GoogleCloudLoggingSinkOptions
                    {
                        UseJsonOutput = true,
                    };
                    al.GoogleCloudLogging(config).MinimumLevel.Information();
                }
            })
            .CreateLogger();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var webHost = WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(Configuration)
                .UseStartup<Startup>()
                .UseSerilog()
                //.UseKestrel()
                //instead of Kestrel, use for IIS for Windows deployment
                .UseIIS(); 
                //.UseUrls("http://localhost:5485/");

            return webHost;
        }

        public static void Main(string[] args)
        {
            Log.Logger = Logger;
            Serilog.Debugging.SelfLog.Enable(TextWriter.Synchronized(Console.Out));

            try
            {
                Console.WriteLine("test output");
                CreateWebHostBuilder(args)
                    .Build()
                    .Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}