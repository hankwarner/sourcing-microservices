using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceSourcing.Models;
using Dapper;
using Serilog;
using System.Data.SqlClient;
using System.Data;

namespace ServiceSourcing.Services
{
    public class LocationServices : ILocationServices
    {
        private string connectionString = Environment.GetEnvironmentVariable("CONN_SQL_SRV_02");

        public string GetBranchLogonID(string branchNumber)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var query = @"
                        SELECT Logon FROM [FergusonIntegration].[sourcing].[DistributionCenter]
                        WHERE BranchNumber = @branchNumber";

                    var logonId = conn.Query<string>(query, new { branchNumber }, commandTimeout: 6).FirstOrDefault();

                    conn.Close();

                    return logonId;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = $"Sql Exception in GetBranchLogonID: ";
                Log.Error(ex, errorMessage);
                throw ex;
            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in GetBranchLogonID: ";
                Log.Error(ex, errorMessage);
                throw ex;
            }
        }

        public Dictionary<string, LocationData> GetLogonLocationData(string branch)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var query = @"
                                SELECT BranchNumber, Address1, Address2, City, State, Zip, WarehouseManagementSoftware, BranchLocation, DCLocation, SODLocation, Logon
                                FROM [FergusonIntegration].[sourcing].[DistributionCenter] WHERE Logon in 
                                    ('Dist', 
                                    (SELECT Logon FROM [FergusonIntegration].[sourcing].[DistributionCenter] WHERE BranchNumber = @branch))
                                AND Active = 1 AND Zip != '0'";

                    var results = conn.Query<LocationData>(query, new { branch }, commandTimeout: 3).ToList();

                    var logonLocationData = results.ToDictionary(locationData => locationData.BranchNumber);

                    conn.Close();

                    return logonLocationData;
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

        public List<GoogleOriginData> GetOriginDataForGoogle(List<string> branches)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var query = @"
                        SELECT BranchNumber, Latitude, Longitude, Address1, Address2, City, State, Zip 
                        FROM [FergusonIntegration].[sourcing].[DistributionCenter]
                        WHERE BranchNumber in @branches";

                    var originData = conn.Query<GoogleOriginData>(query, new { branches }, commandTimeout: 6).ToList();

                    conn.Close();

                    return originData;
                }
            }
            catch (SqlException ex)
            {
                var errorMessage = "Sql Exception in GetBranchLogonID: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                var errorMessage = "Error in GetBranchLogonID: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }


        public List<LocationData> RequestLocationAddressAndWMS(List<string> logons)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    const string query = @"
                        SELECT BranchNumber, Address1, Address2, City, State, Zip
	                        , CASE WHEN Logon = 'Dist' THEN 1 ELSE WarehouseManagementSoftware END HasWMS
                        FROM [FergusonIntegration].[sourcing].[DistributionCenter]
                        WHERE Logon in @logons AND Active = 1 AND Zip != '0'";

                    var locationData = connection.Query<LocationData>(query,
                        new { logons },
                        commandTimeout: 6)
                        .ToList();

                    connection.Close();

                    return locationData;
                }
            }
            catch (SqlException ex)
            {
                var errorMessage = "Sql Exception in RequestLocationAddressAndWMS: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                var errorMessage = "Error in RequestLocationAddressAndWMS: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }

        public List<LocationData> GetBranchesByState(string state)
        {
            try
            {
                var states = new List<string>() { state };

                if (state == "CT" || state == "RI" || state == "VT")
                {
                    var boardingStates = GetNeighboringStates(state);

                    states.AddRange(boardingStates);
                }
                
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    var query = @"
                        SELECT BranchNumber,Logon
                        FROM [FergusonIntegration].[sourcing].[DistributionCenter]
                        WHERE State in @states AND Logon != 'Dist' AND Logon IS NOT NULL";

                    var branchesInState = conn.Query<LocationData>(query, new { states }, commandTimeout: 3).ToList();

                    conn.Close();

                    return branchesInState;
                }
            }
            catch (SqlException ex)
            {
                string errorMessage = "Sql Exception in GetBranchLogonID: ";
                Log.Error(ex, errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                string errorMessage = "Error in GetBranchLogonID: ";
                Log.Error(ex, errorMessage);
                throw;
            }
        }

        public List<string> GetNeighboringStates(string state)
        {
            var boardingStates = new List<string>();

            switch (state)
            {
                case "CT":
                    boardingStates.Add("NY");
                    boardingStates.Add("MA");
                    break;
                case "RI":
                    boardingStates.Add("MA");
                    break;
                case "VT":
                    boardingStates.Add("NH");
                    boardingStates.Add("NY");
                    boardingStates.Add("MA");
                    break;
            }

            return boardingStates;
        }
    }
}
