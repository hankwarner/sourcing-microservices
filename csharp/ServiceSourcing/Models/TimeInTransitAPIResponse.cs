using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceSourcing.Models
{
    public class TimeInTransitAPIResponse
    {
        public emsResponse emsResponse { get; set; }
    }

    public class emsResponse
    {
        public int numberOfServices { get; set; }
        public List<service> services { get; set; }
    }

    public class service
    {
        public string serviceLevelDescription { get; set; }
        public int businessTransitDays { get; set; }
        public string deliveryDate { get; set; }
        public string deliveryTime { get; set; }
        public string deliveryDayOfWeek { get; set; }
    }

    //public class ShipmentAddressConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return (objectType == typeof(ShipmentAddress) || objectType == typeof(ShipmentAddress[]));
    //    }
    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        var token = JToken.Load(reader);
    //        if (token.Type == JTokenType.Object)
    //        {
    //            return token.ToObject<ShipmentAddress>();
    //        }
    //        if (token.Type == JTokenType.Array)
    //        {
    //            // If multiple shipment addresses exists, return the shipper address
    //            var shipmentAddressArray = JArray.Parse(token.ToString());
    //            var address = new ShipmentAddress();
    //            foreach (var line in shipmentAddressArray)
    //            {
    //                var currAddress = JsonConvert.DeserializeObject<ShipmentAddress>(line.ToString());
    //                if (currAddress.Type.Description == "Shipper Address")
    //                {
    //                    address = currAddress;
    //                }
    //            }
    //            return address;
    //        }
    //        return null;
    //    }
    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        serializer.Serialize(writer, value);
    //    }
    //}
}
