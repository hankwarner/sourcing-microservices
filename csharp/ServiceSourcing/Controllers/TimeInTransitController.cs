using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceSourcing.Models;
using ServiceSourcing.Services;
using Serilog;
using Google.Type;

namespace ServiceSourcing.Controllers
{
    [ApiVersion("2")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class TimeInTransitController : ControllerBase
    {
        private readonly ITimeInTransitService _services;

        public TimeInTransitController(ITimeInTransitService services)
        {
            _services = services;
        }

        [HttpPost("GetEstimatedArrival")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<Dictionary<string, List<KeyValuePair<string, DateTime>>>> GetEstimatedArrival(TimeInTransitRequestData timeInTransitData)
        {
            Log.Information($"GetTimeInTransit called. ShippingData: {timeInTransitData}.");
            try
            {
                var timeInTransitResults = _services.GetTimeInTransit(timeInTransitData);

                return Ok(timeInTransitResults);
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
