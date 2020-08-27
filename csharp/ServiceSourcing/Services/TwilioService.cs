using Dapper;
using ServiceSourcing.Models;
using ServiceSourcing.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.Data;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Rest.Notify.V1.Service;
using Twilio.Types;
using NotificationResource = Twilio.Rest.Notify.V1.Service.NotificationResource;
using Polly;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ServiceSourcing.Services
{
    public class TwilioService : ITwilioService
    {
        private const string OriginPhoneNumber = "+14047248672";

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ILogger<TwilioService> _logger;
        private readonly TwilioSettings _settings;

        public TwilioService(ILogger<TwilioService> logger,
                                    IOptions<TwilioSettings> settings,
                                    IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;

            _settings = settings.Value;
            TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
        }


        public string SendText(SMS sms)
        {
            if (string.IsNullOrEmpty(sms.Phone)) throw new ArgumentNullException(nameof(sms.Phone));
            if (string.IsNullOrEmpty(sms.Message)) throw new ArgumentNullException(nameof(sms.Message));

            var message = MessageResource.Create(
                body: sms.Message,
                from: new PhoneNumber(OriginPhoneNumber),
                to: new PhoneNumber(sms.Phone)
            );

            return "Success!";
        }
    }
}
