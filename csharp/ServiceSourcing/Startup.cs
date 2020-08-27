using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ServiceSourcing.Auth;
using ServiceSourcing.Models;
using ServiceSourcing.Options;
using ServiceSourcing.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Google.Protobuf.WellKnownTypes;

namespace ServiceSourcing
{
    public class Startup
    {
        public IConfiguration Configuration { get; set; }
        public IHostingEnvironment Environment { get; set; }
        public string Auth0ClientId { get; }
        public string Auth0ClientSecret { get; }
        public string Auth0Domain { get; set; }
        public string SupplyClientId { get; }
        public string SupplyClientSecret { get; }
        public string SupplyDomain { get; set; }
        public string ConnectionString { get; set; }
        public string GoogleDistanceMatrixAPIKey { get; set; }

        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;

            Auth0ClientId = System.Environment.GetEnvironmentVariable("AUTH0_CLIENT_ID");
            Auth0ClientSecret = System.Environment.GetEnvironmentVariable("AUTH0_CLIENT_SECRET");
            Auth0Domain = System.Environment.GetEnvironmentVariable("AUTH0_DOMAIN");
            SupplyClientId = "ServiceSourcing";
            SupplyClientSecret = "ServiceSourcingSecret";
            SupplyDomain = "https://www.supply.com";
            ConnectionString = Configuration["NpgsqlConnectionString"];
        }

        protected Startup() { }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public virtual void ConfigureServices(IServiceCollection services)
        {
            #region setup mvc
            var supplyRegex = new Regex(@"^https?://(.*\.supply\.com|localhost)(:\d{1,5})?$");
            services
                .AddCors(options =>
                {
                    options
                        .AddDefaultPolicy(builder => builder.AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(supplyRegex.IsMatch));
                })
                .AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Latest);

            services
                .AddHealthChecks()
                .AddUrlGroup(new Uri("https://www.supply.com/abdashboard.aspx"));
            services
                .AddHsts(opts =>
                {
                    opts.MaxAge = TimeSpan.FromDays(365); // set HSTS to 1 year
                });
            
            services
                .AddResponseCompression(opts => opts.EnableForHttps = true)
                .AddResponseCaching()      // see https://docs.microsoft.com/en-us/aspnet/core/performance/caching/response?view=aspnetcore-2.2
                .AddMemoryCache()          // adds an in-memory cache for general use
                .AddHttpContextAccessor(); // add DI for HttpContext
            #endregion setup mvc

            #region setup versioning and documentation
            // configure api versioning and swagger gen
            services
                .AddApiVersioning(options => { options.ReportApiVersions = true; })
                .AddVersionedApiExplorer(options =>
                {
                    options.GroupNameFormat           = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            if (Environment.IsDevelopment())
            {
                services
                    .AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>()
                    .AddSwaggerGen(options =>
                    {
                        // add a custom operation filter which sets default values
                        options.OperationFilter<SwaggerDefaultValues>();

                        // integrate xml comments
                        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceSourcing.xml");
                        options.IncludeXmlComments(filePath);
                        options.DescribeAllEnumsAsStrings();
                        options.DescribeStringEnumsInCamelCase();
                        options.EnableAnnotations();
                        options.ConfigureAuth0OpenIdSwaggerAuth();
                        options.ConfigureSupplyOpenIdSwaggerAuth();
                    })
                    .ConfigureSwaggerGen(options =>
                    {
                        options.OperationFilter<Auth0AuthHeaderParameterOperationFilter>();
                        options.OperationFilter<SupplyIdp.SupplyAuthHeaderParameterOperationFilter>();
                    });
            }
            #endregion setup versioning and documentation

            #region setup database
            services
                .AddDbContext<DataContext>(dbOptions =>
                {
                    dbOptions.UseNpgsql(ConnectionString, npgsqlOptions =>
                    {
                        if (Environment.IsDevelopment())
                        {
                            npgsqlOptions.RemoteCertificateValidationCallback(DataContext.ValidateRemoteCertDev);
                        }
                        else if (Environment.IsProduction())
                        {
                            npgsqlOptions.RemoteCertificateValidationCallback(DataContext.ValidateRemoteCertProd);
                        }
                        else {
                            throw new InvalidOperationException($"Environment '{Environment.EnvironmentName}' needs to be set up");
                        }
                    });
                });
            #endregion setup database
            
            #region setup authentication
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultAuthenticateScheme = "Auth0OpenJwtBearer";
                    options.DefaultChallengeScheme = "Auth0OpenIdConnect";
                })
                .AddCookie(options =>
                {
                    options.Cookie.HttpOnly = false;
                    options.Cookie.SameSite = SameSiteMode.None;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.CookieManager = new ChunkingCookieManager()
                    {
                        ThrowForPartialCookies = true
                    };
                })
                .ConfigureSupplyIdp($"{SupplyDomain}/idp/v1")
                .ConfigureAuth0OpenIddAuth(Auth0ClientId, Auth0ClientSecret, Auth0Domain);

