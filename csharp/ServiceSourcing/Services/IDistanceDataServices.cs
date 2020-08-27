using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServiceSourcing.Models;

namespace ServiceSourcing.Services
{
    public interface IDistanceDataServices
    {
        List<DistanceData> RequestBranchDistanceData(string masterProductNumber, string zip, int milesAllowedFromZip);

        Dictionary<string, List<DistanceData>> RequestMultipleItemDistanceData(string sqlWhereClause);

        List<DistanceData> RequestLocationsWithinMiles(string zip, int? distanceInMiles);

        List<DistanceData> RequestZipCodePrecisionDeliveryEligibility(string zip, int milesAllowedFromZip);

        Dictionary<string, double> RequestBranchDistancesByZipCode(string zipCode, List<string> branches);
    }
}
