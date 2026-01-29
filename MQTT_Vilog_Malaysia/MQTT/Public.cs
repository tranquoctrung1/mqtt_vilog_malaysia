using Microsoft.Extensions.Configuration;
using MQTT_Vilog_Malaysia.ConfigClass;
using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MQTT_Vilog_Malaysia.MQTT
{
    public class Public
    {
        public async Task PublishAsync(string topic)
        {
            IConfiguration config = new ConfigurationBuilder()
             .AddJsonFile("settings.json")
              .AddEnvironmentVariables()
              .Build();

            Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

            var factory = new MqttClientFactory();
            using var client = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(settings.IpMQTT, int.Parse(settings.Port))
                .Build();

            await client.ConnectAsync(options);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Array.Empty<byte>())
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(true)
                .Build();

            await client.PublishAsync(message);

            await client.DisconnectAsync();
        }
    }
}
