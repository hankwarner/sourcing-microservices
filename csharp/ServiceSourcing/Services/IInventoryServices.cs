using ServiceSourcing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Services
{
    public interface IInventoryServices
    {
        List<LocationInventory> RequestItemInventory(List<string> masterProductNumber);

        Dictionary<string, Dictionary<string, int>> RequestMultipleItemsInventory(List<string> masterProductNumbers);

        Dictionary<int, Dictionary<int, bool>> RequestStockingStatus(List<string> masterProductNumbers);

        List<Item> RequestKitMembers(string kitID);
    }
}
