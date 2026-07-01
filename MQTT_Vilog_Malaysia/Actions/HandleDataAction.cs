using MQTT_Vilog_Malaysia.Models;
using MQTT_Vilog_Malaysia.MQTT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class HandleDataAction : IDisposable
    {
        private bool disposed = false;

        public async Task HandleDataKronheMeter(string stringRealTime, Dictionary<string, JsonElement> Log, DateTime time, string imei, double battery, string siteid, string location, int signal)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            AnalyzeDataAction analyzeDataAction = new AnalyzeDataAction();
            ChannelConfigAction channelConfigAction = new ChannelConfigAction();
            DataLoggerAction dataLoggerAction = new DataLoggerAction();

            try
            {
                LogKronheModel realtimeData = await analyzeDataAction.AnalyzeDataRealTimeHronheMeter(stringRealTime, time, siteid, location, imei, signal, battery);
                List<LogKronheModel> logs = await analyzeDataAction.AnalyzeLogDataHronheMeter(Log);

                List<ChannelConfigModel> channelConfig = await channelConfigAction.GetChannelByLoggerId(imei);

                ChannelConfigModel channelForward = channelConfig.Where(c => c.ForwardFlow == true).FirstOrDefault();
                ChannelConfigModel channelReverse = channelConfig.Where(c => c.ReverseFlow == true).FirstOrDefault();

                // Collect all channel-config value/index updates and flush in ONE BulkWrite.
                var chUpdates = new List<(string channelId, DataLoggerModel value, bool isIndex)>();

                if (channelForward != null && !string.IsNullOrEmpty(channelForward.ChannelId))
                {
                    DataLoggerModel dataUpdate = new DataLoggerModel();
                    dataUpdate.Value = Math.Round(realtimeData.ForwardFlow, 2);
                    dataUpdate.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                    DataLoggerModel dataUpdateIndex = new DataLoggerModel();
                    dataUpdateIndex.Value = Math.Round(realtimeData.ForwardIndex, 2);
                    dataUpdateIndex.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                    chUpdates.Add((channelForward.ChannelId, dataUpdate, false));
                    chUpdates.Add((channelForward.ChannelId, dataUpdateIndex, true));

                    if (logs.Count > 0)
                    {
                        List<DataLoggerModel> dataInsert = new List<DataLoggerModel>();
                        List<DataLoggerModel> dataInsertIndex = new List<DataLoggerModel>();

                        foreach (var item in logs)
                        {
                            DataLoggerModel el = new DataLoggerModel();
                            el.Value = Math.Round(item.ForwardFlow, 2);
                            el.TimeStamp = item.TimeStamp.AddHours(8);

                            DataLoggerModel el2 = new DataLoggerModel();
                            el2.Value = Math.Round(item.ForwardIndex, 2);
                            el2.TimeStamp = item.TimeStamp.AddHours(8);

                            dataInsert.Add(el);
                            dataInsertIndex.Add(el2);
                        }

                        await dataLoggerAction.InsertDataLogger(dataInsert, channelForward.ChannelId);
                        await dataLoggerAction.InsertIndexLogger(dataInsertIndex, channelForward.ChannelId);

                    }
                }
                if (channelReverse != null && !string.IsNullOrEmpty(channelReverse.ChannelId))
                {

                    DataLoggerModel dataUpdate = new DataLoggerModel();
                    dataUpdate.Value = Math.Round(realtimeData.ReverseFlow, 2);
                    dataUpdate.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                    DataLoggerModel dataUpdateIndex = new DataLoggerModel();
                    dataUpdateIndex.Value = Math.Round(realtimeData.ReverseIndex, 2);
                    dataUpdateIndex.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                    chUpdates.Add((channelReverse.ChannelId, dataUpdate, false));
                    chUpdates.Add((channelReverse.ChannelId, dataUpdateIndex, true));

                    if (logs.Count > 0)
                    {
                        List<DataLoggerModel> dataInsert = new List<DataLoggerModel>();
                        List<DataLoggerModel> dataInsertIndex = new List<DataLoggerModel>();

                        foreach (var item in logs)
                        {
                            DataLoggerModel el = new DataLoggerModel();
                            el.Value = Math.Round(item.ReverseFlow, 2);
                            el.TimeStamp = item.TimeStamp.AddHours(8);

                            DataLoggerModel el2 = new DataLoggerModel();
                            el2.Value = Math.Round(item.ReverseIndex, 2);
                            el2.TimeStamp = item.TimeStamp.AddHours(8);

                            dataInsert.Add(el);
                            dataInsertIndex.Add(el2);
                        }

                        await dataLoggerAction.InsertDataLogger(dataInsert, channelReverse.ChannelId);
                        await dataLoggerAction.InsertIndexLogger(dataInsertIndex, channelReverse.ChannelId);

                    }
                }


                // forward total channel
                DataLoggerModel dataUpdateForwardTotal = new DataLoggerModel();
                dataUpdateForwardTotal.Value = Math.Round(realtimeData.ForwardIndex, 2);
                dataUpdateForwardTotal.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                // reverse total channel
                DataLoggerModel dataUpdateReverseTotal = new DataLoggerModel();
                dataUpdateReverseTotal.Value = Math.Round(realtimeData.ReverseIndex, 2);
                dataUpdateReverseTotal.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                // net total channel
                DataLoggerModel dataUpdateNetTotal = new DataLoggerModel();
                dataUpdateNetTotal.Value = Math.Round(realtimeData.NetIndex, 2);
                dataUpdateNetTotal.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                // alarm channel
                DataLoggerModel dataUpdateAlarm = new DataLoggerModel();
                dataUpdateAlarm.Value = realtimeData.Alarm;
                dataUpdateAlarm.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                // battery channel
                DataLoggerModel dataBattery = new DataLoggerModel();
                dataBattery.Value = battery;
                dataBattery.TimeStamp = DateTime.Now.AddHours(8);

                // battery capacity channel
                DataLoggerModel dataBatteryCapacity = new DataLoggerModel();
                dataBatteryCapacity.Value = Math.Round(realtimeData.BatteryRemain, 2);
                dataBatteryCapacity.TimeStamp = DateTime.Now.AddHours(8);

                // signal channel
                DataLoggerModel dataSignal = new DataLoggerModel();
                dataSignal.Value = signal;
                dataSignal.TimeStamp = DateTime.Now.AddHours(8);

                chUpdates.Add(($"{imei}_98", dataUpdateForwardTotal, false));
                chUpdates.Add(($"{imei}_99", dataUpdateReverseTotal, false));
                chUpdates.Add(($"{imei}_100", dataUpdateNetTotal, false));
                chUpdates.Add(($"{imei}_101", dataUpdateAlarm, false));
                chUpdates.Add(($"{imei}_05", dataBattery, false));
                chUpdates.Add(($"{imei}_06", dataBatteryCapacity, false));
                chUpdates.Add(($"{imei}_07", dataSignal, false));

                await channelConfigAction.BulkUpdateValues(chUpdates);

                foreach (var chUpdate in chUpdates)
                {
                    if (!chUpdate.isIndex)
                    {
                        await RealtimePublisher.PublishChannelUpdateAsync(imei, chUpdate.channelId, chUpdate.value.Value, chUpdate.value.TimeStamp);
                    }
                }

                //insert data logger for battery channel
                List<DataLoggerModel> listInsertBateryChannel = new List<DataLoggerModel>();
                listInsertBateryChannel.Add(dataBattery);
                await dataLoggerAction.InsertDataLogger(listInsertBateryChannel, $"{imei}_05");

                //insert data logger for battery capacity channel
                List<DataLoggerModel> listInsertBateryCapacityChannel = new List<DataLoggerModel>();
                listInsertBateryCapacityChannel.Add(dataBatteryCapacity);
                await dataLoggerAction.InsertDataLogger(listInsertBateryCapacityChannel, $"{imei}_06");

                //insert data logger for signal channel
                List<DataLoggerModel> listInsertSignalChannel = new List<DataLoggerModel>();
                listInsertSignalChannel.Add(dataSignal);
                await dataLoggerAction.InsertDataLogger(listInsertSignalChannel, $"{imei}_07");

                List<DataLoggerModel> dataInsertForwardTotal = new List<DataLoggerModel>();
                List<DataLoggerModel> dataInsertReverseTotal = new List<DataLoggerModel>();
                List<DataLoggerModel> dataInsertNetTotalTotal = new List<DataLoggerModel>();
                List<DataLoggerModel> dataInsertAlarmTotal = new List<DataLoggerModel>();

                if (logs.Count > 0)
                {
                    foreach (var item in logs)
                    {
                        DataLoggerModel el = new DataLoggerModel();
                        el.Value = Math.Round(item.ForwardIndex, 2);
                        el.TimeStamp = item.TimeStamp.AddHours(8);

                        DataLoggerModel el2 = new DataLoggerModel();
                        el2.Value = Math.Round(item.ReverseIndex, 2);
                        el2.TimeStamp = item.TimeStamp.AddHours(8);

                        DataLoggerModel el3 = new DataLoggerModel();
                        el3.Value = Math.Round(item.NetIndex, 2);
                        el3.TimeStamp = item.TimeStamp.AddHours(8);

                        DataLoggerModel el4 = new DataLoggerModel();
                        el4.Value = item.Alarm;
                        el4.TimeStamp = item.TimeStamp.AddHours(8);

                        dataInsertForwardTotal.Add(el);
                        dataInsertReverseTotal.Add(el2);
                        dataInsertNetTotalTotal.Add(el3);
                        dataInsertAlarmTotal.Add(el4);
                    }

                    await dataLoggerAction.InsertDataLogger(dataInsertForwardTotal, $"{imei}_98");
                    await dataLoggerAction.InsertDataLogger(dataInsertReverseTotal, $"{imei}_99");
                    await dataLoggerAction.InsertDataLogger(dataInsertNetTotalTotal, $"{imei}_100");
                    await dataLoggerAction.InsertDataLogger(dataInsertAlarmTotal, $"{imei}_101");

                }

            }
            catch (Exception ex)
            {

                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }

        public async Task HandleDataKronheMeterOverTime(string stringRealTime, Dictionary<string, JsonElement> Log, DateTime time, string imei, double battery, string siteid, string location, int signal)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            AnalyzeDataAction analyzeDataAction = new AnalyzeDataAction();
            ChannelConfigAction channelConfigAction = new ChannelConfigAction();
            DataLoggerAction dataLoggerAction = new DataLoggerAction();

            try
            {
                LogKronheModel realtimeData = await analyzeDataAction.AnalyzeDataRealTimeHronheMeter(stringRealTime, time, siteid, location, imei, signal, battery);
                List<LogKronheModel> logs = await analyzeDataAction.AnalyzeLogDataHronheMeter(Log);

                List<ChannelConfigModel> channelConfig = await channelConfigAction.GetChannelByLoggerId(imei);

                ChannelConfigModel channelForward = channelConfig.Where(c => c.ForwardFlow == true).FirstOrDefault();
                ChannelConfigModel channelReverse = channelConfig.Where(c => c.ReverseFlow == true).FirstOrDefault();

                DateTime now = DateTime.Now.AddHours(8);
                DateTime realtime = realtimeData.TimeStamp.AddHours(8);

                // Collect all channel-config value/index updates and flush in ONE BulkWrite.
                var chUpdates = new List<(string channelId, DataLoggerModel value, bool isIndex)>();

                if (channelForward != null && !string.IsNullOrEmpty(channelForward.ChannelId))
                {
                    DataLoggerModel dataUpdate = new DataLoggerModel();
                    dataUpdate.Value = Math.Round(realtimeData.ForwardFlow, 2);
                    dataUpdate.TimeStamp = DateTime.Now.AddHours(8);

                    DataLoggerModel dataUpdateIndex = new DataLoggerModel();
                    dataUpdateIndex.Value = Math.Round(realtimeData.ForwardIndex, 2);
                    dataUpdateIndex.TimeStamp = DateTime.Now.AddHours(8);

                    chUpdates.Add((channelForward.ChannelId, dataUpdate, false));
                    chUpdates.Add((channelForward.ChannelId, dataUpdateIndex, true));

                    if (logs.Count > 0)
                    {
                        List<DataLoggerModel> dataInsert = new List<DataLoggerModel>();
                        List<DataLoggerModel> dataInsertIndex = new List<DataLoggerModel>();

                        double totalSecondDiff = 0;

                        if(logs.Count >= 2)
                        {
                            DateTime timeLog1 = logs[0].TimeStamp;
                            DateTime timeLog2 = logs[1].TimeStamp;

                            totalSecondDiff = Math.Abs((timeLog1 - timeLog2).TotalSeconds);
                        }

                        DateTime? currentTime = await dataLoggerAction.GetCurrentTimeStampDataLogger(channelForward?.ChannelId ?? "");

                        foreach (var item in logs)
                        {

                            DateTime timeLog = item.TimeStamp.AddHours(8);

                            double diff = Math.Abs((realtime - timeLog).TotalSeconds);

                            DateTime realTimeLog = now.AddSeconds(-diff);
                            realTimeLog = new DateTime(realTimeLog.Year, realTimeLog.Month, realTimeLog.Day, realTimeLog.Hour, item.TimeStamp.Minute, 0);
                            realTimeLog = realTimeLog.AddHours(8);

                            DataLoggerModel el = new DataLoggerModel();
                            el.Value = Math.Round(item.ForwardFlow, 2);
                            el.TimeStamp = realTimeLog;

                            DataLoggerModel el2 = new DataLoggerModel();
                            el2.Value = Math.Round(item.ForwardIndex, 2);
                            el2.TimeStamp = realTimeLog;

                            if(currentTime != null)
                            {
                                if (DateTime.Compare(realTimeLog, currentTime.Value.AddHours(7)) > 0)
                                {
                                    dataInsert.Add(el);
                                    dataInsertIndex.Add(el2);
                                }
                            }
                            else
                            {
                                dataInsert.Add(el);
                                dataInsertIndex.Add(el2);
                            }

                        }

                        await dataLoggerAction.InsertDataLogger(dataInsert, channelForward.ChannelId);
                        await dataLoggerAction.InsertIndexLogger(dataInsertIndex, channelForward.ChannelId);

                    }
                }
                if (channelReverse != null && !string.IsNullOrEmpty(channelReverse.ChannelId))
                {

                    DataLoggerModel dataUpdate = new DataLoggerModel();
                    dataUpdate.Value = Math.Round(realtimeData.ReverseFlow, 2);
                    dataUpdate.TimeStamp = DateTime.Now.AddHours(8);

                    DataLoggerModel dataUpdateIndex = new DataLoggerModel();
                    dataUpdateIndex.Value = Math.Round(realtimeData.ReverseIndex, 2);
                    dataUpdateIndex.TimeStamp = DateTime.Now.AddHours(8);

                    chUpdates.Add((channelReverse.ChannelId, dataUpdate, false));
                    chUpdates.Add((channelReverse.ChannelId, dataUpdateIndex, true));

                    if (logs.Count > 0)
                    {
                        List<DataLoggerModel> dataInsert = new List<DataLoggerModel>();
                        List<DataLoggerModel> dataInsertIndex = new List<DataLoggerModel>();

                        double totalSecondDiff = 0;

                        if (logs.Count >= 2)
                        {
                            DateTime timeLog1 = logs[0].TimeStamp;
                            DateTime timeLog2 = logs[1].TimeStamp;

                            totalSecondDiff = Math.Abs((timeLog1 - timeLog2).TotalSeconds);
                        }

                        DateTime? currentTime = await dataLoggerAction.GetCurrentTimeStampDataLogger(channelForward?.ChannelId ?? "");


                        foreach (var item in logs)
                        {

                            DateTime timeLog = item.TimeStamp.AddHours(8);

                            double diff = Math.Abs((realtime - timeLog).TotalSeconds);

                            DateTime realTimeLog = now.AddSeconds(-diff);
                            realTimeLog = new DateTime(realTimeLog.Year, realTimeLog.Month, realTimeLog.Day, realTimeLog.Hour, item.TimeStamp.Minute, 0);
                            realTimeLog = realTimeLog.AddHours(8);


                            DataLoggerModel el = new DataLoggerModel();
                            el.Value = Math.Round(item.ReverseFlow, 2);
                            el.TimeStamp = realTimeLog;

                            DataLoggerModel el2 = new DataLoggerModel();
                            el2.Value = Math.Round(item.ReverseIndex, 2);
                            el2.TimeStamp = realTimeLog;
                            if(currentTime != null)
                            {
                                if (DateTime.Compare(realTimeLog, currentTime.Value.AddHours(7)) > 0)
                                {
                                    dataInsert.Add(el);
                                    dataInsertIndex.Add(el2);
                                }
                            }                    
                            else
                            {
                                dataInsert.Add(el);
                                dataInsertIndex.Add(el2);
                            }
                        }

                        await dataLoggerAction.InsertDataLogger(dataInsert, channelReverse.ChannelId);
                        await dataLoggerAction.InsertIndexLogger(dataInsertIndex, channelReverse.ChannelId);

                    }
                }


                // forward total channel
                DataLoggerModel dataUpdateForwardTotal = new DataLoggerModel();
                dataUpdateForwardTotal.Value = Math.Round(realtimeData.ForwardIndex, 2);
                dataUpdateForwardTotal.TimeStamp = DateTime.Now.AddHours(8);

                // reverse total channel
                DataLoggerModel dataUpdateReverseTotal = new DataLoggerModel();
                dataUpdateReverseTotal.Value = Math.Round(realtimeData.ReverseIndex, 2);
                dataUpdateReverseTotal.TimeStamp = DateTime.Now.AddHours(8);

                // net total channel
                DataLoggerModel dataUpdateNetTotal = new DataLoggerModel();
                dataUpdateNetTotal.Value = Math.Round(realtimeData.NetIndex, 2);
                dataUpdateNetTotal.TimeStamp = DateTime.Now.AddHours(8);

                // alarm channel
                DataLoggerModel dataUpdateAlarm = new DataLoggerModel();
                dataUpdateAlarm.Value = realtimeData.Alarm;
                dataUpdateAlarm.TimeStamp = DateTime.Now.AddHours(8);

                // battery channel
                DataLoggerModel dataBattery = new DataLoggerModel();
                dataBattery.Value = battery;
                dataBattery.TimeStamp = DateTime.Now.AddHours(8);

                // battery capacity channel
                DataLoggerModel dataBatteryCapacity = new DataLoggerModel();
                dataBatteryCapacity.Value = Math.Round(realtimeData.BatteryRemain, 2);
                dataBatteryCapacity.TimeStamp = DateTime.Now.AddHours(8);

                // signal channel
                DataLoggerModel dataSignal = new DataLoggerModel();
                dataSignal.Value = signal;
                dataSignal.TimeStamp = DateTime.Now.AddHours(8);

                chUpdates.Add(($"{imei}_98", dataUpdateForwardTotal, false));
                chUpdates.Add(($"{imei}_99", dataUpdateReverseTotal, false));
                chUpdates.Add(($"{imei}_100", dataUpdateNetTotal, false));
                chUpdates.Add(($"{imei}_101", dataUpdateAlarm, false));
                chUpdates.Add(($"{imei}_05", dataBattery, false));
                chUpdates.Add(($"{imei}_06", dataBatteryCapacity, false));
                chUpdates.Add(($"{imei}_07", dataSignal, false));

                await channelConfigAction.BulkUpdateValues(chUpdates);

                foreach (var chUpdate in chUpdates)
                {
                    if (!chUpdate.isIndex)
                    {
                        await RealtimePublisher.PublishChannelUpdateAsync(imei, chUpdate.channelId, chUpdate.value.Value, chUpdate.value.TimeStamp);
                    }
                }

                //insert data logger for battery channel
                List<DataLoggerModel> listInsertBateryChannel = new List<DataLoggerModel>();
                listInsertBateryChannel.Add(dataBattery);
                await dataLoggerAction.InsertDataLogger(listInsertBateryChannel, $"{imei}_05");

                //insert data logger for battery capacity channel
                List<DataLoggerModel> listInsertBateryCapacityChannel = new List<DataLoggerModel>();
                listInsertBateryCapacityChannel.Add(dataBatteryCapacity);
                await dataLoggerAction.InsertDataLogger(listInsertBateryCapacityChannel, $"{imei}_06");

                //insert data logger for signal channel
                List<DataLoggerModel> listInsertSignalChannel = new List<DataLoggerModel>();
                listInsertSignalChannel.Add(dataSignal);
                await dataLoggerAction.InsertDataLogger(listInsertSignalChannel, $"{imei}_07");

                List<DataLoggerModel> dataInsertForwardTotal = new List<DataLoggerModel>();
                List<DataLoggerModel> dataInsertReverseTotal = new List<DataLoggerModel>();
                List<DataLoggerModel> dataInsertNetTotalTotal = new List<DataLoggerModel>();
                List<DataLoggerModel> dataInsertAlarmTotal = new List<DataLoggerModel>();

                if (logs.Count > 0)
                {

                    double totalSecondDiff = 0;

                    if (logs.Count >= 2)
                    {
                        DateTime timeLog1 = logs[0].TimeStamp;
                        DateTime timeLog2 = logs[1].TimeStamp;

                        totalSecondDiff = Math.Abs((timeLog1 - timeLog2).TotalSeconds);
                    }

                    DateTime? currentTime = await dataLoggerAction.GetCurrentTimeStampDataLogger(channelForward.ChannelId);

                    foreach (var item in logs)
                    {
                        DateTime timeLog = item.TimeStamp.AddHours(8);

                        double diff = Math.Abs((realtime - timeLog).TotalSeconds);

                        DateTime realTimeLog = now.AddSeconds(-diff);
                        realTimeLog = new DateTime(realTimeLog.Year, realTimeLog.Month, realTimeLog.Day, realTimeLog.Hour, item.TimeStamp.Minute, 0);
                        realTimeLog = realTimeLog.AddHours(8);


                        DataLoggerModel el = new DataLoggerModel();
                        el.Value = Math.Round(item.ForwardIndex, 2);
                        el.TimeStamp = realTimeLog;

                        DataLoggerModel el2 = new DataLoggerModel();
                        el2.Value = Math.Round(item.ReverseIndex, 2);
                        el2.TimeStamp = realTimeLog;

                        DataLoggerModel el3 = new DataLoggerModel();
                        el3.Value = Math.Round(item.NetIndex, 2);
                        el3.TimeStamp = realTimeLog;

                        DataLoggerModel el4 = new DataLoggerModel();
                        el4.Value = item.Alarm;
                        el4.TimeStamp = realTimeLog;

                        if(currentTime != null)
                        {
                            if (DateTime.Compare(realTimeLog, currentTime.Value.AddHours(7)) > 0)
                            {
                                dataInsertForwardTotal.Add(el);
                                dataInsertReverseTotal.Add(el2);
                                dataInsertNetTotalTotal.Add(el3);
                                dataInsertAlarmTotal.Add(el4);
                            }
                        }
                        else
                        {
                            dataInsertForwardTotal.Add(el);
                            dataInsertReverseTotal.Add(el2);
                            dataInsertNetTotalTotal.Add(el3);
                            dataInsertAlarmTotal.Add(el4);
                        }
                       
                       
                    }

                    await dataLoggerAction.InsertDataLogger(dataInsertForwardTotal, $"{imei}_98");
                    await dataLoggerAction.InsertDataLogger(dataInsertReverseTotal, $"{imei}_99");
                    await dataLoggerAction.InsertDataLogger(dataInsertNetTotalTotal, $"{imei}_100");
                    await dataLoggerAction.InsertDataLogger(dataInsertAlarmTotal, $"{imei}_101");

                }

            }
            catch (Exception ex)
            {

                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }
        public async Task HandleDataSUMeter(string stringRealTime, List<string> logData, string imei, string siteid, string location, int signal, double battery)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            AnalyzeDataAction analyzeDataAction = new AnalyzeDataAction();
            ChannelConfigAction channelConfigAction = new ChannelConfigAction();
            DataLoggerAction dataLoggerAction = new DataLoggerAction();

            try
            {

                RealTimeModel real = await analyzeDataAction.AnalyzeDataRealTimeSUMeter(stringRealTime, siteid, location, imei, signal, battery);
                List<ChannelConfigModel> channel = await channelConfigAction.GetChannelByLoggerId(imei);

                string channelForward = channel.Where(c => c.ForwardFlow == true).FirstOrDefault()?.ChannelId ?? "";
                string channelReverse = channel.Where(c => c.ReverseFlow == true).FirstOrDefault()?.ChannelId ?? "";

                // Collect all channel-config value/index updates and flush in ONE BulkWrite.
                var chUpdates = new List<(string channelId, DataLoggerModel value, bool isIndex)>();

                if (real != null)
                {
                    if (channelForward != "")
                    {
                        DataLoggerModel data = new DataLoggerModel();
                        data.TimeStamp = real.TimeStamp.AddHours(8);
                        data.Value = real.ForwardFlow;

                        DataLoggerModel index = new DataLoggerModel();
                        index.TimeStamp = real.TimeStamp.AddHours(8);
                        index.Value = real.ForwardIndex;

                        chUpdates.Add((channelForward, data, false));
                        chUpdates.Add((channelForward, index, true));
                        chUpdates.Add(($"{imei}_98", index, false));
                    }
                    if (channelReverse != "")
                    {
                        DataLoggerModel data = new DataLoggerModel();
                        data.TimeStamp = real.TimeStamp.AddHours(8);
                        data.Value = real.ReverseFlow;

                        DataLoggerModel index = new DataLoggerModel();
                        index.TimeStamp = real.TimeStamp.AddHours(8);
                        index.Value = real.ReverseIndex;

                        chUpdates.Add((channelReverse, data, false));
                        chUpdates.Add((channelReverse, index, true));
                        chUpdates.Add(($"{imei}_99", index, false));
                    }


                    DataLoggerModel datamodbus = new DataLoggerModel();
                    datamodbus.TimeStamp = real.TimeStamp.AddHours(8);
                    datamodbus.Value = real.ModbusPowerSupplyDown;
                    chUpdates.Add(($"{imei}_100", datamodbus, false));
                    List<DataLoggerModel> t = new List<DataLoggerModel>();
                    t.Add(datamodbus);
                    await dataLoggerAction.InsertDataLogger(t, $"{imei}_100");

                    DataLoggerModel dataMemory = new DataLoggerModel();
                    dataMemory.TimeStamp = real.TimeStamp.AddHours(8);
                    dataMemory.Value = real.MemoryError;
                    chUpdates.Add(($"{imei}_101", dataMemory, false));
                    List<DataLoggerModel> t2 = new List<DataLoggerModel>();
                    t2.Add(dataMemory);
                    await dataLoggerAction.InsertDataLogger(t2, $"{imei}_101");

                    DataLoggerModel dataLowTransmitter = new DataLoggerModel();
                    dataLowTransmitter.TimeStamp = real.TimeStamp.AddHours(8);
                    dataLowTransmitter.Value = real.LowTransmitterVoltage;
                    chUpdates.Add(($"{imei}_103", dataLowTransmitter, false));
                    List<DataLoggerModel> t3 = new List<DataLoggerModel>();
                    t3.Add(dataLowTransmitter);
                    await dataLoggerAction.InsertDataLogger(t3, $"{imei}_103");

                    DataLoggerModel dataReverse = new DataLoggerModel();
                    dataReverse.TimeStamp = real.TimeStamp.AddHours(8);
                    dataReverse.Value = real.ReverseFlowWarning;
                    chUpdates.Add(($"{imei}_104", dataReverse, false));
                    List<DataLoggerModel> t4 = new List<DataLoggerModel>();
                    t4.Add(dataReverse);
                    await dataLoggerAction.InsertDataLogger(t4, $"{imei}_104");

                    DataLoggerModel dataDrying = new DataLoggerModel();
                    dataDrying.TimeStamp = real.TimeStamp.AddHours(8);
                    dataDrying.Value = real.DryingWarning;
                    chUpdates.Add(($"{imei}_105", dataDrying, false));
                    List<DataLoggerModel> t5 = new List<DataLoggerModel>();
                    t5.Add(dataDrying);
                    await dataLoggerAction.InsertDataLogger(t5, $"{imei}_105");

                    DataLoggerModel dataLowFlowMeter = new DataLoggerModel();
                    dataLowFlowMeter.TimeStamp = real.TimeStamp.AddHours(8);
                    dataLowFlowMeter.Value = real.LowFlowMeterVoltage;
                    chUpdates.Add(($"{imei}_106", dataLowFlowMeter, false));
                    List<DataLoggerModel> t6 = new List<DataLoggerModel>();
                    t6.Add(dataLowFlowMeter);
                    await dataLoggerAction.InsertDataLogger(t6, $"{imei}_106");

                    DataLoggerModel dataCommunication = new DataLoggerModel();
                    dataCommunication.TimeStamp = real.TimeStamp.AddHours(8);
                    dataCommunication.Value = real.CommunicationError;
                    chUpdates.Add(($"{imei}_107", dataCommunication, false));
                    List<DataLoggerModel> t7 = new List<DataLoggerModel>();
                    t7.Add(dataCommunication);
                    await dataLoggerAction.InsertDataLogger(t7, $"{imei}_107");


                    DataLoggerModel dataNetIndex = new DataLoggerModel();
                    dataNetIndex.TimeStamp = real.TimeStamp.AddHours(8);
                    dataNetIndex.Value = real.NetIndex;
                    chUpdates.Add(($"{imei}_108", dataNetIndex, false));

                    DataLoggerModel dataSignal = new DataLoggerModel();
                    dataSignal.TimeStamp = real.TimeStamp.AddHours(8);
                    dataSignal.Value = signal;
                    chUpdates.Add(($"{imei}_109", dataSignal, false));
                    List<DataLoggerModel> t9 = new List<DataLoggerModel>();
                    t9.Add(dataSignal);
                    await dataLoggerAction.InsertDataLogger(t9, $"{imei}_109");

                    DataLoggerModel dataBattery = new DataLoggerModel();
                    dataBattery.TimeStamp = real.TimeStamp.AddHours(8);
                    dataBattery.Value = battery;
                    chUpdates.Add(($"{imei}_110", dataBattery, false));
                    List<DataLoggerModel> t10 = new List<DataLoggerModel>();
                    t10.Add(dataBattery);
                    await dataLoggerAction.InsertDataLogger(t10, $"{imei}_110");

                    // flush all channel-config updates (realtime) in one BulkWrite
                    await channelConfigAction.BulkUpdateValues(chUpdates);

                    foreach (var chUpdate in chUpdates)
                    {
                        if (!chUpdate.isIndex)
                        {
                            await RealtimePublisher.PublishChannelUpdateAsync(imei, chUpdate.channelId, chUpdate.value.Value, chUpdate.value.TimeStamp);
                        }
                    }
                }

                List<LogSUModel> list = await analyzeDataAction.AnalyzeDataLog(logData, real.Unit);

                if (list.Count > 0)
                {
                    List<DataLoggerModel> listForwardFlow = new List<DataLoggerModel>();
                    List<DataLoggerModel> listReverseFlow = new List<DataLoggerModel>();
                    List<DataLoggerModel> listIndexForward = new List<DataLoggerModel>();
                    List<DataLoggerModel> listIndexReverse = new List<DataLoggerModel>();
                    List<DataLoggerModel> listNetIndex = new List<DataLoggerModel>();

                    double diffMinutes = 1;
                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        diffMinutes = (list[i].TimeStamp - list[i + 1].TimeStamp).TotalMinutes;
                        double diff = 60 / diffMinutes;

                        double forwarFlow = (list[i].ForwardIndex - list[i + 1].ForwardIndex) * diff;
                        double reverseFlow = (list[i].ReverseIndex - list[i + 1].ReverseIndex) * diff;

                        list[i].ForwardFlow = Math.Round(forwarFlow, 2);
                        list[i].ReverseFlow = Math.Round(reverseFlow, 2);

                        DataLoggerModel el = new DataLoggerModel();
                        el.TimeStamp = list[i].TimeStamp.AddHours(8);
                        el.Value = list[i].ForwardFlow;

                        DataLoggerModel el2 = new DataLoggerModel();
                        el2.TimeStamp = list[i].TimeStamp.AddHours(8);
                        el2.Value = list[i].ReverseFlow;

                        DataLoggerModel el3 = new DataLoggerModel();
                        el3.TimeStamp = list[i].TimeStamp.AddHours(8);
                        el3.Value = list[i].ForwardIndex;

                        DataLoggerModel el4 = new DataLoggerModel();
                        el4.TimeStamp = list[i].TimeStamp.AddHours(8);
                        el4.Value = list[i].ReverseIndex;

                        DataLoggerModel el5 = new DataLoggerModel();
                        el5.TimeStamp = list[i].TimeStamp.AddHours(8);
                        el5.Value = list[i].NetIndex;

                        listForwardFlow.Add(el);
                        listReverseFlow.Add(el2);
                        listIndexForward.Add(el3);
                        listIndexReverse.Add(el4);
                        listNetIndex.Add(el5);
                    }
                    if (channelForward != "")
                    {
                        DataLoggerModel lastIndexForward = await dataLoggerAction.GetLastValueIndexLogger(channelForward);

                        if (lastIndexForward != null)
                        {
                            if (lastIndexForward.TimeStamp != null && lastIndexForward.Value != null)
                            {
                                if (lastIndexForward.TimeStamp.Value == list[list.Count - 1].TimeStamp.AddMinutes(-diffMinutes))
                                {
                                    double diff = 60 / diffMinutes;
                                    double forwarFlow = (list[list.Count - 1].ForwardIndex - lastIndexForward.Value.Value) * diff;

                                    DataLoggerModel el = new DataLoggerModel();
                                    el.TimeStamp = list[list.Count - 1].TimeStamp.AddHours(8);
                                    el.Value = forwarFlow;

                                    DataLoggerModel el3 = new DataLoggerModel();
                                    el3.TimeStamp = list[list.Count - 1].TimeStamp.AddHours(8);
                                    el3.Value = list[list.Count - 1].ForwardIndex;

                                    DataLoggerModel el4 = new DataLoggerModel();
                                    el4.TimeStamp = listNetIndex[listNetIndex.Count - 1].TimeStamp.Value.AddHours(8);
                                    el4.Value = listNetIndex[listNetIndex.Count - 1].Value;


                                    listForwardFlow.Add(el);
                                    listIndexForward.Add(el3);
                                    listNetIndex.Add(el4);
                                }

                                listForwardFlow = listForwardFlow.Where(d => d.TimeStamp > lastIndexForward.TimeStamp.Value.AddHours(8)).ToList();
                                listIndexForward = listIndexForward.Where(d => d.TimeStamp > lastIndexForward.TimeStamp.Value.AddHours(8)).ToList();
                                listNetIndex = listNetIndex.Where(d => d.TimeStamp > lastIndexForward.TimeStamp.Value.AddHours(8)).ToList();
                            }

                        }

                        if (listForwardFlow.Count > 0)
                        {
                            listForwardFlow = listForwardFlow.OrderBy(d => d.TimeStamp).ToList();

                            await dataLoggerAction.InsertDataLogger(listForwardFlow, channelForward);

                        }
                        if (listIndexForward.Count > 0)
                        {
                            listIndexForward = listIndexForward.OrderBy(d => d.TimeStamp).ToList();

                            //await ChannelConfigAction.UpdateValueAction($"{imei}_98",listIndexForward[listIndexForward.Count - 1]);
                            await dataLoggerAction.InsertDataLogger(listIndexForward, $"{imei}_98");
                            await dataLoggerAction.InsertIndexLogger(listIndexForward, channelForward);
                        }

                        if (listNetIndex.Count > 0)
                        {
                            listNetIndex = listNetIndex.OrderBy(d => d.TimeStamp).ToList();

                            await dataLoggerAction.InsertDataLogger(listNetIndex, $"{imei}_108");

                        }

                    }

                    if (channelReverse != "")
                    {
                        DataLoggerModel lastIndexReverse = await dataLoggerAction.GetLastValueIndexLogger(channelReverse);

                        if (lastIndexReverse != null)
                        {

                            if (lastIndexReverse.TimeStamp != null && lastIndexReverse.Value != null)
                            {
                                if (lastIndexReverse.TimeStamp.Value == list[list.Count - 1].TimeStamp.AddMinutes(-diffMinutes))
                                {
                                    double diff = 60 / diffMinutes;
                                    double reverseflow = (list[list.Count - 1].ReverseIndex - lastIndexReverse.Value.Value) * diff;

                                    DataLoggerModel el2 = new DataLoggerModel();
                                    el2.TimeStamp = list[list.Count - 1].TimeStamp.AddHours(8);
                                    el2.Value = reverseflow;

                                    DataLoggerModel el4 = new DataLoggerModel();
                                    el4.TimeStamp = list[list.Count - 1].TimeStamp.AddHours(8);
                                    el4.Value = list[list.Count - 1].ReverseIndex;

                                    listReverseFlow.Add(el2);
                                    listIndexReverse.Add(el4);
                                }

                                listReverseFlow = listReverseFlow.Where(d => d.TimeStamp > lastIndexReverse.TimeStamp.Value.AddHours(8)).ToList();
                                listIndexReverse = listIndexReverse.Where(d => d.TimeStamp > lastIndexReverse.TimeStamp.Value.AddHours(8)).ToList();
                            }


                        }

                        if (listReverseFlow.Count > 0)
                        {
                            listReverseFlow = listReverseFlow.OrderBy(d => d.TimeStamp).ToList();
                            await dataLoggerAction.InsertDataLogger(listReverseFlow, channelReverse);

                        }
                        if (listIndexReverse.Count > 0)
                        {
                            listIndexReverse = listIndexReverse.OrderBy(d => d.TimeStamp).ToList();

                            //await ChannelConfigAction.UpdateValueAction($"{imei}_99", listIndexReverse[listIndexReverse.Count - 1]);
                            await dataLoggerAction.InsertDataLogger(listIndexReverse, $"{imei}_99");
                            await dataLoggerAction.InsertIndexLogger(listIndexReverse, channelReverse);
                        }

                    }

                }

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }

        public async void HandleDataBatteryAndSignalSUMeter(string imei, double signal, double battery)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            ChannelConfigAction channelConfigAction = new ChannelConfigAction();
            DataLoggerAction dataLoggerAction = new DataLoggerAction();
            

            try
            {
                List<ChannelConfigModel> channel = await channelConfigAction.GetChannelByLoggerId(imei);

                string channelBattery = channel.Where(c => c.BatMetterChannel == true).FirstOrDefault()?.ChannelId ?? "";
                string channelSignal = channel.Where(c => c.BatLoggerChannel == true).FirstOrDefault()?.ChannelId ?? "";

                if (channelBattery != "")
                {
                    DateTime now = DateTime.Now;

                    DataLoggerModel el = new DataLoggerModel();
                    el.TimeStamp = now;
                    el.Value = battery;

                    DataLoggerModel elLog = new DataLoggerModel();
                    elLog.TimeStamp = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
                    elLog.Value = battery;

                    List<DataLoggerModel> l = new List<DataLoggerModel>();
                    l.Add(elLog);


                    await channelConfigAction.UpdateValueAction(channelBattery, el);
                    await dataLoggerAction.InsertDataLogger(l, channelBattery);


                }
                if (channelSignal != "")
                {
                    DateTime now = DateTime.Now;

                    DataLoggerModel el = new DataLoggerModel();
                    el.TimeStamp = now;
                    el.Value = signal;

                    DataLoggerModel elLog = new DataLoggerModel();
                    elLog.TimeStamp = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
                    elLog.Value = signal;

                    List<DataLoggerModel> l = new List<DataLoggerModel>();
                    l.Add(elLog);


                    await channelConfigAction.UpdateValueAction(channelSignal, el);
                    await dataLoggerAction.InsertDataLogger(l, channelSignal);

                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }
        public async Task HandleDataLevelMeter(string payload, Dictionary<string, JsonElement> Log, DateTime time, string imei, double battery, string siteid, string location, int signal)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            AnalyzeDataAction analyzeDataAction = new AnalyzeDataAction();
            ChannelConfigAction channelConfigAction = new ChannelConfigAction();
            DataLoggerAction dataLoggerAction = new DataLoggerAction();
            try
            {
                LogLevelModel realtimeData = await analyzeDataAction.AnalyzeDataRealTimeLevelMeter(payload, time, siteid, location, imei, signal, battery);
                List<LogLevelModel> logs = await analyzeDataAction.AnalyzeLogDataLevelMeter(Log);

                var chUpdates = new List<(string channelId, DataLoggerModel value, bool isIndex)>();

                DataLoggerModel dataLevel = new DataLoggerModel { Value = realtimeData.Level ?? 0, TimeStamp = realtimeData.TimeStamp.AddHours(8) };
                DataLoggerModel dataBattery = new DataLoggerModel { Value = battery, TimeStamp = DateTime.Now.AddHours(8) };
                DataLoggerModel dataSignal = new DataLoggerModel { Value = signal, TimeStamp = DateTime.Now.AddHours(8) };

                chUpdates.Add(($"{imei}_200", dataLevel, false));
                chUpdates.Add(($"{imei}_05", dataBattery, false));
                chUpdates.Add(($"{imei}_07", dataSignal, false));

                await channelConfigAction.BulkUpdateValues(chUpdates);

                foreach (var chUpdate in chUpdates)
                {
                    if (!chUpdate.isIndex)
                    {
                        await RealtimePublisher.PublishChannelUpdateAsync(imei, chUpdate.channelId, chUpdate.value.Value, chUpdate.value.TimeStamp);
                    }
                }

                await dataLoggerAction.InsertDataLogger(new List<DataLoggerModel> { dataBattery }, $"{imei}_05");
                await dataLoggerAction.InsertDataLogger(new List<DataLoggerModel> { dataSignal }, $"{imei}_07");

                if (logs.Count > 0)
                {
                    List<DataLoggerModel> dataInsertLevel = new List<DataLoggerModel>();
                    foreach (var item in logs)
                        dataInsertLevel.Add(new DataLoggerModel { Value = item.Level ?? 0, TimeStamp = item.TimeStamp.AddHours(8) });
                    await dataLoggerAction.InsertDataLogger(dataInsertLevel, $"{imei}_200");
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }

        public async Task HandleDataLevelMeterOverTime(string payload, Dictionary<string, JsonElement> Log, DateTime time, string imei, double battery, string siteid, string location, int signal)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            AnalyzeDataAction analyzeDataAction = new AnalyzeDataAction();
            ChannelConfigAction channelConfigAction = new ChannelConfigAction();
            DataLoggerAction dataLoggerAction = new DataLoggerAction();
            try
            {
                LogLevelModel realtimeData = await analyzeDataAction.AnalyzeDataRealTimeLevelMeter(payload, time, siteid, location, imei, signal, battery);
                List<LogLevelModel> logs = await analyzeDataAction.AnalyzeLogDataLevelMeter(Log);

                DateTime now = DateTime.Now.AddHours(8);
                DateTime realtime = realtimeData.TimeStamp.AddHours(8);

                var chUpdates = new List<(string channelId, DataLoggerModel value, bool isIndex)>();

                DataLoggerModel dataLevel = new DataLoggerModel { Value = realtimeData.Level ?? 0, TimeStamp = now };
                DataLoggerModel dataBattery = new DataLoggerModel { Value = battery, TimeStamp = now };
                DataLoggerModel dataSignal = new DataLoggerModel { Value = signal, TimeStamp = now };

                chUpdates.Add(($"{imei}_200", dataLevel, false));
                chUpdates.Add(($"{imei}_05", dataBattery, false));
                chUpdates.Add(($"{imei}_07", dataSignal, false));

                await channelConfigAction.BulkUpdateValues(chUpdates);

                foreach (var chUpdate in chUpdates)
                {
                    if (!chUpdate.isIndex)
                    {
                        await RealtimePublisher.PublishChannelUpdateAsync(imei, chUpdate.channelId, chUpdate.value.Value, chUpdate.value.TimeStamp);
                    }
                }

                await dataLoggerAction.InsertDataLogger(new List<DataLoggerModel> { dataBattery }, $"{imei}_05");
                await dataLoggerAction.InsertDataLogger(new List<DataLoggerModel> { dataSignal }, $"{imei}_07");

                if (logs.Count > 0)
                {
                    DateTime? currentTime = await dataLoggerAction.GetCurrentTimeStampDataLogger($"{imei}_200");
                    List<DataLoggerModel> dataInsertLevel = new List<DataLoggerModel>();
                    foreach (var item in logs)
                    {
                        DateTime timeLog = item.TimeStamp.AddHours(8);
                        double diff = Math.Abs((realtime - timeLog).TotalSeconds);
                        DateTime realTimeLog = now.AddSeconds(-diff);
                        realTimeLog = new DateTime(realTimeLog.Year, realTimeLog.Month, realTimeLog.Day, realTimeLog.Hour, item.TimeStamp.Minute, 0);
                        realTimeLog = realTimeLog.AddHours(8);

                        if (currentTime == null || DateTime.Compare(realTimeLog, currentTime.Value.AddHours(7)) > 0)
                            dataInsertLevel.Add(new DataLoggerModel { Value = item.Level ?? 0, TimeStamp = realTimeLog });
                    }
                    await dataLoggerAction.InsertDataLogger(dataInsertLevel, $"{imei}_200");
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Chặn finalizer gọi lại Dispose
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Giải phóng managed resources ở đây
                    Console.WriteLine("Disposing handle data managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~HandleDataAction() { 
        
            Dispose(false);
        }
    }
}
