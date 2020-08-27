using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace ServiceSourcing.Options
{
    public class UPSSSettings
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string AccessLicenseNumber { get; set; }
        public string UpsTimeInTransitApiUrl { get; set; }
    }
}
