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
    public class ItemDataController : ControllerBase
    {
        private readonly IItemDataService _services;

        public ItemDataController(IItemDataService services)
        {
            _services = services;
        }

        [HttpPost("GetItemDataByMPN")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<Dictionary<string, List<ItemData>>> GetItemDataByMPN([FromBody] List<int> MPNs)
        {
            Log.Information($"GetItemDataFromMPN called. Logons {MPNs}");
            try
            {
                var MPNsWithItemData = _services.RequestItemDataByMPN(MPNs);

                if (MPNsWithItemData != null) return Ok(MPNsWithItemData);

                Log.Error($"no ItemData found for MPNs: {MPNs}");
                return NotFound($"No ItemData found for MPNs: {MPNs}");

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
