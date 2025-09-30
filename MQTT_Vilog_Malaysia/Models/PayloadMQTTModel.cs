using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Models
{
    public class PayloadMQTTModel
    {
        public string IMEI { get; set; }
        public string IMSI { get; set; }
        public string Model { get; set; }
        public string Payload { get; set; }
        public int interrupt { get; set; }
        public int interrupt_level { get; set; }
        public double battery { get; set; }
        public int signal { get; set; }
        public DateTime time { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public DateTime gps_time { get; set; }

        // Dùng Dictionary để giữ key "1", "2", ...
        [JsonExtensionData]
        public Dictionary<string, JsonElement> AdditionalData { get; set; }
    }
}
