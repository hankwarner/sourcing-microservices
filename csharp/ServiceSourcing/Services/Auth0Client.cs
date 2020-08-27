using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ServiceSourcing.Models.Exceptions;
using ServiceSourcing.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;

namespace ServiceSourcing.Services
{
    public class Auth0Client : IAuthClient
    {
        private readonly string _auth0ClientId;
        private readonly string _auth0ClientSecret;
        private readonly AsyncPolicy _policy;
        private HttpClient _client { get; set; }

        public Auth0Client(HttpClient client, IOptions<Auth0Options> options)
        {
            _policy = Policy
                .Handle<RetryableException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(10),
                });
            _client = client;
            _auth0ClientId = options.Value.ClientId;
            _auth0ClientSecret = options.Value.ClientSecret;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request, CancellationToken token = default)
        {
            var func = new Func<CancellationToken, Task<HttpResponseMessage>>(async (cancellationToken) =>
            {
                var response = await _client.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }
                else
                {
                    const string message = "Error retrieving response.  Check inner details for more info.";
                    var microServiceException = new ApplicationException(message);
                    microServiceException.Data.Add("response", response);
                    throw microServiceException;
                }
            });
            var policyResponse = await _policy
                .ExecuteAsync(func, token);

            return policyResponse;
        }

        public async Task<AccessTokenData> RefreshAccessTokenAsync(string refreshToken,
            CancellationToken token = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _auth0ClientId),
                    new KeyValuePair<string, string>("client_secret", _auth0ClientSecret),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken)
                })
            };

            var response = await ExecuteAsync(request, token);

            return JsonConvert.DeserializeObject<AccessTokenData>(await response.Content.ReadAsStringAsync());
        }
    }
}