using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ServiceSourcing.Models;
using ServiceSourcing.Options;
using Serilog;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;

using RestSharp;
using RestSharp.Authenticators;
using UPSSecurity = ServiceSourcing.Models;

namespace ServiceSourcing.Services
{
    public class TimeInTransitService : ITimeInTransitService
    {
        private readonly UPSSSettings _settings;

        public TimeInTransitService(IOptions<UPSSSettings> settings)
        {
            _settings = settings.Value;
        }

        public TimeInTransitAPIResponse GetTimeInTransitAPIResponse(TimeInTransAPIRequest timeInTransApiRequest)
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetry(4, _ => TimeSpan.FromSeconds(30), (ex, ts, count, context) =>
                {
                    const string errorMessage = "Error in GetTimeInTransit";
                    Log.Warning(ex, $"{errorMessage} . Retrying...");
                    if (count == 4) { Log.Error(ex, errorMessage); }
                });

            //Authenticate and establish client
            var apiUrl = _settings.UpsTimeInTransitApiUrl;
            var headers = new Dictionary<string, string>
            {
                {"Username", _settings.Username},
                {"Password", _settings.Password},
                {"AccessLicenseNumber", _settings.AccessLicenseNumber},
                {"transId", "1"},
                {"transactionSrc", "ServiceSourcing"}
            };

            return retryPolicy.Execute(() =>
            {
                var client = new RestClient(apiUrl);
                client.AddDefaultHeaders(headers);

                var jsonRequest = JsonConvert.SerializeObject(timeInTransApiRequest);

                var req = new RestRequest(Method.POST);
                req.AddHeader("Content-Type", "application/json");
                req.AddParameter("application/json; charset=utf-8", jsonRequest, ParameterType.RequestBody);

                var jsonResponse = client.Execute(req).Content;

                var upsResponse = JObject.Parse(jsonResponse);
                var apiResponse = upsResponse.ToObject<TimeInTransitAPIResponse>();

                return apiResponse;
            });
        }

        public Dictionary<string, List<KeyValuePair<string, DateTime>>> GetTimeInTransit(TimeInTransitRequestData timeInTransitData)
        {
            try
            {
                var apiRequestObject = new TimeInTransAPIRequest
                {
                    destinationCityName = timeInTransitData.ShipTo.City,
                    destinationPostalCode = timeInTransitData.ShipTo.Zip,
                    destinationStateProvince = timeInTransitData.ShipTo.State,
                    shipDate = DateTime.Today.ToString("yyyy-MM-dd"),
                    shipTime = DateTime.Now.ToString("hh:mm:ss"),
                    weight = (timeInTransitData.Weight ?? 0) == 0 ? 1 : (double) timeInTransitData.Weight,
                    numberOfPackages = (timeInTransitData.TotalPackagesInShipment ?? 0) == 0
                        ? 1 : (int) timeInTransitData.TotalPackagesInShipment
                };

                var formattedResults = new Dictionary<string, List<KeyValuePair<string, DateTime>>>();

                foreach (var origin in timeInTransitData.ShipFrom)
                {
                    apiRequestObject.originCityName = origin.City;
                    apiRequestObject.originPostalCode = origin.Zip;
                    apiRequestObject.originStateProvince = origin.State;

                    var response = GetTimeInTransitAPIResponse(apiRequestObject);
                    var listOfOptions = (from service in response.emsResponse.services
                            let arrivalDate = DateTime.ParseExact(service.deliveryDate, "yyyy-MM-dd", null)
                            let arrivalTime = TimeSpan.ParseExact(service.deliveryTime, @"hh\:mm\:ss", null)
                            let dayAndTime = arrivalDate + arrivalTime
                            select new KeyValuePair<string, DateTime>(service.serviceLevelDescription, dayAndTime))
                        .ToList();

                    formattedResults.Add(origin.BranchNumber, listOfOptions);
                }
                return formattedResults;
            }
            catch (Exception ex)
            {
                var errorMessage = "Error in GetTimeInTransit";
                Log.Error(ex, errorMessage);
                throw;
            }
        }
    }
}
