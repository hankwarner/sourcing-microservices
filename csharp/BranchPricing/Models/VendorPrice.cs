using System;
using System.Collections.Generic;
using System.Text;

namespace BranchPricing.Models
{
    public class VendorPrice
    {
        public int MasterProductNumber { get; set; }

        public string VendorId { get; set; }

        public double Price { get; set; }
    }
}
