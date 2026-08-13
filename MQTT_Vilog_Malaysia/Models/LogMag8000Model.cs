using System;

namespace MQTT_Vilog_Malaysia.Models
{
    public class LogMag8000Model
    {
        public DateTime TimeStamp { get; set; }
        public double Flow { get; set; }
        // Raw decoded register value. HandleDataAction.HandleDataMag8000 feeds this into
        // channel _100 (Net Totalizer), not _98 -- _98 (Forward Totalizer) is derived there
        // as ForwardTotal + ReverseTotal.
        public double ForwardTotal { get; set; }
        public double ReverseTotal { get; set; }
        public int Alarm { get; set; }
        public int Battery { get; set; }
    }
}
