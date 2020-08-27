using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceSourcing.Models;
using ServiceSourcing.Services;
using Serilog;
using Newtonsoft.Json.Linq;

namespace ServiceSourcing.Controllers
{
    [ApiVersion("2")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class TwilioController : ControllerBase
    {
        private readonly ITwilioService _services;

        public TwilioController(ITwilioService services) 
        {
            _services = services;
        }

        [HttpPost]
        [Route("SendTextMessage")]
        public ActionResult<string> SendTextMessage(SMS message)
        {
            var result = _services.SendText(message);
            return $"Text Successfully sent to {message.Phone}!";
        }
    }
}