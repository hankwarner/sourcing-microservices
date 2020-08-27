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
    [Route("api/v{version:apiVersion}/supply/oauth2")]
    public class SupplyTokensController : ControllerBase
    {
        private readonly ILogger<SupplyTokensController> _logger;
        private readonly IAuthClient _authClient;

        public SupplyTokensController(ILogger<SupplyTokensController> logger, SupplyClient authClient)
        {
            _logger = logger;
            _authClient = authClient;
        }

        [HttpGet]
        [Route("auth")]
        [Authorize(AuthenticationSchemes = "SupplyOpenIdConnect")]
        public async Task<ActionResult<SupplyBearerTokenData>> OAuthAuthorize(CancellationToken token = default)
        {
            dynamic logs = new ExpandoObject();
            try
            {
                var refreshToken = await HttpContext.GetTokenAsync("SupplyOpenIdConnect", "refresh_token");
                var newAccessTokenData = await _authClient.RefreshAccessTokenAsync(refreshToken, token);
                var customBearerTokenData = new SupplyBearerTokenData
                {
                    access_token = newAccessTokenData.access_token,
                    refresh_token = refreshToken,
                    expires_in = newAccessTokenData.expires_in,
                    scope = newAccessTokenData.scope,
                    token_type = "Bearer",
                };

                return new ActionResult<SupplyBearerTokenData>(customBearerTokenData);
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
        public async Task<ActionResult<SupplyRefreshedBearerTokenData>> RefreshOAuthTokens([FromForm] string refreshToken,
            CancellationToken token)
        {
            dynamic logs = new ExpandoObject();
            try
            {
                var newAccessTokenData = await _authClient.RefreshAccessTokenAsync(refreshToken, token);
                var customBearerTokenData = new SupplyRefreshedBearerTokenData
                {
                    access_token = newAccessTokenData.id_token,
                    expires_in = newAccessTokenData.expires_in,
                    scope = newAccessTokenData.scope,
                    token_type = "Bearer",
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

        public class SupplyBearerTokenData
        {
            public string access_token;
            public string refresh_token;
            public string expires_in;
            public string scope;
            public string token_type;
        }

        public class SupplyRefreshedBearerTokenData
        {
            public string access_token;
            public string expires_in;
            public string scope;
            public string token_type;
        }

        public class SupplyRefreshTokenFormPostData
        {
            public string client_id { get; set; }
            public string refresh_token { get; set; }
            public string grant_type { get; set; }
        }
    }
}