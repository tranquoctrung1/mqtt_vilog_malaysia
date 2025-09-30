using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Models
{
    [BsonIgnoreExtraElements]
    public class ChannelConfigModel
    {
        public ObjectId Id { get; set; }
        public string ChannelId { get; set; }

        public string LoggerId { get; set; }

        public string ChannelName { get; set; }
        public string Unit { get; set; }
        public Nullable<bool> Pressure1 { get; set; }
        public Nullable<bool> Pressure2 { get; set; }
        public Nullable<bool> ForwardFlow { get; set; }
        public Nullable<bool> ReverseFlow { get; set; }
        public Nullable<DateTime> IndexTimeStamp { get; set; }
        public Nullable<double> LastIndex { get; set; }
        public Nullable<double> LastValue { get; set; }
        public Nullable<DateTime> TimeStamp { get; set; }
        public Nullable<bool> DisplayOnLable { get; set; }
        public Nullable<double> BaseLine { get; set; }
        public Nullable<double> BaseMin { get; set; }
        public Nullable<double> BaseMax { get; set; }
        public Nullable<bool> OtherChannel { get; set; }
        public Nullable<bool> BatMetterChannel { get; set; }
        public Nullable<bool> BatLoggerChannel { get; set; }
    }
}
