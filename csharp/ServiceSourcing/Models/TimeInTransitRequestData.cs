using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceSourcing.Models;
using ServiceSourcing.Options;
using Microsoft.Extensions.Options;

namespace ServiceSourcing.Models
{
    
    public class TimeInTransitRequestData
    {
        public List<ShipFrom> ShipFrom { get; set; }
        public ShipTo ShipTo { get; set; }
        public double? Weight { get; set; }
        public int? TotalPackagesInShipment { get; set; }
    }
}
