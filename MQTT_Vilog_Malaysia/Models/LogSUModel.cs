using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Models
{
    public class LogSUModel
    {
        public DateTime TimeStamp { get; set; }
        public double NetIndex { get; set; }
        public double ForwardIndex { get; set; }
        public double ReverseIndex { get; set; }
        public double ForwardFlow { get; set; }
        public double ReverseFlow { get; set; }
    }
}
