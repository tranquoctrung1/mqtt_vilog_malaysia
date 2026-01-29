using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Models
{
    [BsonIgnoreExtraElements]
    public class ConfigVilogModel
    {
        public ObjectId Id { get; set; }
        public string oldSiteId { get; set; }
        public string oldLocation { get; set; }
        public string siteId { get; set; }
        public string location { get; set; }
        public string typeMeter { get; set; }
        public Nullable<bool> isComplete { get; set; }
    }
}
