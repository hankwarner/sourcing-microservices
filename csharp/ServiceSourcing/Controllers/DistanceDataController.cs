using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceSourcing.Models;
using ServiceSourcing.Services;
using Serilog;
using Newtonsoft.Json;
using Google.Protobuf.WellKnownTypes;

namespace ServiceSourcing.Controllers
{
    [ApiVersion("2")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class DistanceDataController : ControllerBase
    {
        private readonly IDistanceDataServices _services;
        private readonly ILocationServices _locationServices;
        private readonly IGoogleDistanceServices _googleServices;

        public DistanceDataController(IDistanceDataServices services, ILocationServices locationServices, IGoogleDistanceServices googleServices)
        {
            _services = services;
            _locationServices = locationServices;
            _googleServices = googleServices;
        }


        [HttpGet]
        [Route("GetBranchDistanceData/{masterProductNumber}/{zip}/{milesAllowedFromZip}")]
        public ActionResult<List<DistanceData>> GetBranchDistanceData(string masterProductNumber, string zip, int milesAllowedFromZip)
        {
            Log.Information($"GetBranchDistanceData called. MPN {masterProductNumber}, Zip {zip}, Miles allowed from zip {milesAllowedFromZip}");
            var distanceData = new List<DistanceData>();

            try
            {
                // Remove last four digits from zip
                zip = zip.Split("-")[0];

                distanceData = _services.RequestBranchDistanceData(masterProductNumber, zip, milesAllowedFromZip);

                if (distanceData == null)
                {
                    Log.Error("distanceData returned a null value");
                    return NotFound("distanceData returned a null value");
                }

            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in GetBranchDistanceData: {ex}";
                Log.Error(errorMessage);
                var error = new DistanceData();
                error.Error = errorMessage;
                distanceData.Add(error);
            }

            Log.Information($"Response: {JsonConvert.SerializeObject(distanceData)}");
            return distanceData;
        }


        [HttpGet]
        [Route("GetZipCodePrecisionDeliveryEligibility/{zip}/{milesAllowedFromZip}")]
        public ActionResult<bool> GetZipCodePrecisionDeliveryEligibility(string zip, int milesAllowedFromZip)
        {
            Log.Information($"GetZipCodePrecisionDeliveryEligibility called. Zip {zip}, Miles allowed from zip {milesAllowedFromZip}");
            bool isZipPrecisionDeliveryEligible = false;
            
            try
            {
                // Remove last four digits from zip
                zip = zip.Split("-")[0];

                var eligibleBranches = _services.RequestZipCodePrecisionDeliveryEligibility(zip, milesAllowedFromZip);

                if(eligibleBranches.Count > 0)
                {
                    isZipPrecisionDeliveryEligible = true;
                }

            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in GetZipCodePrecisionDeliveryEligibility: {ex}";
                Log.Error(errorMessage);
                return NotFound(errorMessage);
            }

            return isZipPrecisionDeliveryEligible;
        }


        [HttpPost]
        [Route("PostBranchDistanceData")]
        public ActionResult<Dictionary<string, List<DistanceData>>> PostBranchDistanceData([FromBody] List<RequestForDistanceData> request)
        {
            Log.Information($"PostBranchDistanceData called. Request: {JsonConvert.SerializeObject(request)}");
            var response = new Dictionary<string, List<DistanceData>>();

            try
            {
                var sqlWhereClause = CreateMultipleBranchDistanceDataQuery(request);

                response = _services.RequestMultipleItemDistanceData(sqlWhereClause);

                // Add an empty array for items with no available PDE branches
                HandleItemsWithNoDistanceData(request, response);

            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in PostBranchDistanceData: {ex}";
                Log.Error(errorMessage);

                return NotFound(errorMessage);
            }

            Log.Information($"Response: {JsonConvert.SerializeObject(response)}");
            return response;
        }


        private string CreateMultipleBranchDistanceDataQuery(List<RequestForDistanceData> request)
        {
            var sqlWhereClause = " WHERE ";
            var totalCount = request.Count();


            for (int i=0; i < totalCount; i++)
            {
                var zip = request[i].Zip;
                var masterProductNumber = request[i].MasterProductNumber;
                var milesAllowedFromZip = request[i].MilesAllowedFromZip;

                sqlWhereClause += $"(dist.ZipCode = '{zip}' " +
                                  $"AND inv.MPID = '{masterProductNumber}' " +
                                  $"AND CEILING(DistanceInMeters * 0.0006213712) <= {milesAllowedFromZip} " +
                                   "AND inv.QTY > 0 " +
                                   "AND sfl.PRECISION_DELIVERY = 'T') ";

                // Add 'OR' if it's not the last item
                if ((i + 1) != totalCount)
                {
                    sqlWhereClause += " OR ";
                }

            }

            return sqlWhereClause;
        }


        private void HandleItemsWithNoDistanceData(List<RequestForDistanceData> request, Dictionary<string, List<DistanceData>> response)
        {
            try
            {
                foreach (var item in request)
                {
                    var mpn = item.MasterProductNumber;

                    // For empty results sets, add a dictionary entry with an empty list
                    if (!response.ContainsKey(mpn))
                    {
                        var emptyList = new List<DistanceData>();
                        response.Add(mpn, emptyList);
                    }
                }
            }
            catch(Exception ex)
            {
                string errorMessage = $"Error in HandleItemsWithNoDistanceData: {ex}";
                Log.Error(errorMessage);
            }
        }


        [HttpGet]
        [Route("GetLocationsWithinMiles/{zip}/{distanceInMiles?}")]
        public ActionResult<List<DistanceData>> GetLocationsWithinMiles(string zip, int? distanceInMiles = null)
        {
            Log.Information($"Zip {zip}, distanceInMiles {distanceInMiles}");
            var distanceData = new List<DistanceData>();

            try
            {
                // Remove last four digits from zip
                zip = zip.Split("-")[0];

                distanceData = _services.RequestLocationsWithinMiles(zip, distanceInMiles);

                if (distanceData == null)
                {
                    Log.Error("distanceData returned a null value");
                    return NotFound("distanceData returned a null value");
                }

            }
            catch (Exception ex)
            {
                string errorMessage = $"Error in GetLocationsWithinMiles: {ex}";
                Log.Error(errorMessage);
                var error = new DistanceData();
                error.Error = errorMessage;
                distanceData.Add(error);
            }

            return distanceData;
        }

        [HttpPost("GetBranchDistancesByZipCode/{zipCode}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<Dictionary<string, double>> GetBranchDistancesByZipCode([FromRoute] string zipCode, [FromBody] List<string> branches)
        {
            Log.Information($"GetBranchDistancesByZipCode called. ZipCode: {zipCode}. Branches: {branches}");
            try
            {
                var branchesWithDistance = _services.RequestBranchDistancesByZipCode(zipCode, branches);

                if (branchesWithDistance == null || branches.Count() != branchesWithDistance.Count())
                {
                    try
                    {
                        var missingBranches = branches.Where(b1 => !branchesWithDistance.Any(b2 => b1 == b2.Key)).ToList();
                        var missingDistanceData = GetMissingBranchDistances(zipCode, missingBranches);
                        foreach (var newDistance in missingDistanceData)
                        {
                            branchesWithDistance.Add(newDistance.Key, newDistance.Value);
                        }
                    }
                    catch(Exception ex)
                    {
                        string errorMessage = $"Exception in GetMissingBranchDistances: ";
                        Log.Error(ex, errorMessage);
                    }
                }

                if (branchesWithDistance != null && branchesWithDistance.Count() != 0) return Ok(branchesWithDistance);

                Log.Error($"no Distances found for zipCode: {zipCode} and Branches: {branches}");
                return NotFound($"no Distances found for zipCode: {zipCode} and Branches: {branches}");

            }
            catch (Exception ex)
            {
                var details = new ProblemDetails
                {
                    Detail = ex.StackTrace,
                    Title = ex.Message
                };
                return StatusCode(500, details);
            }
        }

        private IEnumerable<KeyValuePair<string, double>> GetMissingBranchDistances(string zipCode, List<string> missingBranches)
        {
            var originsForGoogle = _locationServices.GetOriginDataForGoogle(missingBranches);
            var branchDistancesFromGoogle = _googleServices.GetDistanceDataFromGoogle(zipCode, originsForGoogle);

            _ = Task.Run(() =>
            {
                SaveDistanceData(branchDistancesFromGoogle);
            });

            return (from distributionCenterDistance in branchDistancesFromGoogle
                let miles = Math.Ceiling(distributionCenterDistance.DistanceInMeters * 0.0006213712)
                select new KeyValuePair<string, double>(distributionCenterDistance.BranchNumber, miles)).ToList();
        }

        private void SaveDistanceData(List<DistributionCenterDistance> newBranchDistances)
        {
            try
            {
                var success = _locationServices.SaveBranchDistanceData(newBranchDistances);
                if (success) return;
                var errorMessage = $"Error in SaveDistanceData";
                Log.Error(errorMessage);
            }
            catch (Exception ex)
            {
                var errorMessage = $"Error in SaveDistanceData: {ex}";
                Log.Error(errorMessage);
            }
        }

    }
}