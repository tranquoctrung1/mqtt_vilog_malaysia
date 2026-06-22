using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.ConfigClass
{
    public class Settings
    {
        public string Host { get; set; }
        public string DBName { get; set; }
        public string IpMQTT { get; set; }
        public string Port { get; set; }
        public string IpCheck { get; set; }

        // Number of concurrent message-processing workers. Defaults applied in code if 0.
        public int WorkerCount { get; set; }
        // Max messages buffered before MQTT receive backpressures (Wait). Defaults applied in code if 0.
        public int QueueCapacity { get; set; }

        // Dump raw payload of every message to ./Log/{topic} (debug/audit). Off in production:
        // it writes one file per topic and grows unbounded (disk bloat + per-message I/O).
        public bool LogRawPayload { get; set; }
    }
}
