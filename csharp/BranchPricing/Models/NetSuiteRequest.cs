using System;
using System.Collections.Generic;
using System.Text;

namespace BranchPricing.Models
{
    public class NetSuiteRequest
    {
        public void SetNetItem()
        {
            if(PriceSource.ToUpper() != "GRP")
            {
                isNetItem = true;
            }
        }

        public int ItemId { get; set; }

        public int MasterProductNumber { get; set; }

        public string PriceSource { get; set; }

        public string PriceFormula { get; set; }

        public bool isNetItem { get; set; } = false;

        public double? ListPrice { get; set; }

        public List<BranchPrice> BranchPrices { get; set; }
    }
}
