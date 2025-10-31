using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Models
{
    [BsonIgnoreExtraElements]
    public class DeviceTokenAppModel
    {
        public string UserName { get; set; }
        public string DeviceToken { get; set; }
        public bool Status { get; set; }
        public bool Sound { get; set; }

    }
}
