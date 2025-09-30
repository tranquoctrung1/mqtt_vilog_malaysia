using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Models
{
    [BsonIgnoreExtraElements]
    public class HistoryAlarmModel
    {
        public string SiteId { get; set; }
        public string Location { get; set; }
        public string ChannelName { get; set; }
        public string ChannelId { get; set; }
        public string LoggerId { get; set; }
        public string Content { get; set; }
        public Nullable<DateTime> TimeStampHasValue { get; set; }
        public Nullable<DateTime> TimeStampAlarm {  get; set; }
        public Nullable<int> Type { get; set; }
    }
}
