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
using Grpc.Core;

namespace ServiceSourcing.Controllers
{
    [ApiVersion("2")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ILocationServices _services;
        private readonly IDistanceDataServices _distanceServices;
        private readonly IGoogleDistanceServices _googleServices;

        public LocationController(ILocationServices services, IDistanceDataServices distanceServices, IGoogleDistanceServices googleServices)
        {
            _services = services;
            _distanceServices = distanceServices;
            _googleServices = googleServices;
        }

        [HttpGet("GetBranchLogonID/{branchNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<string> GetBranchLogonID(string branchNumber)
        {
            Log.Information($"GetBranchLogonID called. Branch Number {branchNumber}");
            try
            {
                var logonID = _services.GetBranchLogonID(branchNumber);

                if (logonID != null) return Ok(logonID);

                Log.Error($"no Logon found for BranchNumber: {branchNumber}");
                return NotFound($"No Logon found for BranchNumber: {branchNumber}");

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

        [HttpGet("GetLogonLocationData/{branchNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<Dictionary<string, LocationData>> GetLogonLocationData(string branchNumber)
        {
            try
            {
                var logonLocationData = _services.GetLogonLocationData(branchNumber);

                if (logonLocationData != null) return Ok(logonLocationData);

                Log.Error($"no Branches found for Logons: {branchNumber}");
                return NotFound($"No Branches found for Logons: {branchNumber}");

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


        [HttpPost("GetLocationAddressAndWMSByLogon")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<List<LocationData>> GetLocationAddressAndWMSByLogon([FromBody] List<string> logons)
        {
            Log.Information(@"GetLocationAddressAndWMSByLogon called. Logons {Logons}", logons);
            try
            {
                var locationAddressAndWMS = _services.RequestLocationAddressAndWMS(logons);

                if (locationAddressAndWMS != null) return Ok(locationAddressAndWMS);

                Log.Error($"no location data found for Logons: {logons}");
                return NotFound($"No location data found for Logons: {logons}");

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

        [HttpGet("GetLogonByLocation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<LocationData> GetLogonByLocation([FromQuery] string zipCode, [FromQuery] string state)
        {
            try
            {
                var locationData = _services.GetBranchesByState(state);

                var branchesInState = locationData.Select(l => l.BranchNumber).ToList();

                var distanceController = new DistanceDataController(_distanceServices, _services, _googleServices);
                var res = distanceController.GetBranchDistancesByZipCode(zipCode, branchesInState);

                var actionResult = res.Result as OkObjectResult;
                var locationDict = actionResult.Value as Dictionary<string, double>;

                var closestDistance = locationDict.Min(b => b.Value);
                var closestBranch = locationDict.FirstOrDefault(b => b.Value == closestDistance).Key;

                var logon = locationData.FirstOrDefault(l => l.BranchNumber == closestBranch);

                return logon;
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
    }
}
