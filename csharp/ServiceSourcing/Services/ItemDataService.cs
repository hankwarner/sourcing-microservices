using System;
using System.Collections.Generic;
using System.Linq;
using ServiceSourcing.Models;
using Dapper;
using Serilog;
using System.Data.SqlClient;

namespace ServiceSourcing.Services
{
    public class ItemDataService : IItemDataService
    {
        private string connectionString = Environment.GetEnvironmentVariable("CONN_SQL_SRV_02");

        public Dictionary<int, ItemData> RequestItemDataByMPN(List<int> MPNs)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var query = @"
                        SELECT MPN, ItemCategory, Manufacturer, BulkPack, BulkPackQuantity, PreferredShippingMethod, Weight, SourcingGuideline, Vendor,
                            CASE WHEN [StockingStatus533] = 'Stocking' THEN 1 WHEN [StockingStatus533] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus533],
                            CASE WHEN [StockingStatus423] = 'Stocking' THEN 1 WHEN [StockingStatus423] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus423],
                            CASE WHEN [StockingStatus761] = 'Stocking' THEN 1 WHEN [StockingStatus761] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus761],
                            CASE WHEN [StockingStatus2911] = 'Stocking' THEN 1 WHEN [StockingStatus2911] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus2911],
                            CASE WHEN [StockingStatus2920] = 'Stocking' THEN 1 WHEN [StockingStatus2920] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus2920],
                            CASE WHEN [StockingStatus474] = 'Stocking' THEN 1 WHEN [StockingStatus474] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus474],
                            CASE WHEN [StockingStatus986] = 'Stocking' THEN 1 WHEN [StockingStatus986] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus986],
                            CASE WHEN [StockingStatus321] = 'Stocking' THEN 1 WHEN [StockingStatus321] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus321],
                            CASE WHEN [StockingStatus625] = 'Stocking' THEN 1 WHEN [StockingStatus625] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus625],
                            CASE WHEN [StockingStatus688] = 'Stocking' THEN 1 WHEN [StockingStatus688] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus688],
                            CASE WHEN [StockingStatus796] = 'Stocking' THEN 1 WHEN [StockingStatus796] = 'Non-Stocking' THEN 0 ELSE null END [StockingStatus796]
                            FROM [FergusonIntegration].[ferguson].[Items] WHERE MPN in @MPNs ";

                    var MPNsWithItemDataList = conn.Query<ItemData>(query, new { MPNs }, commandTimeout: 6).ToDictionary(
                        row => row.MPN,
                        row => row).ToList();

                    var missingItems = MPNs.Where(x => MPNsWithItemDataList.All(y => y.Key != x)).ToList();

                    var MPNsWithItemData = MPNsWithItemDataList.ToDictionary(pair => pair.Key, pair => pair.Value);

                    foreach(var item in missingItems)
                    {
                        MPNsWithItemData.Add(item, null);
                    }

                    conn.Close();

                    return MPNsWithItemData;
                }
            }
            catch (SqlException ex)
            {
                var errorMessage = "Sql Exception in GetBranchNumbersByLogon: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                var errorMessage = "Error in GetBranchNumbersByLogon: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }
    }
}
