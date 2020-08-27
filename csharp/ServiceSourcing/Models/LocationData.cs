using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Models
{
    public class LocationData
    {
        public string BranchNumber { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public bool WarehouseManagementSoftware { get; set; }
        public bool BranchLocation { get; set; }
        public bool DCLocation { get; set; }
        public bool SODLocation { get; set; }
        public string Logon { get; set; }
    }
}
