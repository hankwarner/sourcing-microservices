using System;
using BranchPricing.Controllers;
using Serilog;
using Helpers;
using BranchPricing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Serilog.Formatting.Json;
using Newtonsoft.Json;

namespace BranchPricing
{
    class Program
    {
        private const string logPath = @"\\SRV-PRO-DASS-01.nbsupply.com\FTPTargets\Sourcing\Branch Pricing\BranchPricing.log";
        public const string teamsUrl = "https://outlook.office.com/webhook/ae6905f1-47fe-4d94-893e-78e7bc088d73@3c2f8435-994c-4552-8fe8-2aec2d0822e4/IncomingWebhook/8e2172e0a059419c809ac5ea12f92da0/89f765ec-9688-47bc-b817-1e989e9a2767";
        private static List<int> itemsWithMultiplePrices = new List<int>();

        static void Main()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(new JsonFormatter(), logPath)
                .CreateLogger();
            Log.Information("Program started");

            try
            {
                // Pulls the entire Branch Pricing feed, grouped by item.
                var branchPricingInFeed = NetSuiteController.GetBranchPricingFromFeed();

                // Pull all existing items with vendor pricing in NetSuite from Ripley tables.
                NetSuiteController.GetVendorPricesInNetSuite();

                if(branchPricingInFeed.Count() == 0)
                {
                    Log.Information("No pricing changes have been made in the pricing feed in the past 14 days.");
                    return;
                }

                var netsuiteRequests = new List<NetSuiteRequest>();

                Parallel.ForEach(
                    branchPricingInFeed,
                    new ParallelOptions { MaxDegreeOfParallelism = 10 },
                    (item) =>
                    {
                        // Create the netsuite request object. This will also set the Vendor ID.
                        var netsuiteRequest = NetSuiteController.CreateRequest(item);
                        var itemId = netsuiteRequest.ItemId;
                        Log.Information($"Item ID {itemId}");

                        // If item has multiple prices, we need to track these and report to FEI
                        CheckForMultiplePricesByVendor(item, itemId);

                        // Compare with NetSuite to see if pricing needs to be updated.
                        var branchesWithoutUpdates = NetSuiteController.CheckForPricingUpdates(netsuiteRequest);

                        // Remove rows that do not require an update.
                        if (branchesWithoutUpdates.Count() > 0)
                        {
                            NetSuiteController.RemoveBranchesWithoutPricingUpdates(netsuiteRequest, branchesWithoutUpdates);
                        }

                        // Send to NetSuite only if there are rows that require a pricing update.
                        if (netsuiteRequest.BranchPrices.Count == 0)
                        {
                            Log.Information("No pricing updates required for this item.");
                        }
                        else
                        {
                            Log.Information($"NetSuite request added for item id {itemId}");
                            netsuiteRequests.Add(netsuiteRequest);
                        }
                    }
                );

                if (netsuiteRequests.Count() == 0)
                {
                    Log.Information("No pricing updates required. No requests sent to NetSuite.");
                    return;
                }

                Log.Information("itemsWithMultiplePrices {itemsWithMultiplePrices}", itemsWithMultiplePrices);
                Log.Information("All NetSuite Requests: {netsuiteRequests}", JsonConvert.SerializeObject(netsuiteRequests));

                // Send branch pricing information that needs to be updated to NetSuite
                UpdateBranchPricingInNetSuite(netsuiteRequests);

            }
            catch (SqlException ex)
            {
                var errorMessage = $"A SQL error occurred. Error: {ex}";
                Log.Error(errorMessage);
                string title = "Error in BranchPricing";
                string color = "yellow";
                TeamsHelper teamsMessage = new TeamsHelper(title, errorMessage, color, teamsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);

                // Restart the program when a timeout error occurs
                if (ex.Message.ToLower().Contains("timeout"))
                {
                    Log.Information("Restarting Program.Main()");
                    Program.Main();
                }

            }
            catch (Exception ex)
            {
                Log.Error("Error in BranchPricing: {ex}", ex);
                string title = "Error in BranchPricing";
                string text = $"Error message: {ex.Message}";
                string color = "red";
                TeamsHelper teamsMessage = new TeamsHelper(title, text, color, teamsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
            }
        }


        public static void UpdateBranchPricingInNetSuite(List<NetSuiteRequest> netsuiteRequests)
        {
            try
            {
                Parallel.ForEach(
                    netsuiteRequests,
                    new ParallelOptions { MaxDegreeOfParallelism = 10 },
                    (netsuiteRequest) =>
                    {
                        NetSuiteController.SendToNetSuite(netsuiteRequest);
                    }
                );

            }
            catch (Exception ex)
            {
                Log.Error("Error in UpdateBranchPricingInNetSuite: {ex}", ex);
                string title = "Error in BranchPricing UpdateBranchPricingInNetSuite";
                string text = $"Error message: {ex.Message}";
                string color = "red";
                TeamsHelper teamsMessage = new TeamsHelper(title, text, color, teamsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
            }
        }


        private static void CheckForMultiplePricesByVendor(IGrouping<int, BranchPricingFeed> item, int itemId)
        {
            var itemsByVendor = item.GroupBy(vendor => vendor.LOGON);
            bool hasMultplePricesForSameVendor = false;

            foreach (var vendor in itemsByVendor)
            {
                if (vendor.Count() > 1)
                {
                    hasMultplePricesForSameVendor = true;
                    break;
                }
            }

            if (hasMultplePricesForSameVendor)
            {
                itemsWithMultiplePrices.Add(itemId);
            }
        }


    }
}
