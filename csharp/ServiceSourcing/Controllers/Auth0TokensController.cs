using System;
using System.Dynamic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ServiceSourcing.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace ServiceSourcing.Controllers
{
    [ApiVersion("2")]
    [ApiVersion("1")]
    [ApiController]
    [Route("api/v{version:apiVersion}/auth0/oauth2")]
    public class Auth0TokensController : ControllerBase
    {
        private readonly ILogger<Auth0TokensController> _logger;
        private readonly IAuthClient _authClient;

        public Auth0TokensController(ILogger<Auth0TokensController> logger, Auth0Client authClient)
        {
            _logger = logger;
            _authClient = authClient;
        }

        [HttpGet]
        [Route("auth")]
        [Authorize(AuthenticationSchemes = "Auth0OpenIdConnect")]
        public async Task<ActionResult<Auth0BearerTokenData>> OAuthAuthorize(CancellationToken token = default)
        {
            dynamic logs = new ExpandoObject();
            try
            {
                var refreshToken = await HttpContext.GetTokenAsync("Auth0OpenIdConnect", "refresh_token");
                var newAccessTokenData = await _authClient.RefreshAccessTokenAsync(refreshToken, token);
                var customBearerTokenData = new Auth0BearerTokenData
                {
                    access_token = newAccessTokenData.id_token,
                    refresh_token = refreshToken,
                    expires_in = newAccessTokenData.expires_in,
                    scope = newAccessTokenData.scope,
                    token_type = "Bearer",
                };

                return new ActionResult<Auth0BearerTokenData>(customBearerTokenData);
            }
            catch (ApplicationException e)
            {
                _logger.LogError(e, "failure refreshing token");

                return BadRequest(e);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "problem listing all repos");
                logs.Result = "failure";
                return Content(
                    JsonConvert.SerializeObject((ExpandoObject) logs, Formatting.Indented),
                    "application/json");
            }
            finally
            {
                _logger.LogInformation("{@log}", (ExpandoObject) logs);
            }
        }

        [HttpPost]
        [Route("token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Auth0RefreshedBearerTokenData>> RefreshOAuthTokens([FromForm] string refreshToken,
            CancellationToken token)
        {
            dynamic logs = new ExpandoObject();
            try
            {
                var newAccessTokenData = await _authClient.RefreshAccessTokenAsync(refreshToken, token);
                var customBearerTokenData = new Auth0RefreshedBearerTokenData
                {
                    access_token = newAccessTokenData.id_token,
                    expires_in = newAccessTokenData.expires_in,
                    scope = newAccessTokenData.scope,
                    token_type = "Bearer"
                };

                return Ok(customBearerTokenData);
            }
            catch (ApplicationException e) when (e.Data.Contains("response") && e.Data["response"] is HttpResponseMessage response)
            {
                _logger.LogError(e, "failure refreshing token");
                logs.Result = "failure";
                var errorResponse = new ProblemDetails
                {
                    Status = (int)response.StatusCode,
                    Title = response.StatusCode.ToString(),
                    Detail = await response.Content.ReadAsStringAsync()
                };
                logs.ResponseInformation = errorResponse;

                return BadRequest(errorResponse);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "failure refreshing token");
                logs.Result = "failure";
                return Content(
                    JsonConvert.SerializeObject((ExpandoObject) logs, Formatting.Indented),
                    "application/json");
            }
            finally
            {
                _logger.LogInformation("{@log}", (ExpandoObject) logs);
            }
        }

        public class Auth0BearerTokenData
        {
            public string access_token;
            public string refresh_token;
            public string expires_in;
            public string scope;
            public string token_type;
        }

        public class Auth0RefreshedBearerTokenData
        {
            public string access_token;
            public string expires_in;
            public string scope;
            public string token_type;
        }

        public class Auth0RefreshTokenFormPostData
        {
            public string client_id { get; set; }
            public string refresh_token { get; set; }
            public string grant_type { get; set; }
        }
    }
}