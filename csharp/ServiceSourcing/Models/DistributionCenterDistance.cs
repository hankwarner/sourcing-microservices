using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Models
{
    public class DistributionCenterDistance
    {
        public string BranchNumber { get; set; }
        public string ZipCode { get; set; }
        public int DistanceInMeters { get; set; }
    }
}
