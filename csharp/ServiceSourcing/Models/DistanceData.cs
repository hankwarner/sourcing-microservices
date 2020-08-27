using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ServiceSourcing.Models
{
    public class DistanceData
    {
        [JsonProperty("locationNetSuiteId")]
        public int NetSuiteListId { get; set; }

        [JsonProperty("distanceFromZip")]
        public decimal DistanceInMiles { get; set; }

        [JsonProperty("masterProductNumber")]
        public string MPID { get; set; }

        public int DistributionCenterNetSuiteId { get; set; }

        public int Quantity { get; set; }

        public string Error { get; set; } = null;
    }
}
