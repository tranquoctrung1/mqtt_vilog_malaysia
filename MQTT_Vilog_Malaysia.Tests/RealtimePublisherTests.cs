using MQTT_Vilog_Malaysia.MQTT;
using Newtonsoft.Json.Linq;
using System;
using Xunit;

namespace MQTT_Vilog_Malaysia.Tests
{
    public class RealtimePublisherTests
    {
        [Fact]
        public void BuildTopic_CombinesLoggerAndChannelId()
        {
            string topic = RealtimePublisher.BuildTopic("LOGGER123", "LOGGER123_02");

            Assert.Equal("Vilog_RealTime/LOGGER123/LOGGER123_02", topic);
        }

        [Fact]
        public void BuildPayloadJson_ContainsChannelValueAndTimeStamp()
        {
            DateTime ts = new DateTime(2026, 7, 1, 10, 30, 0, DateTimeKind.Utc);

            string json = RealtimePublisher.BuildPayloadJson("LOGGER123_02", 12.34, ts);
            JObject obj = JObject.Parse(json);

            Assert.Equal("LOGGER123_02", obj["ChannelId"]!.Value<string>());
            Assert.Equal(12.34, obj["Value"]!.Value<double>());
            Assert.Equal(ts, obj["TimeStamp"]!.Value<DateTime>());
        }

        [Fact]
        public void BuildPayloadJson_AllowsNullValueAndTimeStamp()
        {
            string json = RealtimePublisher.BuildPayloadJson("LOGGER123_02", null, null);
            JObject obj = JObject.Parse(json);

            Assert.True(obj["Value"]!.Type == JTokenType.Null);
            Assert.True(obj["TimeStamp"]!.Type == JTokenType.Null);
        }
    }
}
