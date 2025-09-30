using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Models
{
    public class RealTimeModel
    {
        public DateTime TimeStamp { get; set; }
        public double NetIndex { get; set; }
        public double ForwardIndex { get; set; }
        public double ReverseIndex { get; set; }
        public double ForwardFlow { get; set; }
        public double ReverseFlow { get; set; }
        public int Unit { get; set; }
        public int CommunicationError { get; set; }
        public int LowFlowMeterVoltage { get; set; }
        public int DryingWarning { get; set; }
        public int ReverseFlowWarning { get; set; }
        public int LowTransmitterVoltage { get; set; }
        public int MemoryError { get; set; }
        public int ModbusPowerSupplyDown { get; set; }
        public double Battery { get; set; }
        public double Signal { get; set; }
    }
}
