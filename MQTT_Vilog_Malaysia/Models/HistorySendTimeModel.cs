using MongoDB.Bson.Serialization.Attributes;
using System;

namespace MQTT_Vilog_Malaysia.Models
{
    [BsonIgnoreExtraElements]
    public class HistorySendTimeModel
    {
        public string SiteId { get; set; }
        public string Location { get; set; }
        public string LoggerId { get; set; }
        public Nullable<DateTime> TimeStamp { get; set; }
    }
}