            services.Configure<Auth0Options>(options =>
            {
                options.Domain = Auth0Domain;
                options.ClientId = Auth0ClientId;
                options.ClientSecret = Auth0ClientSecret;
            });
            services.Configure<SupplyAuthOptions>(options =>
            {
                options.Domain = SupplyDomain;
                options.ClientId = SupplyClientId;
                options.ClientSecret = SupplyClientSecret;
            })
            .Configure<TwilioSettings>(options =>
            {
                options.AccountSid = GetRequiredEnvironmentVariable("TWILIO_ACCOUNTSID");
                options.AuthToken = GetRequiredEnvironmentVariable("TWILIO_AUTHTOKEN");
            })
            .Configure<GoogleDistanceSettings>(options => 
            {
                options.GoogleDistanceMatrixAPIKey = GetRequiredEnvironmentVariable("GOOGLE_DISTANCE_MATRIX_API_KEY");
            })
            .Configure<UPSSSettings>(options =>
            {
                options.AccessLicenseNumber = GetRequiredEnvironmentVariable("UPS_ACCESS_LICENSE_NUMBER");
                options.Username = GetRequiredEnvironmentVariable("UPS_USERNAME");
                options.Password = GetRequiredEnvironmentVariable("UPS_PASSWORD");
                options.UpsTimeInTransitApiUrl = Environment.IsDevelopment()
                    ? "https://wwwcie.ups.com/ship/v1/shipments/transittimes"
                    : "https://onlinetools.ups.com/ship/v1/shipments/transittimes";
            });

            var keyPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            services
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
            #endregion setup authentication


            services.Configure<NewRelic>(options =>
            {
                options.NewRelic_App_Name = Configuration.GetValue<string>("NEWRELIC_APP_NAME");
            });
            Log.Information(Configuration.GetValue<string>("NEWRELIC_APP_NAME"));

            services.AddHttpClient<Auth0Client>(client => { client.BaseAddress = new Uri(Auth0Domain); });
            services.AddHttpClient<SupplyClient>(client => { client.BaseAddress = new Uri(SupplyDomain); });

            services.AddSingleton<IInventoryServices, InventoryServices>();
            services.AddSingleton<IDistanceDataServices, DistanceDataServices>();
            services.AddSingleton<ITwilioService, TwilioService>();
            services.AddSingleton<ILocationServices, LocationServices>();
            services.AddSingleton<IGoogleDistanceServices, GoogleDistanceServices>();
            services.AddSingleton<IItemDataService, ItemDataService>();
            services.AddSingleton<ITimeInTransitService, TimeInTransitService>();
        }

        private static string GetRequiredEnvironmentVariable(string environmentVariableName)
        {
            var variable = System.Environment.GetEnvironmentVariable(environmentVariableName);

            if (variable == null) throw new InvalidOperationException($"{environmentVariableName} is null but must be set.");

            return variable;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder application, IHostingEnvironment environment, IApiVersionDescriptionProvider provider)
        {
            Log.Information("In {environmentName} environment", environment.EnvironmentName);

            if (environment.IsDevelopment() || environment.IsStaging())
            {
                application.UseDeveloperExceptionPage();
                IdentityModelEventSource.ShowPII = true;
            }
            
            if (environment.IsProduction())
            {
                application.UseHsts();
            }

            application
                .UseResponseCompression()
                .UseHealthChecks("/health", new HealthCheckOptions {Predicate = _ => true})
                .UseHealthChecks("/health/self", new HealthCheckOptions {Predicate = _ => false})
                .UseForwardedHeaders(new ForwardedHeadersOptions {ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto})
                .UseCors()
                .UseAuthentication()
                .UseMvc()
                .UseStaticFiles();

            if (environment.IsDevelopment())
            {
                application
                    .UseSwagger()
                    .UseSwaggerUI(options =>
                    {
                        // set swagger to be the home page
                        options.RoutePrefix = "";

                        // build a swagger endpoint for each discovered API version
                        var sortedApis = provider
                            .ApiVersionDescriptions
                            .OrderByDescending(e => e.ApiVersion);

                        foreach (var description in sortedApis)
                        {
                            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                                description.GroupName.ToUpperInvariant());
                        }
                    });

            }
        }
    }
}