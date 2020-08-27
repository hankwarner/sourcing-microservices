using System;
using System.Collections.Generic;
using System.Linq;
using ServiceSourcing.Models;
using System.Threading.Tasks;

namespace ServiceSourcing.Services
{
    public interface IItemDataService
    {
        Dictionary<int, ItemData> RequestItemDataByMPN(List<int> MPNs);
    }
}
