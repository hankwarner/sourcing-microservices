using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestSharp;
using ServiceSourcing.Services;
using Xunit;

namespace ServiceSourcing.UnitTest
{
    public class TestDistanceDataController
    {
        private static string baseUrl = "http://service-sourcing.supply.com/api/v2/DistanceData/";
        private static string zip = "33615";
        private static int milesAllowedFromZip = 30;


        [Fact]
        public static void Test_GetDistanceDataForMultipleItems()
        {
            var sqlWhereClause = $"(dist.ZipCode = '{zip}' " +
                                  "AND inv.MPID = '1353677' " +
                                  "AND CEILING(DistanceInMeters * 0.0006213712) <= 30 " +
                                  "AND inv.QTY > 0 " +
                                  "AND sfl.PRECISION_DELIVERY = 'T') " +
                                   " OR " +
                                 $"(dist.ZipCode = '{zip}' " +
                                  "AND inv.MPID = '2500' " +
                                  "AND CEILING(DistanceInMeters * 0.0006213712) <= 30 " +
                                  "AND inv.QTY > 0 " +
                                  "AND sfl.PRECISION_DELIVERY = 'T') ";

            var distanceDataService = new DistanceDataServices();

            var response = distanceDataService.RequestMultipleItemDistanceData(sqlWhereClause);

            Assert.Equal(2, response["1353677"].Count());
            Assert.Equal(2, response["2500"].Count());
        }


        [Fact]
        public static void Test_GetAllLocationDistance()
        {
            string url = baseUrl + "GetLocationsWithinMiles/" + zip;

            var client = new RestClient(url);
            var request = CreateNewGetRequest();
            var response = client.Execute(request);

            Assert.NotNull(response);
        }


        [Fact]
        public static void Test_GetZipCodePrecisionDeliveryEligibility()
        {
            string url = baseUrl + "GetZipCodePrecisionDeliveryEligibility/" + zip + "/" + milesAllowedFromZip;

            var client = new RestClient(url);
            var request = CreateNewGetRequest();
            var response = client.Execute(request).Content;

            bool isZipCodeEligible;

            if (bool.TryParse(response, out isZipCodeEligible))
            {
                Assert.True(isZipCodeEligible);
            }
        }


        public static RestRequest CreateNewGetRequest()
        {
            RestRequest request = new RestRequest(Method.GET);
            SetHeaders(request);

            return request;
        }


        public static void SetHeaders(RestRequest request)
        {
            request.AddHeader("Accept", "application/json");
            request.AddHeader("Content-Type", "application/json");
        }
    }
}
