using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Models
{
    public class RequestForDistanceData
    {
        public string MasterProductNumber { get; set; }

        public string Zip { get; set; }

        public int MilesAllowedFromZip { get; set; }
    }
}
