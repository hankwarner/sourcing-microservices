using System;
using System.Collections.Generic;
using System.Text;

namespace BranchPricing.Models
{
    public class BranchPricingFeed
    {
        public double? ListPrice
        {
            get
            {
                if(BR_LIST == null)
                {
                    return MSTR_LIST_PRICE;
                }
                else
                {
                    return BR_LIST;
                }
            }
        }


        public void SetFormula()
        {
            switch (PRICE_SRC)
            {
                case "GRP":
                    Formula = FORMULA_GRP;
                    break;
                case "PROD":
                    Formula = FORMULA_PROD;
                    break;
                case "MSTR_LIST":
                    Formula = MSTR_LIST_PRICE.ToString();
                    break;
                case "BR_LIST":
                    Formula = BR_LIST.ToString();
                    break;
                default:
                    break;
            }
        }


        public int ItemId { get; set; }

        public int MPID { get; set; }

        public string LOGON { get; set; }

        public double? BR_LIST { get; set; }

        public double? MSTR_LIST_PRICE { get; set; }

        public string PRICE_SRC { get; set; }

        public double PRICE_BOOK_NET { get; set; }
        
        public string? FORMULA_GRP { get; set; }

        public string? FORMULA_PROD { get; set; }

        public string Formula { get; set; } = "";
    }
}
