using System;
using System.Collections.Generic;
using System.Text;

namespace BranchPricing.Models
{
    public class BranchPrice
    {
        public string BranchName { get; set; }

        public string VendorId { get; set; }

        public double Price { get; set; }


        public void SetVendorId()
        {
            VendorId = GetVendorId(BranchName);
        }


        // Production
        public static string GetVendorId(string branchName)
        {
            string vendorId = "";

            switch (branchName)
            {
                case "CHARLOTTE":
                    vendorId = "18062534";
                    break;
                case "CHICAGO":
                    vendorId = "18072742";
                    break;
                case "COL":
                    vendorId = "18072743";
                    break;
                case "DALLAS":
                    vendorId = "18072744";
                    break;
                case "DETROIT":
                    vendorId = "18072745";
                    break;
                case "GARDEN":
                    vendorId = "18072746";
                    break;
                case "KC":
                    vendorId = "18072747";
                    break;
                case "LAKEWOOD":
                    vendorId = "18072748";
                    break;
                case "LENZ":
                    vendorId = "18072749";
                    break;
                case "LYNN":
                    vendorId = "18072750";
                    break;
                case "NASH":
                    vendorId = "18072751";
                    break;
                case "OHVAL":
                    vendorId = "18072752";
                    break;
                case "ORL":
                    vendorId = "18072753";
                    break;
                case "PHOENIX":
                    vendorId = "18072754";
                    break;
                case "PLYMOUTH":
                    vendorId = "18072755";
                    break;
                case "RICH":
                    vendorId = "18072756";
                    break;
                case "SACRAMENTO":
                    vendorId = "18072757";
                    break;
                case "SEATTLE":
                    vendorId = "18072758";
                    break;
                default:
                    break;
            }

            return vendorId;
        }

        // Sandbox
        //public static string GetVendorId(string branchName)
        //{
        //    string vendorId = "";

        //    switch (branchName)
        //    {
        //        case "CHARLOTTE":
        //            vendorId = "17500950";
        //            break;
        //        case "CHICAGO":
        //            vendorId = "17500951";
        //            break;
        //        case "COL":
        //            vendorId = "17500952";
        //            break;
        //        case "DALLAS":
        //            vendorId = "17500953";
        //            break;
        //        case "DETROIT":
        //            vendorId = "17500954";
        //            break;
        //        case "GARDEN":
        //            vendorId = "17500955";
        //            break;
        //        case "KC":
        //            vendorId = "17500956";
        //            break;
        //        case "LAKEWOOD":
        //            vendorId = "17500957";
        //            break;
        //        case "LENZ":
        //            vendorId = "17500958";
        //            break;
        //        case "LYNN":
        //            vendorId = "17500959";
        //            break;
        //        case "NASH":
        //            vendorId = "17500960";
        //            break;
        //        case "OHVAL":
        //            vendorId = "17500961";
        //            break;
        //        case "ORL":
        //            vendorId = "17500962";
        //            break;
        //        case "PHOENIX":
        //            vendorId = "17500963";
        //            break;
        //        case "PLYMOUTH":
        //            vendorId = "17500964";
        //            break;
        //        case "RICH":
        //            vendorId = "17500965";
        //            break;
        //        case "SACRAMENTO":
        //            vendorId = "17500966";
        //            break;
        //        case "SEATTLE":
        //            vendorId = "17500967";
        //            break;
        //        default:
        //            break;
        //    }

        //    return vendorId;
        //}
    }
}