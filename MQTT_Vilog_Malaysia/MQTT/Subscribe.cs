using MQTT_Vilog_Malaysia.Actions;
using MQTT_Vilog_Malaysia.Models;
using MQTTnet;
using MQTTnet.Extensions.TopicTemplate;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.MQTT
{
    public class Subscribe
    {
        public MqttTopicTemplate sampleTemplate = new("#");

        public async Task Handle_Received_Application_Message(string host, int port, string ipcheck)
        {
            /*
             * This sample subscribes to a topic and processes the received message.
             */

            var mqttFactory = new MqttClientFactory();


            using (var mqttClient = mqttFactory.CreateMqttClient())
            {
                var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(host).Build();


                mqttClient.ApplicationMessageReceivedAsync += e =>
                {

                    Thread t = new Thread(async () =>
                    {
                        try
                        {

                            string topic = e.ApplicationMessage.Topic;

                            if (topic.ToLower().Trim().Contains("vilog_"))
                            {
                                string[] splitTopic = topic.Split(new char[] { '_' }, StringSplitOptions.None);

                                if (splitTopic.Length == 4 && splitTopic[0].ToLower().Trim() == "vilog")
                                {
                                    // handle insert site and channel
                                    string loggerid = splitTopic[2];
                                    string location = splitTopic[1];

                                    string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                                    if (payload != "")
                                    {
                                        var options = new JsonSerializerOptions
                                        {
                                            PropertyNameCaseInsensitive = true // To handle case mismatches
                                        };

                                        var dataObjects = JsonSerializer.Deserialize<PayloadMQTTModel>(payload);

                                        if (dataObjects.IMEI != "")
                                        {
                                            using (CheckImeiAvailableAction checkImeiAvailableAction = new CheckImeiAvailableAction())
                                            {
                                                string url = $"{ipcheck}";

                                                //int check = await checkImeiAvailableAction.CheckImeiAvailable(dataObjects.IMEI, url);

                                                //if (check == 1)

                                                using (SiteAction siteAction2 = new SiteAction())
                                                {
                                                    List<SiteModel> listSite = await siteAction2.GetSite(loggerid);
                                                    if (listSite.Count <= 0)
                                                    {
                                                        if (dataObjects.Payload.Length > 50)
                                                        {
                                                            // SU meter
                                                            // insert site 
                                                            SiteModel site = new SiteModel();
                                                            site.SiteId = loggerid;
                                                            site.IMEI = dataObjects.IMEI;
                                                            site.Location = location;
                                                            site.LoggerId = loggerid;
                                                            site.Longitude = 0;
                                                            site.Latitude = 0;
                                                            site.DisplayGroup = "Vilog";
                                                            site.StartHour = 0;
                                                            site.StartDay = 1;
                                                            site.IsDisplay = true;
                                                            site.InterVal = 5;
                                                            site.TypeMeter = "SU";

                                                            using (SiteAction siteAction = new SiteAction())
                                                            {
                                                                siteAction.InsertSite(site);
                                                            }

                                                            // insert channel config

                                                            List<ChannelConfigModel> listChannels = new List<ChannelConfigModel>();

                                                            ChannelConfigModel flow = new ChannelConfigModel();
                                                            flow.ChannelId = $"{loggerid}_02";
                                                            flow.ChannelName = "2.1 Forward Flow";
                                                            flow.LoggerId = loggerid;
                                                            flow.Unit = "m3/h";
                                                            flow.ForwardFlow = true;
                                                            flow.ReverseFlow = false;
                                                            flow.Pressure1 = false;
                                                            flow.Pressure2 = false;
                                                            flow.TimeStamp = null;
                                                            flow.LastValue = null;
                                                            flow.IndexTimeStamp = null;
                                                            flow.LastIndex = null;
                                                            flow.OtherChannel = false;

                                                            listChannels.Add(flow);

                                                            ChannelConfigModel reverse = new ChannelConfigModel();
                                                            reverse.ChannelId = $"{loggerid}_03";
                                                            reverse.ChannelName = "2.2 Reverse Flow";
                                                            reverse.LoggerId = loggerid;
                                                            reverse.Unit = "m3/h";
                                                            reverse.ForwardFlow = false;
                                                            reverse.ReverseFlow = true;
                                                            reverse.Pressure1 = false;
                                                            reverse.Pressure2 = false;
                                                            reverse.TimeStamp = null;
                                                            reverse.LastValue = null;
                                                            reverse.IndexTimeStamp = null;
                                                            reverse.LastIndex = null;
                                                            reverse.OtherChannel = false;

                                                            listChannels.Add(reverse);

                                                            ChannelConfigModel flowTotal = new ChannelConfigModel();
                                                            flowTotal.ChannelId = $"{loggerid}_98";
                                                            flowTotal.ChannelName = "2.3 Forward Totalizer";
                                                            flowTotal.LoggerId = loggerid;
                                                            flowTotal.Unit = "m3";
                                                            flowTotal.ForwardFlow = false;
                                                            flowTotal.ReverseFlow = false;
                                                            flowTotal.Pressure1 = false;
                                                            flowTotal.Pressure2 = false;
                                                            flowTotal.TimeStamp = null;
                                                            flowTotal.LastValue = null;
                                                            flowTotal.IndexTimeStamp = null;
                                                            flowTotal.LastIndex = null;
                                                            flowTotal.OtherChannel = false;

                                                            listChannels.Add(flowTotal);

                                                            ChannelConfigModel reverseTotal = new ChannelConfigModel();
                                                            reverseTotal.ChannelId = $"{loggerid}_99";
                                                            reverseTotal.ChannelName = "2.4 Reverse Totalizer";
                                                            reverseTotal.LoggerId = loggerid;
                                                            reverseTotal.Unit = "m3";
                                                            reverseTotal.ForwardFlow = false;
                                                            reverseTotal.ReverseFlow = false;
                                                            reverseTotal.Pressure1 = false;
                                                            reverseTotal.Pressure2 = false;
                                                            reverseTotal.TimeStamp = null;
                                                            reverseTotal.LastValue = null;
                                                            reverseTotal.IndexTimeStamp = null;
                                                            reverseTotal.LastIndex = null;
                                                            reverseTotal.OtherChannel = false;

                                                            listChannels.Add(reverseTotal);

                                                            ChannelConfigModel power = new ChannelConfigModel();
                                                            power.ChannelId = $"{loggerid}_100";
                                                            power.ChannelName = "1.1 Power Supply Down";
                                                            power.LoggerId = loggerid;
                                                            power.Unit = "-";
                                                            power.ForwardFlow = false;
                                                            power.ReverseFlow = false;
                                                            power.Pressure1 = false;
                                                            power.Pressure2 = false;
                                                            power.TimeStamp = null;
                                                            power.LastValue = null;
                                                            power.IndexTimeStamp = null;
                                                            power.LastIndex = null;
                                                            power.OtherChannel = true;

                                                            listChannels.Add(power);

                                                            ChannelConfigModel mem = new ChannelConfigModel();
                                                            mem.ChannelId = $"{loggerid}_101";
                                                            mem.ChannelName = "1.2 Memmory Error";
                                                            mem.LoggerId = loggerid;
                                                            mem.Unit = "-";
                                                            mem.ForwardFlow = false;
                                                            mem.ReverseFlow = false;
                                                            mem.Pressure1 = false;
                                                            mem.Pressure2 = false;
                                                            mem.TimeStamp = null;
                                                            mem.LastValue = null;
                                                            mem.IndexTimeStamp = null;
                                                            mem.LastIndex = null;
                                                            mem.OtherChannel = true;

                                                            listChannels.Add(mem);

                                                            ChannelConfigModel low = new ChannelConfigModel();
                                                            low.ChannelId = $"{loggerid}_103";
                                                            low.ChannelName = "1.3 Low Transmitter Voltage";
                                                            low.LoggerId = loggerid;
                                                            low.Unit = "-";
                                                            low.ForwardFlow = false;
                                                            low.ReverseFlow = false;
                                                            low.Pressure1 = false;
                                                            low.Pressure2 = false;
                                                            low.TimeStamp = null;
                                                            low.LastValue = null;
                                                            low.IndexTimeStamp = null;
                                                            low.LastIndex = null;
                                                            low.OtherChannel = true;

                                                            listChannels.Add(low);

                                                            ChannelConfigModel warning = new ChannelConfigModel();
                                                            warning.ChannelId = $"{loggerid}_104";
                                                            warning.ChannelName = "1.4 Reverse Flow Warning";
                                                            warning.LoggerId = loggerid;
                                                            warning.Unit = "-";
                                                            warning.ForwardFlow = false;
                                                            warning.ReverseFlow = false;
                                                            warning.Pressure1 = false;
                                                            warning.Pressure2 = false;
                                                            warning.TimeStamp = null;
                                                            warning.LastValue = null;
                                                            warning.IndexTimeStamp = null;
                                                            warning.LastIndex = null;
                                                            warning.OtherChannel = true;

                                                            listChannels.Add(warning);

                                                            ChannelConfigModel dry = new ChannelConfigModel();
                                                            dry.ChannelId = $"{loggerid}_105";
                                                            dry.ChannelName = "1.5 Drying Warning";
                                                            dry.LoggerId = loggerid;
                                                            dry.Unit = "-";
                                                            dry.ForwardFlow = false;
                                                            dry.ReverseFlow = false;
                                                            dry.Pressure1 = false;
                                                            dry.Pressure2 = false;
                                                            dry.TimeStamp = null;
                                                            dry.LastValue = null;
                                                            dry.IndexTimeStamp = null;
                                                            dry.LastIndex = null;
                                                            dry.OtherChannel = true;

                                                            listChannels.Add(dry);

                                                            ChannelConfigModel meter = new ChannelConfigModel();
                                                            meter.ChannelId = $"{loggerid}_106";
                                                            meter.ChannelName = "1.6 Low Flow Meter Warning";
                                                            meter.LoggerId = loggerid;
                                                            meter.Unit = "-";
                                                            meter.ForwardFlow = false;
                                                            meter.ReverseFlow = false;
                                                            meter.Pressure1 = false;
                                                            meter.Pressure2 = false;
                                                            meter.TimeStamp = null;
                                                            meter.LastValue = null;
                                                            meter.IndexTimeStamp = null;
                                                            meter.LastIndex = null;
                                                            meter.OtherChannel = true;

                                                            listChannels.Add(meter);

                                                            ChannelConfigModel com = new ChannelConfigModel();
                                                            com.ChannelId = $"{loggerid}_107";
                                                            com.ChannelName = "1.7 Comms Error";
                                                            com.LoggerId = loggerid;
                                                            com.Unit = "-";
                                                            com.ForwardFlow = false;
                                                            com.ReverseFlow = false;
                                                            com.Pressure1 = false;
                                                            com.Pressure2 = false;
                                                            com.TimeStamp = null;
                                                            com.LastValue = null;
                                                            com.IndexTimeStamp = null;
                                                            com.LastIndex = null;
                                                            com.OtherChannel = true;

                                                            listChannels.Add(com);

                                                            ChannelConfigModel net = new ChannelConfigModel();
                                                            net.ChannelId = $"{loggerid}_108";
                                                            net.ChannelName = "2.5 Net Totalizer";
                                                            net.LoggerId = loggerid;
                                                            net.Unit = "m3";
                                                            net.ForwardFlow = false;
                                                            net.ReverseFlow = false;
                                                            net.Pressure1 = false;
                                                            net.Pressure2 = false;
                                                            net.TimeStamp = null;
                                                            net.LastValue = null;
                                                            net.IndexTimeStamp = null;
                                                            net.LastIndex = null;
                                                            net.OtherChannel = false;

                                                            listChannels.Add(net);

                                                            ChannelConfigModel signal = new ChannelConfigModel();
                                                            signal.ChannelId = $"{loggerid}_109";
                                                            signal.ChannelName = "2.6 Signal";
                                                            signal.LoggerId = loggerid;
                                                            signal.Unit = "-";
                                                            signal.ForwardFlow = false;
                                                            signal.ReverseFlow = false;
                                                            signal.Pressure1 = false;
                                                            signal.Pressure2 = false;
                                                            signal.TimeStamp = null;
                                                            signal.LastValue = null;
                                                            signal.IndexTimeStamp = null;
                                                            signal.LastIndex = null;
                                                            signal.OtherChannel = false;

                                                            listChannels.Add(signal);

                                                            ChannelConfigModel battery = new ChannelConfigModel();
                                                            battery.ChannelId = $"{loggerid}_110";
                                                            battery.ChannelName = "2.7 Battery";
                                                            battery.LoggerId = loggerid;
                                                            battery.Unit = "-";
                                                            battery.ForwardFlow = false;
                                                            battery.ReverseFlow = false;
                                                            battery.Pressure1 = false;
                                                            battery.Pressure2 = false;
                                                            battery.TimeStamp = null;
                                                            battery.LastValue = null;
                                                            battery.IndexTimeStamp = null;
                                                            battery.LastIndex = null;
                                                            battery.OtherChannel = false;

                                                            listChannels.Add(battery);

                                                            using (ChannelConfigAction channelConfigAction = new ChannelConfigAction())
                                                            {
                                                                foreach (ChannelConfigModel channel in listChannels)
                                                                {
                                                                    channelConfigAction.InsertChannelConfig(channel);
                                                                }
                                                            }

                                                            using (DataLoggerAction dataLoggerAction = new DataLoggerAction())
                                                            {
                                                                foreach (ChannelConfigModel channel in listChannels)
                                                                {
                                                                    dataLoggerAction.CreateDataLoggerCollection(channel.ChannelId, false);
                                                                    dataLoggerAction.CreateDataLoggerCollection(channel.ChannelId, true);
                                                                }
                                                            }

                                                            string realTimeString = dataObjects.Payload.Substring(0, 66);

                                                            List<string> log = new List<string>();

                                                            for (int i = 66; i < dataObjects.Payload.Length; i += 60)
                                                            {
                                                                string l = dataObjects.Payload.Substring(i, 60);
                                                                log.Add(l);
                                                            }

                                                            using (HandleDataAction handleDataAction = new HandleDataAction())
                                                            {
                                                                Console.WriteLine("Execute handle data SU Meter");
                                                                handleDataAction.HandleDataSUMeter(realTimeString, log, site.LoggerId, site.SiteId, site.Location, dataObjects.signal, dataObjects.battery);
                                                                Console.WriteLine("Done executed handle data SU Meter");
                                                            }

                                                            checkImeiAvailableAction.UpdateUsedForImei(dataObjects.IMEI, url);
                                                        }
                                                        else if (dataObjects.Payload.Length <= 50)
                                                        {
                                                            // Kronhe meter

                                                            // insert site 
                                                            SiteModel site = new SiteModel();
                                                            site.SiteId = loggerid;
                                                            site.IMEI = dataObjects.IMEI;
                                                            site.Location = location;
                                                            site.LoggerId = loggerid;
                                                            site.Longitude = 0;
                                                            site.Latitude = 0;
                                                            site.DisplayGroup = "Vilog";
                                                            site.StartHour = 0;
                                                            site.StartDay = 1;
                                                            site.IsDisplay = true;
                                                            site.InterVal = 5;
                                                            site.TypeMeter = "Kronhe";

                                                            using (SiteAction siteAction = new SiteAction())
                                                            {
                                                                siteAction.InsertSite(site);
                                                            }

                                                            // insert channel 

                                                            List<ChannelConfigModel> listChannels = new List<ChannelConfigModel>();

                                                            ChannelConfigModel flow = new ChannelConfigModel();
                                                            flow.ChannelId = $"{loggerid}_02";
                                                            flow.ChannelName = "1. Forward Flow";
                                                            flow.LoggerId = loggerid;
                                                            flow.Unit = "m3/h";
                                                            flow.ForwardFlow = true;
                                                            flow.ReverseFlow = false;
                                                            flow.Pressure1 = false;
                                                            flow.Pressure2 = false;
                                                            flow.TimeStamp = null;
                                                            flow.LastValue = null;
                                                            flow.IndexTimeStamp = null;
                                                            flow.LastIndex = null;

                                                            listChannels.Add(flow);

                                                            ChannelConfigModel reverse = new ChannelConfigModel();
                                                            reverse.ChannelId = $"{loggerid}_03";
                                                            reverse.ChannelName = "2. Reverse Flow";
                                                            reverse.LoggerId = loggerid;
                                                            reverse.Unit = "m3/h";
                                                            reverse.ForwardFlow = false;
                                                            reverse.ReverseFlow = true;
                                                            reverse.Pressure1 = false;
                                                            reverse.Pressure2 = false;
                                                            reverse.TimeStamp = null;
                                                            reverse.LastValue = null;
                                                            reverse.IndexTimeStamp = null;
                                                            reverse.LastIndex = null;

                                                            listChannels.Add(reverse);

                                                            ChannelConfigModel flowTotal = new ChannelConfigModel();
                                                            flowTotal.ChannelId = $"{loggerid}_98";
                                                            flowTotal.ChannelName = "3. Forward Totalizer";
                                                            flowTotal.LoggerId = loggerid;
                                                            flowTotal.Unit = "m3";
                                                            flowTotal.ForwardFlow = false;
                                                            flowTotal.ReverseFlow = false;
                                                            flowTotal.Pressure1 = false;
                                                            flowTotal.Pressure2 = false;
                                                            flowTotal.TimeStamp = null;
                                                            flowTotal.LastValue = null;
                                                            flowTotal.IndexTimeStamp = null;
                                                            flowTotal.LastIndex = null;

                                                            listChannels.Add(flowTotal);

                                                            ChannelConfigModel reverseTotal = new ChannelConfigModel();
                                                            reverseTotal.ChannelId = $"{loggerid}_99";
                                                            reverseTotal.ChannelName = "4. Reverse Totalizer";
                                                            reverseTotal.LoggerId = loggerid;
                                                            reverseTotal.Unit = "m3";
                                                            reverseTotal.ForwardFlow = false;
                                                            reverseTotal.ReverseFlow = false;
                                                            reverseTotal.Pressure1 = false;
                                                            reverseTotal.Pressure2 = false;
                                                            reverseTotal.TimeStamp = null;
                                                            reverseTotal.LastValue = null;
                                                            reverseTotal.IndexTimeStamp = null;
                                                            reverseTotal.LastIndex = null;

                                                            listChannels.Add(reverseTotal);

                                                            ChannelConfigModel power = new ChannelConfigModel();
                                                            power.ChannelId = $"{loggerid}_100";
                                                            power.ChannelName = "5. Net Totalizer";
                                                            power.LoggerId = loggerid;
                                                            power.Unit = "m3";
                                                            power.ForwardFlow = false;
                                                            power.ReverseFlow = false;
                                                            power.Pressure1 = false;
                                                            power.Pressure2 = false;
                                                            power.TimeStamp = null;
                                                            power.LastValue = null;
                                                            power.IndexTimeStamp = null;
                                                            power.LastIndex = null;

                                                            listChannels.Add(power);

                                                            ChannelConfigModel mem = new ChannelConfigModel();
                                                            mem.ChannelId = $"{loggerid}_101";
                                                            mem.ChannelName = "6. Alarm";
                                                            mem.LoggerId = loggerid;
                                                            mem.Unit = "-";
                                                            mem.ForwardFlow = false;
                                                            mem.ReverseFlow = false;
                                                            mem.Pressure1 = false;
                                                            mem.Pressure2 = false;
                                                            mem.TimeStamp = null;
                                                            mem.LastValue = null;
                                                            mem.IndexTimeStamp = null;
                                                            mem.LastIndex = null;
                                                            mem.OtherChannel = true;

                                                            listChannels.Add(mem);

                                                            ChannelConfigModel battery = new ChannelConfigModel();
                                                            battery.ChannelId = $"{loggerid}_05";
                                                            battery.ChannelName = "7. Battery Logger";
                                                            battery.LoggerId = loggerid;
                                                            battery.Unit = "-";
                                                            battery.ForwardFlow = false;
                                                            battery.ReverseFlow = false;
                                                            battery.Pressure1 = false;
                                                            battery.Pressure2 = false;
                                                            battery.TimeStamp = null;
                                                            battery.LastValue = null;
                                                            battery.IndexTimeStamp = null;
                                                            battery.LastIndex = null;
                                                            battery.BatLoggerChannel = true;

                                                            listChannels.Add(battery);


                                                            ChannelConfigModel capacity = new ChannelConfigModel();
                                                            capacity.ChannelId = $"{loggerid}_06";
                                                            capacity.ChannelName = "8. Battery Capacity";
                                                            capacity.LoggerId = loggerid;
                                                            capacity.Unit = "V";
                                                            capacity.ForwardFlow = false;
                                                            capacity.ReverseFlow = false;
                                                            capacity.Pressure1 = false;
                                                            capacity.Pressure2 = false;
                                                            capacity.TimeStamp = null;
                                                            capacity.LastValue = null;
                                                            capacity.IndexTimeStamp = null;
                                                            capacity.LastIndex = null;

                                                            listChannels.Add(capacity);

                                                            ChannelConfigModel signal = new ChannelConfigModel();
                                                            signal.ChannelId = $"{loggerid}_07";
                                                            signal.ChannelName = "9. Signal";
                                                            signal.LoggerId = loggerid;
                                                            signal.Unit = "-";
                                                            signal.ForwardFlow = false;
                                                            signal.ReverseFlow = false;
                                                            signal.Pressure1 = false;
                                                            signal.Pressure2 = false;
                                                            signal.TimeStamp = null;
                                                            signal.LastValue = null;
                                                            signal.IndexTimeStamp = null;
                                                            signal.LastIndex = null;

                                                            listChannels.Add(signal);

                                                            using (ChannelConfigAction channelConfigAction = new ChannelConfigAction())
                                                            {
                                                                foreach (ChannelConfigModel channel in listChannels)
                                                                {
                                                                    channelConfigAction.InsertChannelConfig(channel);
                                                                }
                                                            }

                                                            using (DataLoggerAction dataLoggerAction = new DataLoggerAction())
                                                            {
                                                                foreach (ChannelConfigModel channel in listChannels)
                                                                {
                                                                    dataLoggerAction.CreateDataLoggerCollection(channel.ChannelId, false);
                                                                    dataLoggerAction.CreateDataLoggerCollection(channel.ChannelId, true);
                                                                }
                                                            }

                                                            using (HandleDataAction handleDataAction = new HandleDataAction())
                                                            {
                                                                Console.WriteLine("Execute handle data Kronhe Meter");
                                                                DateTime time = DateTime.Parse(dataObjects.time.ToString());
                                                                time = time.AddHours(8);

                                                                handleDataAction.HandleDataKronheMeter(dataObjects.Payload, dataObjects.AdditionalData, time, site.LoggerId, dataObjects.battery, site.SiteId, site.Location, dataObjects.signal);
                                                                Console.WriteLine("Done executed handle data Kronhe Meter");
                                                            }

                                                            checkImeiAvailableAction.UpdateUsedForImei(dataObjects.IMEI, url);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        SiteModel site = listSite[0];

                                                        if (dataObjects.Payload.Length > 50)
                                                        {
                                                            string realTimeString = dataObjects.Payload.Substring(0, 66);

                                                            List<string> log = new List<string>();

                                                            for (int i = 66; i < dataObjects.Payload.Length; i += 60)
                                                            {
                                                                string l = dataObjects.Payload.Substring(i, 60);
                                                                log.Add(l);
                                                            }

                                                            using (HandleDataAction handleDataAction = new HandleDataAction())
                                                            {
                                                                Console.WriteLine("Execute handle data SU Meter");
                                                                handleDataAction.HandleDataSUMeter(realTimeString, log, site.LoggerId, site.SiteId, site.Location, dataObjects.signal, dataObjects.battery);
                                                                Console.WriteLine("Done executed handle data SU Meter");
                                                            }
                                                        }
                                                        else if (dataObjects.Payload.Length <= 50)
                                                        {
                                                            using (HandleDataAction handleDataAction = new HandleDataAction())
                                                            {
                                                                Console.WriteLine("Execute handle data Kronhe Meter");
                                                                DateTime time = DateTime.Parse(dataObjects.time.ToString());
                                                                time = time.AddHours(8);

                                                                handleDataAction.HandleDataKronheMeter(dataObjects.Payload, dataObjects.AdditionalData, time, site.LoggerId, dataObjects.battery, site.SiteId, site.Location, dataObjects.signal);
                                                                Console.WriteLine("Done executed handle data Kronhe Meter");
                                                            }

                                                            checkImeiAvailableAction.UpdateUsedForImei(dataObjects.IMEI, url);
                                                        }



                                                    }

                                                }
                                            }
                                        }

                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    });
                    t.IsBackground = true;
                    t.Start();

                    return Task.CompletedTask;
                };

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                //var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicTemplate(sampleTemplate.WithParameter("id", "2")).Build();
                var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicTemplate(sampleTemplate).Build();

                await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

                Console.WriteLine("MQTT client subscribed to topic.");

                Console.WriteLine("Press enter to exit.");

                Console.ReadLine();
            }
        }
    }
}
