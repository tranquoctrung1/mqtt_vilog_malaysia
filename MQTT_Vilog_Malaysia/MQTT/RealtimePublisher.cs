using MQTT_Vilog_Malaysia.Actions;
using MQTTnet;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.MQTT
{
    public static class RealtimePublisher
    {
        public static IMqttClient? Client { get; set; }

        public static string BuildTopic(string loggerId, string channelId)
        {
            return $"Vilog_RealTime/{loggerId}/{channelId}";
        }

        public static string BuildPayloadJson(string channelId, double? value, DateTime? timeStamp)
        {
            return JsonConvert.SerializeObject(new
            {
                ChannelId = channelId,
                Value = value,
                TimeStamp = timeStamp
            });
        }

        public static async Task PublishChannelUpdateAsync(string loggerId, string channelId, double? value, DateTime? timeStamp)
        {
            try
            {
                if (Client == null || string.IsNullOrEmpty(loggerId) || string.IsNullOrEmpty(channelId))
                {
                    return;
                }

                string topic = BuildTopic(loggerId, channelId);
                string payload = BuildPayloadJson(channelId, value, timeStamp);

                MqttApplicationMessage message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtMostOnce)
                    .Build();

                await Client.PublishAsync(message);
            }
            catch (Exception ex)
            {
                WriteLogAction writeLogAction = new WriteLogAction();
                await writeLogAction.WriteErrorLog($"RealtimePublisher publish failed: {ex.Message}");
            }
        }
    }
}
