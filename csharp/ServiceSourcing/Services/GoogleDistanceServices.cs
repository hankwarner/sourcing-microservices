using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using ServiceSourcing.Models;
using ServiceSourcing.Options;
using Microsoft.Extensions.Options;

namespace ServiceSourcing.Services
{
    public class GoogleDistanceServices : IGoogleDistanceServices
    {
        private readonly GoogleDistanceSettings _settings;

        public GoogleDistanceServices(IOptions<GoogleDistanceSettings> settings)
        {
            _settings = settings.Value;
        }
        public List<DistributionCenterDistance> GetDistanceDataFromGoogle(string destination, List<GoogleOriginData> branches)
        {
            List<DistributionCenterDistance> distances = new List<DistributionCenterDistance>();

            const int branchesBatchSize = 100;

            for (var i = 0; i < branches.Count; i += branchesBatchSize)
            {
                var branchesBatch = branches.Skip(i).Take(branchesBatchSize).ToList();
                var distancesToAdd = GetBatchedDistanceDataFromGoogle(destination, branchesBatch);
                distances.AddRange(distancesToAdd);
            }

            return distances;
        }

        private List<DistributionCenterDistance> GetBatchedDistanceDataFromGoogle(string destination, List<GoogleOriginData> branches)
        {
            var origins = new List<string>();
            var distances = new List<DistributionCenterDistance>();
            foreach (var branch in branches)
            {
                if (branch.Latitude == null && branch.Longitude == null)
                {
                    branch.Address2 = string.IsNullOrEmpty(branch.Address2) ? "" : branch.Address2 + "%2B";
                    origins.Add($"{branch.Address1}%2B{branch.Address2}{branch.City}%2B{branch.State}%2B{branch.Zip}");
                }
                else
                {
                    origins.Add($"{branch.Latitude}%2C{branch.Longitude}");
                }
            }

            var originString = string.Join("%7C", origins);
            var destinationString = destination;
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?region=us&origins={originString}&destinations={destinationString}&key={_settings.GoogleDistanceMatrixAPIKey}";

            GoogleDistanceMatrixAPIResponse response = null;

            const int retryCount = 5;
            for (var i = 0; i < retryCount; i++)
            {
                try
                {
                    using (var client = new WebClient())
                    {
                        var responseString = client.DownloadString(url);

                        response = GoogleDistanceMatrixAPIResponse.FromJson(responseString);
                    }

                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    i++;
                }
            }

            var responseOrigins = response.OriginAddresses;

            for (var i = 0; i < responseOrigins.Length; i++)
            {
                var results = response.Rows[i].Elements;
                for (var j = 0; j < results.Length; j++)
                {
                    var element = results[j];
                    var destinationZip = destination;

                    if (!element.Status.Equals(Status.Ok))
                    {
                        continue;
                    }

                    var distance = element.Distance.Value;
                    var distributionCenter = branches[i].BranchNumber;

                    var distributionCenterDistance = new DistributionCenterDistance()
                    {
                        BranchNumber = distributionCenter,
                        ZipCode = destinationZip,
                        DistanceInMeters = (int)distance
                    };
                    distances.Add(distributionCenterDistance);
                }
            }

            return distances;
        }
    }
}