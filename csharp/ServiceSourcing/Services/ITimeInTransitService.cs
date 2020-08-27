using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceSourcing.Models;

namespace ServiceSourcing.Services
{
    public interface ITimeInTransitService
    {
        Dictionary<string, List<KeyValuePair<string, DateTime>>> GetTimeInTransit(TimeInTransitRequestData timeInTransitData);
    }
}
