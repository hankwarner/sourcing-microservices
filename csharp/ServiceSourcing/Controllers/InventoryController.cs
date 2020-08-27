using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServiceSourcing.Models;
using ServiceSourcing.Services;
using Serilog;
using Newtonsoft.Json.Linq;

namespace ServiceSourcing.Controllers
{
    [ApiVersion("2")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryServices _services;
        public static List<Item> kitMemberItems { get; set; }


        public InventoryController(IInventoryServices services)
        {
            _services = services;
        }


        [HttpGet]
        [Route("GetItemInventory")]
        public ActionResult<List<LocationInventory>> GetItemInventory([FromQuery] List<string> masterProductNumber)
        {
            try
            {
                Log.Information("MPN {@MasterProductNumber}", masterProductNumber);
                var itemInventory = _services.RequestItemInventory(masterProductNumber);

                if (itemInventory == null || itemInventory.Count() == 0)
                {
                    Log.Information("Inventory data not available.");
                    return NotFound();
                }

                return itemInventory;

            }
            catch(Exception ex)
            {
                Log.Error(ex, "Error in GetItemInventory");
                return NotFound();
            }
        }

        [HttpGet]
        [Route("GetMultipleItemsInventory")]
        public ActionResult<Dictionary<string, Dictionary<string, int>>> GetMultipleItemsInventory([FromQuery] List<string> masterProductNumbers)
        {
            try
            {
                Log.Information("MPNs {@MasterProductNumbers}", masterProductNumbers);
                var groupedItemInventory = _services.RequestMultipleItemsInventory(masterProductNumbers);

                if (groupedItemInventory == null || groupedItemInventory.Count() == 0)
                {
                    Log.Information("Inventory data not available.");
                    return NotFound();
                }

                return groupedItemInventory;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetMultipleItemsInventory");
                return NotFound();
            }
        }


        [HttpGet]
        [Route("GetStockingStatus")]
        public ActionResult<Dictionary<int, Dictionary<int, bool>>> GetStockingStatus([FromQuery] List<string> masterProductNumbers)
        {
            try
            {
                Log.Information("MPNs {@MasterProductNumbers}", masterProductNumbers);
                var groupedStockingStatuses = _services.RequestStockingStatus(masterProductNumbers);

                if (groupedStockingStatuses == null || groupedStockingStatuses.Count() == 0)
                {
                    Log.Information("Stocking status data not available.");
                    return NotFound();
                }

                return groupedStockingStatuses;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in GetMultipleItemsInventory");
                return NotFound();
            }
        }


        [HttpGet]
        [Route("GetKitInventory")]
        public ActionResult<List<LocationInventory>> GetKitInventory([FromQuery] string kitID)
        {
            try
            {
                Log.Information($"Kit ID {kitID}");

                kitMemberItems = _services.RequestKitMembers(kitID);

                var masterProductNumbers = kitMemberItems.Select(l => l.MPN).ToList();

                var inventoryResponse = _services.RequestItemInventory(masterProductNumbers);

                if (inventoryResponse == null || inventoryResponse.Count() == 0)
                {
                    Log.Information("Inventory data not available.");
                    return NotFound();
                }

                var inventoryByLocation = inventoryResponse.GroupBy(l => l.BranchNumber).ToList();

                var kitInventory = new List<LocationInventory>();

                foreach(var locationItemInventory in inventoryByLocation)
                {
                    var locationKitInventory = new LocationInventory();
                    locationKitInventory.BranchNumber = locationItemInventory.Key;

                    // Sometimes the location is included in the inventory response but will have 0 quantity
                    var zeroQuantityLines = locationItemInventory.Where(l => l.Quantity == 0)
                                                .Select(i => i.MPN)
                                                .Count();

                    var isMissingMemberItems = locationItemInventory.Count() < kitMemberItems.Count();

                    locationKitInventory.StockingStatus = GetStockingStatus(locationItemInventory);

                    // If the line doesn't include all member items, we know there is not enough to build a complete kit
                    if (isMissingMemberItems || zeroQuantityLines > 0)
                    {
                        locationKitInventory.Quantity = 0;
                    }
                    else
                    {
                        // See how many kits can be built at the location
                        locationKitInventory.Quantity = GetKitInventoryQuantity(locationItemInventory, locationKitInventory);
                    }

                    kitInventory.Add(locationKitInventory);
                }

                Log.Information("Kit inventory {@KitInventory}", kitInventory);
                return kitInventory;
            }
            catch (Exception ex)
            {
                Log.Error("Error in GetKitInventory: @Ex", ex);
                return NotFound();
            }
        }


        /// <summary> Returns the DC stocking status if all kit member items are stocking items.</summary>
        public static string GetStockingStatus(IGrouping<string, LocationInventory> locationItemInventory)
        {
            try
            {
                // If Stocking Status is N/A, it is a branch location and stocking status is not applicable.
                if (locationItemInventory.First().StockingStatus == "N/A") return "N/A";
                
                var stockingItemCount = locationItemInventory
                                            .Where(i => i.StockingStatus == "Stocking Item")
                                            .Count();

                var areAllKitMembersStocking = stockingItemCount == locationItemInventory.Count();

                return areAllKitMembersStocking ? "Stocking Item" : "Non Stock Item";
            }
            catch (Exception ex)
            {
                Log.Error("Error in GetStockingStatus: @Ex", ex);
                throw;
            }
        }


        /// <summary>Determines the maximum number of kits that can be built at a location.
        /// Additionally, it sets the DC Stocking Status at the kit level for each DC location.</summary>
        public static int GetKitInventoryQuantity(IGrouping<string, LocationInventory> location, LocationInventory locationInventory)
        {
            try
            {
                var maxNumOfKits = 0;
                var stockingStatus = "";

                foreach (var itemInventoryLine in location)
                {
                    // Determine how many kits can be built for the current line item
                    var currNumOfKits = GetNumberOfKits(itemInventoryLine);

                    var currStockingStatus = itemInventoryLine.StockingStatus;

                    // Set max number of kits and stocking status on the first iteration
                    var isFirstIteration = location.First() == itemInventoryLine;

                    if (isFirstIteration) 
                    {
                        maxNumOfKits = currNumOfKits;
                    }

                    // If current number of kits that can be build is less that the max, use the smaller number as the new max
                    if (currNumOfKits < maxNumOfKits) { maxNumOfKits = currNumOfKits; }

                    
                    if(!isFirstIteration && currStockingStatus == "Non Stock Item" && stockingStatus == "Stocking Item")
                    {
                        stockingStatus = currStockingStatus;
                    }
                }

                return maxNumOfKits;
            }
            catch(Exception ex)
            {
                Log.Error("Error in GetKitInventoryQuantity: @Ex", ex);
                throw;
            }
        }


        public static int GetNumberOfKits(LocationInventory itemInventoryLine)
        {
            try
            {
                var currMPN = itemInventoryLine.MPN;
                var currQuantity = itemInventoryLine.Quantity;

                var requiredQuantity = kitMemberItems.Where(i => i.MPN == currMPN)
                                        .Select(i => i.Quantity)
                                        .SingleOrDefault();

                return currQuantity / requiredQuantity;
            }
            catch(Exception ex)
            {
                Log.Error("Error in GetNumberOfKits: @Ex", ex);
                throw;
            }
        }
    }
}