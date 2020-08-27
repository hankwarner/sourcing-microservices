using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Models
{
    public class StockingStatus
    {
        public int BranchNumber { get; set; }

        public int MasterProductNumber { get; set; }

        public bool StockingItem { get; set; }
    }
}
