using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceSourcing.Auth
{
    public static class SupplyIdp
    {
        public const string JwtBearer = "SupplyJwtBearer";
        public const string OpenIdConnect = "SupplyOpenIdConnect";

        public static AuthenticationBuilder ConfigureSupplyIdp(this AuthenticationBuilder authBuilder, string authority)
        {
            authBuilder
                .AddJwtBearer(JwtBearer, options =>
                {
                    options.Audience = "ServiceSourcing";
                    options.Authority = authority;
                    var myvalidator =
                        new SupplySecurityTokenValidator(options.SecurityTokenValidators.SingleOrDefault());
                    options.SecurityTokenValidators.Clear();
                    options.SecurityTokenValidators.Add(myvalidator);
                })
                .AddOpenIdConnect(OpenIdConnect, options =>
                {
                    options.AuthenticationMethod = OpenIdConnectRedirectBehavior.FormPost;
                    options.RequireHttpsMetadata = false;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = authority;
                    options.ClientId = "ServiceSourcing";
                    options.ClientSecret = "ServiceSourcingSecret";
                    options.GetClaimsFromUserInfoEndpoint = false;
                    options.SaveTokens = true;
                    options.Scope.Add("email");
                    options.Scope.Add("profile");
                    options.Scope.Add("openid");
                    options.Scope.Add("offline_access");
                    options.ResponseType = "code";
                    options.CallbackPath = "/signin-oidc";
                    options.Prompt = "none";
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            context.ProtocolMessage.SetParameter("audience", "ServiceSourcing");
                            return Task.CompletedTask;
                        },
                    };
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidAudience = "ServiceSourcing",
                        ValidateAudience = true,
                    };
                });

            return authBuilder;
        }

        public static SwaggerGenOptions ConfigureSupplyOpenIdSwaggerAuth(this SwaggerGenOptions options)
        {
            options
                .AddSecurityDefinition(JwtBearer, new ApiKeyScheme
                {
                    Description =
                        "OAuth2 Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
            return options;
        }

        public class SupplyAuthHeaderParameterOperationFilter : IOperationFilter
        {
            public void Apply(Operation operation, OperationFilterContext context)
            {
                var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
                var filters = filterPipeline
                    .Select(filterInfo => filterInfo.Filter)
                    .ToList();
                var bearerAuthorized = filters
                    .Any(filter => (filter is AuthorizeFilter authorizeFilter) &&
                                   authorizeFilter.Policy.AuthenticationSchemes.Contains(JwtBearer));
                var allowAnonymous = filters
                    .Any(filter => filter is IAllowAnonymousFilter);


                if (bearerAuthorized && !allowAnonymous)
                {
                    var bearerAuthorization = new Dictionary<string, IEnumerable<string>>
                    {
                        {JwtBearer, new List<string>()},
                    };
                    if (operation.Security == null)
                    {
                        operation.Security = new IDictionary<string, IEnumerable<string>>[] {bearerAuthorization};
                    }
                    else
                    {
                        operation.Security.Add(bearerAuthorization);
                    }
                }
            }
        }

        public class SupplySecurityTokenValidator : ISecurityTokenValidator
        {
            private ISecurityTokenValidator _securityTokenValidatorImplementation;

            public SupplySecurityTokenValidator(ISecurityTokenValidator securityTokenValidatorImplementation)
            {
                _securityTokenValidatorImplementation = securityTokenValidatorImplementation;
            }

            public bool CanReadToken(string securityToken)
            {
                return _securityTokenValidatorImplementation.CanReadToken(securityToken);
            }

            public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters,
                out SecurityToken validatedToken)
            {
                var claimsPrincipal =
                    _securityTokenValidatorImplementation.ValidateToken(securityToken, validationParameters,
                        out validatedToken);
                return claimsPrincipal;
            }

            public bool CanValidateToken => _securityTokenValidatorImplementation.CanValidateToken;

            public int MaximumTokenSizeInBytes
            {
                get => _securityTokenValidatorImplementation.MaximumTokenSizeInBytes;
                set => _securityTokenValidatorImplementation.MaximumTokenSizeInBytes = value;
            }
        }
    }
}