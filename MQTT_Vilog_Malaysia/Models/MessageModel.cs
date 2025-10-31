using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Models
{
    public class MessageModel
    {
        public string token { get; set; }
        public NotificationModel notification { get; set; }
    }
}
