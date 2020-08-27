using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Models
{
    public class LocationInventory
    {
        public string BranchNumber { get; set; }

        public int Quantity { get; set; }

        public string StockingStatus { get; set; }


        [JsonIgnore]
        public string MPN { get; set; }
    }

    public class InventoryData
    {
        public Available Available = new Available();

        public StockStatus StockStatus = new StockStatus();
    }

    public class Available
    {
        public Dictionary<string, int> LocationInventory = new Dictionary<string, int>();
    }

    public class StockStatus
    {
        public Dictionary<int, bool> LocationStockingStatus = new Dictionary<int, bool>();
    }
}
