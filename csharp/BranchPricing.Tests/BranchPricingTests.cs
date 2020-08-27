using System;
using Xunit;
using BranchPricing.Models;
using BranchPricing.Controllers;
using System.Collections.Generic;
using System.Linq;

namespace BranchPricing.Tests
{
    public class BranchPricingTests
    {
        [Fact]
        public void WillRemoveBranchesWithoutPricingUpdates()
        {
            var branchPriceOne = new BranchPrice();
            branchPriceOne.VendorId = "123456789";
            branchPriceOne.Price = 9.99;

            var branchPriceTwo = new BranchPrice();
            branchPriceTwo.VendorId = "987654321";
            branchPriceTwo.Price = 5.99;

            var branchPriceThree = new BranchPrice();
            branchPriceThree.VendorId = "7777777";
            branchPriceThree.Price = 6.00;

            var branchPricesInFeed = new List<BranchPrice>() { branchPriceOne, branchPriceTwo, branchPriceThree };

            var netsuiteRequest = new NetSuiteRequest();
            netsuiteRequest.MasterProductNumber = 88888888;
            netsuiteRequest.BranchPrices = branchPricesInFeed;

            var itemVendorPriceOne = new VendorPrice();
            itemVendorPriceOne.MasterProductNumber = 88888888;
            itemVendorPriceOne.VendorId = "123456789";
            itemVendorPriceOne.Price = 9.99;

            var itemVendorPriceTwo = new VendorPrice();
            itemVendorPriceTwo.MasterProductNumber = 88888888;
            itemVendorPriceTwo.VendorId = "987654321";
            itemVendorPriceTwo.Price = 7.99;

            var itemVendorPriceThree = new VendorPrice();
            itemVendorPriceThree.MasterProductNumber = 33333333;
            itemVendorPriceThree.VendorId = "987654321";
            itemVendorPriceThree.Price = 2.99;

            NetSuiteController.branchPricingInNetSuite = new List<VendorPrice>() { itemVendorPriceOne, itemVendorPriceTwo, itemVendorPriceThree };

            var branchesWithoutUpdates = NetSuiteController.CheckForPricingUpdates(netsuiteRequest);
            NetSuiteController.RemoveBranchesWithoutPricingUpdates(netsuiteRequest, branchesWithoutUpdates);

            Assert.Equal(2, netsuiteRequest.BranchPrices.Count());
            Assert.Equal(branchPriceTwo.VendorId, netsuiteRequest.BranchPrices[0].VendorId);
            Assert.Equal(branchPriceTwo.Price, netsuiteRequest.BranchPrices[0].Price);
        }
    }
}
