using MQTT_Vilog_Malaysia.Models;
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

        public async void HandleDataKronheMeter(string stringRealTime, Dictionary<string, JsonElement> Log, DateTime time, string imei, double battery, string siteid, string location, int signal)
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

                if (channelForward.ChannelId != "")
                {
                    DataLoggerModel dataUpdate = new DataLoggerModel();
                    dataUpdate.Value = Math.Round(realtimeData.ForwardFlow, 2);
                    dataUpdate.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                    DataLoggerModel dataUpdateIndex = new DataLoggerModel();
                    dataUpdateIndex.Value = Math.Round(realtimeData.ForwardIndex, 2);
                    dataUpdateIndex.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                    await channelConfigAction.UpdateValueAction(channelForward.ChannelId, dataUpdate);
                    await channelConfigAction.UpdateIndexValueAction(channelForward.ChannelId, dataUpdateIndex);

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
                if (channelReverse.ChannelId != "")
                {

                    DataLoggerModel dataUpdate = new DataLoggerModel();
                    dataUpdate.Value = Math.Round(realtimeData.ReverseFlow, 2);
                    dataUpdate.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                    DataLoggerModel dataUpdateIndex = new DataLoggerModel();
                    dataUpdateIndex.Value = Math.Round(realtimeData.ReverseIndex, 2);
                    dataUpdateIndex.TimeStamp = realtimeData.TimeStamp.AddHours(8);

                    await channelConfigAction.UpdateValueAction(channelReverse.ChannelId, dataUpdate);
                    await channelConfigAction.UpdateIndexValueAction(channelReverse.ChannelId, dataUpdateIndex);

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

                await channelConfigAction.UpdateValueAction($"{imei}_98", dataUpdateForwardTotal);
                await channelConfigAction.UpdateValueAction($"{imei}_99", dataUpdateReverseTotal);
                await channelConfigAction.UpdateValueAction($"{imei}_100", dataUpdateNetTotal);
                await channelConfigAction.UpdateValueAction($"{imei}_101", dataUpdateAlarm);
                await channelConfigAction.UpdateValueAction($"{imei}_05", dataBattery);
                await channelConfigAction.UpdateValueAction($"{imei}_06", dataBatteryCapacity);
                await channelConfigAction.UpdateValueAction($"{imei}_07", dataSignal);


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

        public async void HandleDataKronheMeterOverTime(string stringRealTime, Dictionary<string, JsonElement> Log, DateTime time, string imei, double battery, string siteid, string location, int signal)
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

                if (channelForward.ChannelId != "")
                {
                    DataLoggerModel dataUpdate = new DataLoggerModel();
                    dataUpdate.Value = Math.Round(realtimeData.ForwardFlow, 2);
                    dataUpdate.TimeStamp = DateTime.Now.AddHours(8);

                    DataLoggerModel dataUpdateIndex = new DataLoggerModel();
                    dataUpdateIndex.Value = Math.Round(realtimeData.ForwardIndex, 2);
                    dataUpdateIndex.TimeStamp = DateTime.Now.AddHours(8);

                    await channelConfigAction.UpdateValueAction(channelForward.ChannelId, dataUpdate);
                    await channelConfigAction.UpdateIndexValueAction(channelForward.ChannelId, dataUpdateIndex);

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

                        DateTime? currentTime = await dataLoggerAction.GetCurrentTimeStampDataLogger(channelForward.ChannelId);

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
                if (channelReverse.ChannelId != "")
                {

                    DataLoggerModel dataUpdate = new DataLoggerModel();
                    dataUpdate.Value = Math.Round(realtimeData.ReverseFlow, 2);
                    dataUpdate.TimeStamp = DateTime.Now.AddHours(8);

                    DataLoggerModel dataUpdateIndex = new DataLoggerModel();
                    dataUpdateIndex.Value = Math.Round(realtimeData.ReverseIndex, 2);
                    dataUpdateIndex.TimeStamp = DateTime.Now.AddHours(8);

                    await channelConfigAction.UpdateValueAction(channelReverse.ChannelId, dataUpdate);
                    await channelConfigAction.UpdateIndexValueAction(channelReverse.ChannelId, dataUpdateIndex);

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

                        DateTime? currentTime = await dataLoggerAction.GetCurrentTimeStampDataLogger(channelForward.ChannelId);


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

                await channelConfigAction.UpdateValueAction($"{imei}_98", dataUpdateForwardTotal);
                await channelConfigAction.UpdateValueAction($"{imei}_99", dataUpdateReverseTotal);
                await channelConfigAction.UpdateValueAction($"{imei}_100", dataUpdateNetTotal);
                await channelConfigAction.UpdateValueAction($"{imei}_101", dataUpdateAlarm);
                await channelConfigAction.UpdateValueAction($"{imei}_05", dataBattery);
                await channelConfigAction.UpdateValueAction($"{imei}_06", dataBatteryCapacity);
                await channelConfigAction.UpdateValueAction($"{imei}_07", dataSignal);


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
        public async void HandleDataSUMeter(string stringRealTime, List<string> logData, string imei, string siteid, string location, int signal, double battery)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            AnalyzeDataAction analyzeDataAction = new AnalyzeDataAction();
            ChannelConfigAction channelConfigAction = new ChannelConfigAction();
            DataLoggerAction dataLoggerAction = new DataLoggerAction();

            try
            {

                RealTimeModel real = await analyzeDataAction.AnalyzeDataRealTimeSUMeter(stringRealTime, siteid, location, imei, signal, battery);
                List<ChannelConfigModel> channel = await channelConfigAction.GetChannelByLoggerId(imei);

                string channelForward = channel.Where(c => c.ForwardFlow == true).FirstOrDefault().ChannelId;
                string channelReverse = channel.Where(c => c.ReverseFlow == true).FirstOrDefault().ChannelId;

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

                        await channelConfigAction.UpdateValueAction(channelForward, data);
                        await channelConfigAction.UpdateIndexValueAction(channelForward, index);

                        await channelConfigAction.UpdateValueAction($"{imei}_98", index);
                    }
                    if (channelReverse != "")
                    {
                        DataLoggerModel data = new DataLoggerModel();
                        data.TimeStamp = real.TimeStamp.AddHours(8);
                        data.Value = real.ReverseFlow;

                        DataLoggerModel index = new DataLoggerModel();
                        index.TimeStamp = real.TimeStamp.AddHours(8);
                        index.Value = real.ReverseIndex;

                        await channelConfigAction.UpdateValueAction(channelReverse, data);
                        await channelConfigAction.UpdateIndexValueAction(channelReverse, index);

                        await channelConfigAction.UpdateValueAction($"{imei}_99", index);
                    }


                    DataLoggerModel datamodbus = new DataLoggerModel();
                    datamodbus.TimeStamp = real.TimeStamp.AddHours(8);
                    datamodbus.Value = real.ModbusPowerSupplyDown;
                    await channelConfigAction.UpdateValueAction($"{imei}_100", datamodbus);
                    List<DataLoggerModel> t = new List<DataLoggerModel>();
                    t.Add(datamodbus);
                    await dataLoggerAction.InsertDataLogger(t, $"{imei}_100");

                    DataLoggerModel dataMemory = new DataLoggerModel();
                    dataMemory.TimeStamp = real.TimeStamp.AddHours(8);
                    dataMemory.Value = real.MemoryError;
                    await channelConfigAction.UpdateValueAction($"{imei}_101", dataMemory);
                    List<DataLoggerModel> t2 = new List<DataLoggerModel>();
                    t2.Add(dataMemory);
                    await dataLoggerAction.InsertDataLogger(t2, $"{imei}_101");

                    DataLoggerModel dataLowTransmitter = new DataLoggerModel();
                    dataLowTransmitter.TimeStamp = real.TimeStamp.AddHours(8);
                    dataLowTransmitter.Value = real.LowTransmitterVoltage;
                    await channelConfigAction.UpdateValueAction($"{imei}_103", dataLowTransmitter);
                    List<DataLoggerModel> t3 = new List<DataLoggerModel>();
                    t3.Add(dataLowTransmitter);
                    await dataLoggerAction.InsertDataLogger(t3, $"{imei}_103");

                    DataLoggerModel dataReverse = new DataLoggerModel();
                    dataReverse.TimeStamp = real.TimeStamp.AddHours(8);
                    dataReverse.Value = real.ReverseFlowWarning;
                    await channelConfigAction.UpdateValueAction($"{imei}_104", dataReverse);
                    List<DataLoggerModel> t4 = new List<DataLoggerModel>();
                    t4.Add(dataReverse);
                    await dataLoggerAction.InsertDataLogger(t4, $"{imei}_104");

                    DataLoggerModel dataDrying = new DataLoggerModel();
                    dataDrying.TimeStamp = real.TimeStamp.AddHours(8);
                    dataDrying.Value = real.DryingWarning;
                    await channelConfigAction.UpdateValueAction($"{imei}_105", dataDrying);
                    List<DataLoggerModel> t5 = new List<DataLoggerModel>();
                    t5.Add(dataDrying);
                    await dataLoggerAction.InsertDataLogger(t5, $"{imei}_105");

                    DataLoggerModel dataLowFlowMeter = new DataLoggerModel();
                    dataLowFlowMeter.TimeStamp = real.TimeStamp.AddHours(8);
                    dataLowFlowMeter.Value = real.LowFlowMeterVoltage;
                    await channelConfigAction.UpdateValueAction($"{imei}_106", dataLowFlowMeter);
                    List<DataLoggerModel> t6 = new List<DataLoggerModel>();
                    t6.Add(dataLowFlowMeter);
                    await dataLoggerAction.InsertDataLogger(t6, $"{imei}_106");

                    DataLoggerModel dataCommunication = new DataLoggerModel();
                    dataCommunication.TimeStamp = real.TimeStamp.AddHours(8);
                    dataCommunication.Value = real.CommunicationError;
                    await channelConfigAction.UpdateValueAction($"{imei}_107", dataCommunication);
                    List<DataLoggerModel> t7 = new List<DataLoggerModel>();
                    t7.Add(dataCommunication);
                    await dataLoggerAction.InsertDataLogger(t7, $"{imei}_107");


                    DataLoggerModel dataNetIndex = new DataLoggerModel();
                    dataNetIndex.TimeStamp = real.TimeStamp.AddHours(8);
                    dataNetIndex.Value = real.NetIndex;
                    await channelConfigAction.UpdateValueAction($"{imei}_108", dataNetIndex);

                    DataLoggerModel dataSignal = new DataLoggerModel();
                    dataSignal.TimeStamp = real.TimeStamp.AddHours(8);
                    dataSignal.Value = signal;
                    await channelConfigAction.UpdateValueAction($"{imei}_109", dataSignal);
                    List<DataLoggerModel> t9 = new List<DataLoggerModel>();
                    t9.Add(dataSignal);
                    await dataLoggerAction.InsertDataLogger(t9, $"{imei}_109");

                    DataLoggerModel dataBattery = new DataLoggerModel();
                    dataBattery.TimeStamp = real.TimeStamp.AddHours(8);
                    dataBattery.Value = battery;
                    await channelConfigAction.UpdateValueAction($"{imei}_110", dataBattery);
                    List<DataLoggerModel> t10 = new List<DataLoggerModel>();
                    t10.Add(dataBattery);
                    await dataLoggerAction.InsertDataLogger(t10, $"{imei}_110");
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

                string channelBattery = channel.Where(c => c.BatMetterChannel == true).FirstOrDefault().ChannelId;
                string channelSignal = channel.Where(c => c.BatLoggerChannel == true).FirstOrDefault().ChannelId;

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
