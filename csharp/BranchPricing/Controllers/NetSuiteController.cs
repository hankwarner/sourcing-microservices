using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using BranchPricing.Models;
using RestSharp;
using Dapper;
using Newtonsoft.Json;
using Serilog;
using Helpers;

namespace BranchPricing.Controllers
{
    public class NetSuiteController
    {
        private const string branchPriceSuiteletSandboxUrl = "https://634494-sb1.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1793&deploy=1&compid=634494_SB1&h=c9be2e177e087d1f53c9";
        private const string branchPriceSuiteletUrl = "https://634494.extforms.netsuite.com/app/site/hosting/scriptlet.nl?script=1831&deploy=1&compid=634494&h=3d60d994a63e11c2df72";
        public static List<VendorPrice> branchPricingInNetSuite { get; set; }


        public static IEnumerable<IGrouping<int, BranchPricingFeed>> GetBranchPricingFromFeed()
        {
            string db = "Manhattan";
            string connectionString = $"Data Source=srv-pro-sqls-02;Initial Catalog={db};Integrated Security=true";
            
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT i.ITEM_ID ItemId " +
                                        ", MPID " +
                                        ", LOGON " +
                                        ", BR_LIST " +
                                        ", MSTR_LIST_PRICE " +
                                        ", PRICE_SRC " +
                                        ", ROUND([PRICE_BOOK_NET], 2) PRICE_BOOK_NET " +
                                        ", FORMULA_GRP " +
                                        ", FORMULA_PROD " +
                                   "FROM Manhattan.dbo.InventoryFeeds_BranchPricing bp " +
                                   "JOIN [NetSuite].[data].[ITEMS] i " +
                                   "ON bp.MPID = i.FEI__MASTER_PRODUCT_NUMBER " +
                                   // Limits results to price changes made within past two weeks
                                   "WHERE bp.LAST_PRICE_CHG BETWEEN GETDATE()-14 AND GETDATE()";

                    var branchPrices = connection.Query<BranchPricingFeed>(query, commandTimeout: 500).ToList();

                    connection.Close();

                    // Group results by item on master product id
                    var groupedBranchPrices = branchPrices.GroupBy(price => price.ItemId);

                    return groupedBranchPrices;

                }
                catch (SqlException ex)
                {
                    var errorMessage = $"A SQL error occurred in GetBranchPricingFromFeed: {ex}";
                    Log.Error(errorMessage);
                    throw ex;
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error in GetBranchPricingFromFeed. Error: {ex}";
                    Log.Error(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
        }


        public static void GetVendorPricesInNetSuite()
        {
            string db = "NetSuite";
            string connectionString = $"Data Source=srv-pro-sqls-02;Initial Catalog={db};Integrated Security=true";

            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT FEI__MASTER_PRODUCT_NUMBER MasterProductNumber " +
                                        ", p.VENDOR_ID VendorId " +
                                        ", ROUND(p.COST_0, 2) Price " +
                                   "FROM NetSuite.data.ITEMS i " +
                                   "JOIN NetSuite.data.ITEM_VENDOR_PRICING p " +
                                   "ON i.ITEM_ID = p.ITEM_ID ";

                    branchPricingInNetSuite = connection.Query<VendorPrice>(query, commandTimeout: 500).ToList();

                    connection.Close();

                }
                catch (SqlException ex)
                {
                    var errorMessage = $"A SQL error occurred in GetVendorPricesInNetSuite: {ex}";
                    Log.Error(errorMessage);
                    throw ex;
                }
                catch (Exception ex)
                {
                    var errorMessage = $"Error in GetBranchPricesInNetSuite. Error: {ex}";
                    Log.Error(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
        }


        public static NetSuiteRequest CreateRequest(IGrouping<int, BranchPricingFeed> itemPrices)
        {
            var netsuiteRequest = new NetSuiteRequest();
            netsuiteRequest.ItemId = itemPrices.Key;
            netsuiteRequest.MasterProductNumber = itemPrices.First().MPID;

            // Set Prices
            netsuiteRequest.BranchPrices = new List<BranchPrice>();

            // Set Branch prices
            foreach (var line in itemPrices)
            {
                var branchPrice = new BranchPrice();

                branchPrice.BranchName = line.LOGON;
                branchPrice.SetVendorId();
                branchPrice.Price = line.PRICE_BOOK_NET;

                netsuiteRequest.BranchPrices.Add(branchPrice);
            }

            return netsuiteRequest;
        }


        public static List<BranchPrice> CheckForPricingUpdates(NetSuiteRequest netsuiteRequest)
        {
            // Loop through the branches in the netsuiteRequest and see if they need to be updated
            var masterProductNumber = netsuiteRequest.MasterProductNumber;
            var branchPrices = netsuiteRequest.BranchPrices;
            var branchesWithoutUpdates = new List<BranchPrice>();

            try
            {
                for (int i = 0; i < branchPrices.Count(); i++)
                {
                    var vendorPrice = branchPricingInNetSuite.Where(vendor =>
                                        vendor.MasterProductNumber == masterProductNumber &&
                                        vendor.VendorId == branchPrices[i].VendorId)
                                        .FirstOrDefault();

                    if (vendorPrice != null)
                    {
                        if(vendorPrice.Price == branchPrices[i].Price)
                        {
                            branchesWithoutUpdates.Add(branchPrices[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error in CheckForPricingUpdates. Error: {ex}";
                Log.Error(errorMessage);
                string title = "Error in CheckForPricingUpdates";
                string color = "yellow";
                TeamsHelper teamsMessage = new TeamsHelper(title, errorMessage, color, Program.teamsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
            }

            return branchesWithoutUpdates;
        }


        public static void RemoveBranchesWithoutPricingUpdates(NetSuiteRequest netsuiteRequest, List<BranchPrice> branchesToRemove)
        {
            Log.Information("RemoveBranchesWithoutPricingUpdates called.");
            try
            {
                var branchPrices = netsuiteRequest.BranchPrices;
                
                foreach (var branch in branchesToRemove)
                {
                    branchPrices.Remove(branch);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error in RemoveBranchesWithoutPricingUpdates. Error: {ex}";
                Log.Error(errorMessage);
                string title = "Error in RemoveBranchesWithoutPricingUpdates";
                string color = "yellow";
                TeamsHelper teamsMessage = new TeamsHelper(title, errorMessage, color, Program.teamsUrl);
                teamsMessage.LogToMicrosoftTeams(teamsMessage);
            }
        }


        public static void SendToNetSuite(NetSuiteRequest netsuiteRequest)
        {
            try
            {
                var client = new RestClient(branchPriceSuiteletUrl);
                var jsonRequest = JsonConvert.SerializeObject(netsuiteRequest);
                Log.Information("Sending to Suitelet: jsonRequest {jsonRequest}", jsonRequest);

                var request = new RestRequest(Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.AddParameter("branchPricingData", jsonRequest);

                var suiteletResponse = client.Execute(request);

                if (suiteletResponse.Content != null && suiteletResponse.Content[0] != '<')
                {
                    var parsedSuiteletResponse = JsonConvert.DeserializeObject<NetSuiteResponse>(suiteletResponse.Content);
                    Log.Information("Parsed Suitelet response {@parsedSuiteletResponse}", parsedSuiteletResponse);

                    if (parsedSuiteletResponse == null)
                    {
                        string errorMessage = $"Suitelet returned no response. Item was not updated.";
                        Log.Error(errorMessage);
                        string title = "Error in BranchPricing SendToNetSuite";
                        string color = "red";
                        TeamsHelper teamsMessage = new TeamsHelper(title, errorMessage, color, Program.teamsUrl);
                        teamsMessage.LogToMicrosoftTeams(teamsMessage);
                    }
                    else if (parsedSuiteletResponse.result == "Success")
                    {
                        Log.Information($"ItemId {netsuiteRequest.ItemId} updated successfully.");
                    }
                    else if (parsedSuiteletResponse.result == "Error")
                    {
                        string errorMessage = $"Suitelet returned error response: {parsedSuiteletResponse.error}";
                        Log.Error(errorMessage);
                        string title = "Error in BranchPricing SendToNetSuite";
                        string color = "red";
                        TeamsHelper teamsMessage = new TeamsHelper(title, errorMessage, color, Program.teamsUrl);
                        teamsMessage.LogToMicrosoftTeams(teamsMessage);
                    }

                }
                else
                {
                    Log.Information("suiteletResponse {suiteletResponse}", suiteletResponse);
                    Log.Information("NetSuite response could not be parsed.");
                }
            }
            catch(Exception ex)
            {
                Log.Error($"Error sending request to NetSuite. {ex}");
            }
        }
    }
}
