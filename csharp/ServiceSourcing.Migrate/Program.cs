using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using ServiceSourcing.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Serilog;
using Serilog.Events;
using SQLitePCL;

namespace ServiceSourcing.Migrate
{
    class Program
    {
        public class Startup : ServiceSourcing.Startup
        {
            public string ConnectionString { get; set; }
            public string Environment { get; set; }

            public Startup()
            {
                var builder = new ConfigurationBuilder()
                    .AddEnvironmentVariables("ASPNETCORE_");

                var config = builder.Build();
                Environment = config["ENVIRONMENT"].ToLower() ?? "";

                if (new[] {"development", "production"}.Contains(Environment))
                {
                    Configuration = new ConfigurationBuilder()
                        .AddJsonFile($"appsettings.{Environment}.json")
                        .Build();
                    ConnectionString = Configuration["NpgsqlConnectionString"];
                }
                else
                {
                    throw new NotImplementedException("null/unknown environment");
                }
            }

            public override void ConfigureServices(IServiceCollection services)
            {
                services
                    .AddDbContext<DataContext>(options =>
                    {
                        options.UseNpgsql(ConnectionString, _options =>
                        {
                            switch (Environment)
                            {
                                case "development":
                                    _options.RemoteCertificateValidationCallback(DataContext.ValidateRemoteCertDev);
                                    break;
                                case "production":
                                    //TODO: add client certs later
                                    //_options.ProvideClientCertificatesCallback(DataContext.ProvideClientCertificateProd);
                                    _options.RemoteCertificateValidationCallback(DataContext.ValidateRemoteCertProd);
                                    break;
                                default:
                                    throw new NotImplementedException("null/unknown environment");
                            }
                        });
                    });
            }
        }

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Destructure.ByTransforming<ExpandoObject>(e => new Dictionary<string, object>(e))
                .WriteTo.Async(al => { al.Console(LogEventLevel.Debug); })
                .CreateLogger();

            try
            {
                var services = new ServiceCollection();
                var startup = new Startup();
                startup.ConfigureServices(services);
                var serviceProvider = services.BuildServiceProvider();

                var context = serviceProvider
                    .GetRequiredService<DataContext>();
                context.Database.Migrate();

                // must reload Postgres types, fixes a chicken-egg problem with type mapping for enums if the database is being created
                using (var conn = (NpgsqlConnection) context.Database.GetDbConnection())
                {
                    conn.Open();
                    conn.ReloadTypes();
                }

                Log.Information("Migration succeeded");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Migration failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}