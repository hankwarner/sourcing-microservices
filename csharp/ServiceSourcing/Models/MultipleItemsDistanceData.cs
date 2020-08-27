using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Models
{
    public class MultipleItemsDistanceData
    {
        public int ItemId { get; set; }

        public List<DistanceData> distanceData { get; set; }

        public string Error { get; set; } = null;
    }
}
