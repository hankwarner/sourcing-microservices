using ServiceSourcing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Services
{
    public interface ILocationServices
    {
        string GetBranchLogonID(string branchNumber);

        Dictionary<string, LocationData> GetLogonLocationData(string branch);

        List<GoogleOriginData> GetOriginDataForGoogle(List<string> branches);

        bool SaveBranchDistanceData(List<DistributionCenterDistance> newBranchDistances);

        List<LocationData> RequestLocationAddressAndWMS(List<string> logons);

        List<LocationData> GetBranchesByState(string state);
    }
}
