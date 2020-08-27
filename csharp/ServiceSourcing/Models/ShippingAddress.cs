using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Models
{
    public class ShipFrom : ShippingAddress
    {
        public string BranchNumber { get; set; }
    }

    public class ShipTo : ShippingAddress
    {

    }
    public class ShippingAddress
    {
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
    }
}
