using ServiceSourcing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Services
{
    public interface IGoogleDistanceServices
    {
        List<DistributionCenterDistance> GetDistanceDataFromGoogle(string destination, List<GoogleOriginData> branches);
    }
}
