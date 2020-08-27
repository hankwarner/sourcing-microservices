using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ServiceSourcing.Models.Exceptions;
using ServiceSourcing.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;

namespace ServiceSourcing.Services
{
    public class SupplyClient : IAuthClient
    {
        private readonly string _supplyClientId;
        private readonly string _supplyClientSecret;
        private readonly AsyncPolicy _policy;
        private readonly HttpClient _client;

        public SupplyClient(HttpClient client, IOptions<SupplyAuthOptions> options)
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
            _supplyClientId = options.Value.ClientId;
            _supplyClientSecret = options.Value.ClientSecret;
        }

        public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request,
            CancellationToken cancelToken = default)
        {
            var response = await _client.SendAsync(request, cancelToken);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            else
            {
                var blah = response.Content.ReadAsStringAsync().Result;
                const string message = "Error retrieving response.  Check inner details for more info.";
                var microServiceException = new ApplicationException(message);
                microServiceException.Data.Add("response", response);
                throw microServiceException;
            }
        }

        public async Task<AccessTokenData> RefreshAccessTokenAsync(string refreshToken,
            CancellationToken token = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/idp/v1/connect/token")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("client_id", _supplyClientId),
                    new KeyValuePair<string, string>("client_secret", _supplyClientSecret),
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                })
            };

            var response = await ExecuteAsync(request, token);
            var responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<AccessTokenData>(await response.Content.ReadAsStringAsync());
        }
    }
}