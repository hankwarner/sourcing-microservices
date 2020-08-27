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
    public static class Auth0
    {
        public static AuthenticationBuilder ConfigureAuth0OpenIddAuth(this AuthenticationBuilder authBuilder, string clientId,
            string clientSecret, string domain)
        {
            
            authBuilder
                .AddJwtBearer("Auth0JwtBearer", options =>
                {
                    options.Audience = clientId;
                    options.Authority = domain;
                    var myvalidator = new Auth0SecurityTokenValidator(options.SecurityTokenValidators.SingleOrDefault());
                    options.SecurityTokenValidators.Clear();
                    options.SecurityTokenValidators.Add(myvalidator);
                })
                .AddOpenIdConnect("Auth0OpenIdConnect", options =>
                {
                    options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
                    options.RequireHttpsMetadata = false;
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.Authority = domain;
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.Scope.Add("email");
                    options.Scope.Add("profile");
                    options.Scope.Add("openid");
                    options.Scope.Add("offline_access");
                    options.Scope.Add("read:accountdetails");
                    options.ResponseType = "code";
                    options.CallbackPath = "/oauth_callback";
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidAudience = clientId,
                        ValidateAudience = true,
                    };
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context =>
                        {
                            context.ProtocolMessage.SetParameter("connection", "Username-Password-Authentication");

                            return Task.FromResult(0);
                        }
                    };
                });

            return authBuilder;
        }

        public static SwaggerGenOptions ConfigureAuth0OpenIdSwaggerAuth(this SwaggerGenOptions options)
        {
            options
                .AddSecurityDefinition("Auth0JwtBearer", new ApiKeyScheme
                {
                    Description =
                        "OAuth2 Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = "header",
                    Type = "apiKey"
                });
            return options;
        }
    }

    public class Auth0AuthHeaderParameterOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;
            var filters = filterPipeline
                .Select(filterInfo => filterInfo.Filter)
                .ToList();
            var bearerAuthorized = filterPipeline
                .Select(filterInfo => filterInfo.Filter)
                .Any(filter => (filter is AuthorizeFilter authorizeFilter) &&
                               authorizeFilter.Policy.AuthenticationSchemes.Contains("Auth0JwtBearer"));
            var allowAnonymous = filterPipeline
                .Select(filterInfo => filterInfo.Filter)
                .Any(filter => filter is IAllowAnonymousFilter);


            if (bearerAuthorized && !allowAnonymous)
            {
                var bearerAuthorization = new Dictionary<string, IEnumerable<string>>
                {
                    {"Auth0JwtBearer", new List<string>()},
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

    public class Auth0SecurityTokenValidator : ISecurityTokenValidator
    {
        private ISecurityTokenValidator _securityTokenValidatorImplementation;
        public Auth0SecurityTokenValidator(ISecurityTokenValidator securityTokenValidatorImplementation)
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
            var claimsPrincipal = _securityTokenValidatorImplementation.ValidateToken(securityToken, validationParameters, out validatedToken);
            return claimsPrincipal;
            
            //see below for example of validating against specific claims
            var favoriteColorClaims = claimsPrincipal
                .Claims
                .Where(claim => claim.Type.Equals("https://example.com/favorite_color"))
                .ToList();
            var favoriteColorBlue = favoriteColorClaims
                .Where(claim => claim.Value.Equals("blue"))
                .ToList();
            if (favoriteColorClaims.Count > 0 && favoriteColorClaims.Count == favoriteColorBlue.Count)
            {
                return claimsPrincipal;
            }
            else
            {
                //throw new SecurityTokenException("Token not recognized");
                throw new SecurityTokenValidationException("wrong color");
            }
        }

        public bool CanValidateToken => _securityTokenValidatorImplementation.CanValidateToken;

        public int MaximumTokenSizeInBytes
        {
            get => _securityTokenValidatorImplementation.MaximumTokenSizeInBytes;
            set => _securityTokenValidatorImplementation.MaximumTokenSizeInBytes = value;
        }
    }
}