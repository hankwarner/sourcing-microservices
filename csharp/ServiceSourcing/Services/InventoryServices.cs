using Dapper;
using ServiceSourcing.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using System.Data;

namespace ServiceSourcing.Services
{
    public class InventoryServices : IInventoryServices
    {
        private string connectionString = Environment.GetEnvironmentVariable("CONN_SQL_SRV_02");

        public List<LocationInventory> RequestItemInventory(List<string> masterProductNumbers)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "SELECT inv.QTY Quantity " +
                                       ", CASE " +
                                            "WHEN inv.IS_ON_HMW_VIEW = 0 THEN RIGHT('000' + CAST(inv.LOCATION AS VARCHAR(4)), 4) " +
                                            "ELSE CAST(inv.LOCATION as VARCHAR(4)) " +
                                            "END as BranchNumber " +
                                       ", CASE " +
                                            "WHEN inv.IS_ON_HMW_VIEW = 1 THEN feed.Status " +
                                            "ELSE 'N/A' " +
                                            "END as StockingStatus " +
                                        ", inv.MPID MPN " +
                                   "FROM [Manhattan].[dbo].[vwInventoryFeeds_AtcView_Combined] inv " +
                                   "LEFT JOIN [FergusonIntegration].[dbo].[FergusonInventoryFeed] feed " +
                                   "ON inv.MPID = feed.MasterProductNumber and inv.LOCATION = feed.DistributionCenterWarehouse " +
                                   $"WHERE inv.MPID in @masterProductNumbers";

                    var itemInventory = connection.Query<LocationInventory>(
                                            query, 
                                            new { masterProductNumbers },
                                            commandTimeout: 6).ToList();

                    connection.Close();

                    return itemInventory;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = "Error in RequestItemInventory";
                Log.Error(ex, errorMessage);
                throw;
            }
        }


        public Dictionary<string, Dictionary<string, int>> RequestMultipleItemsInventory(List<string> masterProductNumbers)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT inv.QTY Quantity 
                            , CAST(inv.LOCATION as VARCHAR(4)) as BranchNumber
                            , CASE WHEN inv.IS_ON_HMW_VIEW = 1 
                                THEN feed.Status 
                                ELSE 'N/A' 
                                END as StockingStatus 
                            , inv.MPID MPN 
                        FROM [Manhattan].[dbo].[vwInventoryFeeds_AtcView_Combined] inv 
                        LEFT JOIN [FergusonIntegration].[dbo].[FergusonInventoryFeed] feed 
                        ON inv.MPID = feed.MasterProductNumber and inv.LOCATION = feed.DistributionCenterWarehouse " +
                        $"WHERE inv.MPID in @masterProductNumbers";

                    var inventory = connection.Query<LocationInventory>(
                                            query,
                                            new { masterProductNumbers },
                                            commandTimeout: 6).ToList();
                    connection.Close();

                    var groupedInventory = inventory.GroupBy(i => i.MPN);

                    var allInventoryData = groupedInventory
                        .ToDictionary(mpnGroup => mpnGroup.Key, mpnGroup => mpnGroup
                        .ToDictionary(line => line.BranchNumber, line => line.Quantity));

                    return allInventoryData;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = "Error in RequestItemInventory";
                Log.Error(ex, errorMessage);
                throw;
            }
        }


        public Dictionary<int, Dictionary<int, bool>> RequestStockingStatus(List<string> masterProductNumbers)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT DistributionCenterWarehouse AS BranchNumber
	                        , MasterProductNumber
	                        , CASE WHEN [Status] = 'Stocking Item'
		                        THEN CAST(1 AS bit)
		                        ELSE CAST(0 AS bit) END AS StockingItem
                        FROM [FergusonIntegration].[dbo].[FergusonInventoryFeed]
                        WHERE MasterProductNumber in @masterProductNumbers
                        GROUP BY [Status], MasterProductNumber, DistributionCenterWarehouse";

                    var stockingStatuses = connection.Query<StockingStatus>(
                                            query,
                                            new { masterProductNumbers },
                                            commandTimeout: 4).ToList();
                    connection.Close();

                    var groupedStockingStatuses = stockingStatuses.GroupBy(i => i.MasterProductNumber);

                    var allStockingStatusData = groupedStockingStatuses
                        .ToDictionary(mpnGroup => mpnGroup.Key, mpnGroup => mpnGroup
                        .ToDictionary(line => line.BranchNumber, line => line.StockingItem));

                    return allStockingStatusData;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = "Error in RequestItemInventory";
                Log.Error(ex, errorMessage);
                throw;
            }
        }


        public List<Item> RequestKitMembers(string kitID)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT i.FEI__MASTER_PRODUCT_NUMBER MPN 
                            , ig.QUANTITY Quantity 
                        FROM [NetSuite].[data].[ITEM_GROUP] ig 
                        INNER JOIN [NetSuite].[data].[ITEMS] i 
                        ON ig.MEMBER_ID = i.ITEM_ID 
                        WHERE ig.PARENT_ID = @kitID";

                    var memberItems = connection.Query<Item>(
                                            query,
                                            new { kitID },
                                            commandTimeout: 6).ToList();

                    connection.Close();

                    return memberItems;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = "Error in RequestKitMembers";
                Log.Error(ex, errorMessage);
                throw;
            }
        }
    }
}
