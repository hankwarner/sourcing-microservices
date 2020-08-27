using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceSourcing.Models;
using Dapper;
using Serilog;
using System.Data.SqlClient;
using System.Data;
using ServiceSourcing.Services;
using Microsoft.IdentityModel.Tokens;

namespace ServiceSourcing.Services
{
    public class DistanceDataServices : IDistanceDataServices
    {
        private string connectionString = Environment.GetEnvironmentVariable("CONN_SQL_SRV_02");

        public List<DistanceData> RequestBranchDistanceData(string masterProductNumber, string zip, int milesAllowedFromZip)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var query = @"
                        SELECT CEILING(dist.DistanceInMeters * 0.0006213712) DistanceInMiles 
                            , dist.DistributionCenterNetSuiteId 
                            , inv.QTY Quantity 
                        FROM NetSuite.data.SHIP_FROM_LOCATION sfl 
                        JOIN [Manhattan].[dbo].[InventoryFeeds_AtcView_Supply] inv 
                        ON inv.LOCATION = sfl.BRANCH_NUMBER 
                        JOIN [FergusonIntegration].[ferguson].[DistributionCenterDistance] dist 
                        ON dist.DistributionCenterNetSuiteId = sfl.SHIP_FROM_LOCATION_ID 
                        WHERE dist.ZipCode = @zip 
                            AND inv.MPID = @masterProductNumber 
                            AND CEILING(DistanceInMeters * 0.0006213712) <= @milesAllowedFromZip 
                            AND inv.QTY > 0 
                            AND sfl.PRECISION_DELIVERY = 'T'";

                    var distanceData = connection.Query<DistanceData>(query, new { zip, masterProductNumber, milesAllowedFromZip }).ToList();

                    connection.Close();

                    return distanceData;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = $"Sql Exception in RequestBranchDistanceData: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in RequestBranchDistanceData: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }


        public List<DistanceData> RequestZipCodePrecisionDeliveryEligibility(string zip, int milesAllowedFromZip)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var query = @"
                        SELECT CEILING(dist.DistanceInMeters * 0.0006213712) DistanceInMiles 
                            , dist.DistributionCenterNetSuiteId 
                        FROM NetSuite.data.SHIP_FROM_LOCATION sfl 
                        JOIN [Manhattan].[dbo].[InventoryFeeds_AtcView_Supply] inv 
                        ON inv.LOCATION = sfl.BRANCH_NUMBER 
                        JOIN [FergusonIntegration].[ferguson].[DistributionCenterDistance] dist 
                        ON dist.DistributionCenterNetSuiteId = sfl.SHIP_FROM_LOCATION_ID 
                        WHERE dist.ZipCode = @zip 
                            AND CEILING(DistanceInMeters * 0.0006213712) <= @milesAllowedFromZip 
                            AND sfl.PRECISION_DELIVERY = 'T' 
                        GROUP BY CEILING(dist.DistanceInMeters * 0.0006213712) 
                            , dist.DistributionCenterNetSuiteId ";

                    var distanceData = connection.Query<DistanceData>(query, new { zip, milesAllowedFromZip }).ToList();

                    connection.Close();

                    return distanceData;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = $"Sql Exception in RequestZipCodePrecisionDeliveryEligibility: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in RequestZipCodePrecisionDeliveryEligibility: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }


        public Dictionary<string, List<DistanceData>> RequestMultipleItemDistanceData(string sqlWhereClause)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var query = @"
                        SELECT CEILING(dist.DistanceInMeters * 0.0006213712) DistanceInMiles 
                            , dist.DistributionCenterNetSuiteId 
                            , inv.QTY Quantity 
                            , inv.MPID 
                        FROM NetSuite.data.SHIP_FROM_LOCATION sfl 
                        JOIN [Manhattan].[dbo].[InventoryFeeds_AtcView_Supply] inv 
                        ON inv.LOCATION = sfl.BRANCH_NUMBER 
                        JOIN [FergusonIntegration].[ferguson].[DistributionCenterDistance] dist
                        ON dist.DistributionCenterNetSuiteId = sfl.SHIP_FROM_LOCATION_ID @sqlWhereClause ";

                    var distanceData = connection.Query<DistanceData>(query, new { sqlWhereClause }, commandTimeout: 6).ToList();

                    var groupedDistanceData = distanceData.GroupBy(line => line.MPID);

                    // Create a Dictionary of <masterProductNumber, List<DistanceData>>
                    var response = new Dictionary<string, List<DistanceData>>();

                    foreach (var item in groupedDistanceData)
                    {
                        response.Add(item.Key, item.ToList());
                    }

                    connection.Close();

                    return response;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = "Sql Exception in RequestMultipleItemDistanceData: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                string errorMessage = "Error in RequestMultipleItemDistanceData: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }


        public List<DistanceData> RequestLocationsWithinMiles(string zip, int? distanceFromZip)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
                        SELECT dc.NetSuiteListId, (DistanceInMeters * 0.0006213712) DistanceInMiles, dc.BranchNumber 
                        FROM [FergusonIntegration].[ferguson].[DistributionCenterDistance] dist 
                        JOIN [FergusonIntegration].[ferguson].[DistributionCenter] dc 
                        ON dist.DistributionCenterId = dc.Id 
                        WHERE ZipCode = @zip AND (DistanceInMeters * 0.0006213712) <= @distanceFromZip ";

                    var distanceData = connection.Query<DistanceData>(query, new { zip, distanceFromZip }).ToList();

                    connection.Close();

                    return distanceData;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = "Sql Exception in RequestLocationsWithinMiles: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                string errorMessage = "Error in RequestLocationsWithinMiles: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }


        public Dictionary<string, double> RequestBranchDistancesByZipCode(string zipCode, List<string> branches)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    
                    var query = @"
                        SELECT BranchNumber, CEILING(DistanceInMeters * 0.0006213712) DistanceInMiles 
                        FROM [FergusonIntegration].[sourcing].[DistributionCenterDistance] 
                        WHERE ZipCode = @zipCode AND BranchNumber in @branches";

                    var branchesWithDistanceList = conn.Query(query, new { zipCode, branches}, commandTimeout: 6).ToDictionary(
                        row => (string)row.BranchNumber,
                        row => (double)row.DistanceInMiles).ToList();

                    Dictionary<string, double> branchesWithDistance = branchesWithDistanceList.ToDictionary(pair => pair.Key, pair => pair.Value);

                    conn.Close();

                    return branchesWithDistance;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = "Sql Exception in GetBranchDistancesByZipCode: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in GetBranchDistancesByZipCode: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }
    }
}
