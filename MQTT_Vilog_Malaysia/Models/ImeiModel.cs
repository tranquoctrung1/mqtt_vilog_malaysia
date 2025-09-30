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
    public class ImeiModel
    {
        public ObjectId Id { get; set; }

        public string Imei { get; set; }
        public bool Use {  get; set; }
    }
}
