using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceSourcing
{
    /// <summary>
    /// Configures the Swagger generation options.
    /// </summary>
    /// <remarks>This allows API versioning to define a Swagger document per API version after the
    /// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.</remarks>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        readonly IApiVersionDescriptionProvider provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
        /// </summary>
        /// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
        public ConfigureSwaggerOptions( IApiVersionDescriptionProvider provider ) => this.provider = provider;

        /// <inheritdoc />
        public void Configure( SwaggerGenOptions options )
        {
            // add a swagger document for each discovered API version
            // note: you might choose to skip or document deprecated API versions differently
            foreach ( var description in provider.ApiVersionDescriptions )
            {
                options.SwaggerDoc( description.GroupName, CreateInfoForApiVersion( description ) );
            }
        }

        static Info CreateInfoForApiVersion( ApiVersionDescription description )
        {
            var versionInfo = Assembly.GetEntryAssembly().GetName().Version.ToString();
            var descriptionPath = Path.Combine(new [] {Directory.GetCurrentDirectory(), "markdown", "swagger_description.md"});
            var descriptionText = File.Exists(descriptionPath) 
                ? File.ReadAllText(descriptionPath).Replace("{{version}}",versionInfo)
                : "";
            
            var info = new Info()
            {
                Title = "ServiceSourcing",
                Version = description.ApiVersion.ToString(),
                Description = descriptionText,
                Contact = new Contact() { Name = "Hank Warner", Email = "h.warner@supply.com" },
            };

            if ( description.IsDeprecated )
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }
}