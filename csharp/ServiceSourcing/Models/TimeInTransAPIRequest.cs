using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceSourcing.Models;

namespace ServiceSourcing.Models
{
    public class TimeInTransAPIRequest
    {
        public string originCountryCode { get; set; } = "US";
        public string originStateProvince { get; set; }
        public string originCityName { get; set; }
        public string originPostalCode { get; set; }
        public string destinationCountryCode { get; set; } = "US";
        public string destinationStateProvince { get; set; }
        public string destinationCityName { get; set; }
        public string destinationPostalCode { get; set; }
        public double weight { get; set; }
        public string weightUnitOfMeasure { get; set; } = "lbs";
        public string shipDate { get; set; }
        public string shipTime { get; set; }
        public int numberOfPackages { get; set; }
    }
}
