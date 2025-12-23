using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class AnalyzeDataAction : IDisposable
    {
        private bool disposed = false;

        public async Task<LogKronheModel> AnalyzeDataRealTimeHronheMeter(string realTimeString, DateTime time, string siteid, string location, string loggerid, int signal, double battery)
        {
            LogKronheModel log = new LogKronheModel();
            WriteLogAction writeLogAction = new WriteLogAction();
            ConvertHexToDoubleAction convertAction = new ConvertHexToDoubleAction();


            try
            {
                List<string> reg = new List<string>();

                for (int i = 2; i < realTimeString.Length; i += 8)
                {
                    reg.Add(realTimeString.Substring(i, 8));
                }

                if (reg.Count > 0)
                {
                    double flow = convertAction.ConvertHexToDouble(reg[0]);

                    if (flow > 0)
                    {
                        log.ForwardFlow = flow;
                        log.ReverseFlow = 0;
                    }
                    else
                    {
                        log.ReverseFlow = flow;
                        log.ForwardFlow = 0;
                    }

                    log.TimeStamp = time;
                    log.NetIndex = convertAction.ConvertHexToDouble(reg[1]);
                    log.ForwardIndex = convertAction.ConvertHexToDouble(reg[2]);
                    log.ReverseIndex = convertAction.ConvertHexToDouble(reg[3]);
                    log.BatteryRemain = convertAction.ConvertHexToDouble(reg[5]);
                    log.Alarm = Convert.ToInt32(reg[4], 16);
                    if(log.Alarm > 0)
                    {
                        HistoryAlarmModel alarm = new HistoryAlarmModel();
                        alarm.SiteId = siteid;
                        alarm.Location = location;
                        alarm.LoggerId = loggerid;
                        alarm.ChannelId = $"{loggerid}_101";
                        alarm.ChannelName = "6. Alarm";
                        alarm.Content = "";
                        alarm.TimeStampHasValue = DateTime.Now.AddHours(8);
                        alarm.TimeStampAlarm = DateTime.Now.AddHours(8);
                        

                        if (log.Alarm == 1)
                        {
                            alarm.Content = "Active Flow Meansurement Warning Condition";
                            alarm.Type = 10;
                        }
                        else if (log.Alarm == 2)
                        {
                            alarm.Content = "Active < 10% Battery Warning Condition";
                            alarm.Type = 11;
                        }
                        else if (log.Alarm == 4)
                        {
                            alarm.Content = "Active EEPROM Error Condition";
                            alarm.Type = 12;
                        }
                        else if (log.Alarm == 8)
                        {
                            alarm.Content = "Active Communication Error Condition";
                            alarm.Type = 13;
                        }
                        else if (log.Alarm == 16)
                        {
                            alarm.Content = "Empty Pipe";
                            alarm.Type = 14;
                        }
                        else if (log.Alarm == 32)
                        {
                            alarm.Content = "Main Power Failure";
                            alarm.Type = 15;
                        }
                        

                        using (HistoryAlarmAction historyAlarmAction = new HistoryAlarmAction())
                        {
                            HistoryAlarmModel his = await historyAlarmAction.GetLatestAlarm(alarm.SiteId);

                            bool isInsertAlarm = false;

                            if (his != null)
                            {
                                if (his.TimeStampAlarm != null)
                                {
                                    double diff = (alarm.TimeStampAlarm.Value - his.TimeStampAlarm.Value).TotalMinutes;
                                    //if (diff > 60)
                                    //{
                                    //    isInsertAlarm = true;
                                    //}
                                    if(his.Type != alarm.Type)
                                    {
                                        isInsertAlarm = true;
                                    }
                                }
                                else
                                {
                                    isInsertAlarm = true;

                                }
                            }
                            else
                            {
                                isInsertAlarm = true;

                            }

                            if (isInsertAlarm)
                            {
                                //alarm.TimeStampAlarm = alarm.TimeStampAlarm.Value.AddHours(8);
                                //alarm.TimeStampHasValue = alarm.TimeStampHasValue.Value.AddHours(8);
                                historyAlarmAction.InsertAlarm(alarm);

                                // push notification
                                using (DeviceTokenAppAction deviceTokenAppAction = new DeviceTokenAppAction())
                                {
                                    List<DeviceTokenAppModel> listToken = await deviceTokenAppAction.GetDeivceTokenApps();
                                    if (listToken.Count > 0)
                                    {
                                        using (NotificationAction notificationAction = new NotificationAction())
                                        {
                                            string contentPush = $"Channel {alarm.ChannelName} with value: {log.Alarm} is {alarm.Content}";

                                            await notificationAction.SubmitNotification(alarm.Location, alarm.Location, contentPush, listToken);
                                        }
                                    }
                                }
                            }

                        }
                    }
                    
                }

                // insert alarm for battery 
                if(battery <= 3.5)
                {
                    HistoryAlarmModel alarmBattery = new HistoryAlarmModel();
                    alarmBattery.SiteId = siteid;
                    alarmBattery.Location = location;
                    alarmBattery.LoggerId = loggerid;
                    alarmBattery.ChannelId = $"{loggerid}_05";
                    alarmBattery.ChannelName = "7. Battery Logger";
                    alarmBattery.Content = "";
                    alarmBattery.TimeStampHasValue = DateTime.Now.AddHours(8);
                    alarmBattery.TimeStampAlarm = DateTime.Now.AddHours(8);

                    if (battery >= 3.4)
                    {
                        alarmBattery.Content = "Low battery";
                        alarmBattery.Type = 16;
                    }
                    else
                    {
                        alarmBattery.Content = "Out Of Battery";
                        alarmBattery.Type = 17;
                    }

                    using (HistoryAlarmAction historyAlarmAction = new HistoryAlarmAction())
                    {
                        HistoryAlarmModel his = await historyAlarmAction.GetLatestAlarm(alarmBattery.SiteId);

                        bool isInsertAlarm = false;

                        if (his != null)
                        {
                            if (his.TimeStampAlarm != null)
                            {
                                //double diff = (alarmBattery.TimeStampAlarm.Value - his.TimeStampAlarm.Value).TotalMinutes;
                                //if (diff > 60)
                                //{
                                //    isInsertAlarm = true;
                                //}
                                if (his.Type != alarmBattery.Type)
                                {
                                    isInsertAlarm = true;
                                }
                            }
                            else
                            {
                                isInsertAlarm = true;

                            }
                        }
                        else
                        {
                            isInsertAlarm = true;

                        }

                        if (isInsertAlarm)
                        {
                            //alarmBattery.TimeStampAlarm = alarmBattery.TimeStampAlarm.Value.AddHours(8);
                            //alarmBattery.TimeStampHasValue = alarmBattery.TimeStampHasValue.Value.AddHours(8);
                            historyAlarmAction.InsertAlarm(alarmBattery);

                            // push notification
                            using (DeviceTokenAppAction deviceTokenAppAction = new DeviceTokenAppAction())
                            {
                                List<DeviceTokenAppModel> listToken = await deviceTokenAppAction.GetDeivceTokenApps();
                                if (listToken.Count > 0)
                                {
                                    using (NotificationAction notificationAction = new NotificationAction())
                                    {
                                        string contentPush = $"Channel {alarmBattery.ChannelName} with value: {battery} is {alarmBattery.Content}";
                                        await notificationAction.SubmitNotification(alarmBattery.Location, alarmBattery.Location, alarmBattery.Content, listToken);
                                    }
                                }
                            }
                        }

                    }
                }

                // insert alarm for signal
                if (signal < 20)
                {
                    HistoryAlarmModel alarmSignal = new HistoryAlarmModel();
                    alarmSignal.SiteId = siteid;
                    alarmSignal.Location = location;
                    alarmSignal.LoggerId = loggerid;
                    alarmSignal.ChannelId = $"{loggerid}_07";
                    alarmSignal.ChannelName = "9. Signal";
                    alarmSignal.Content = "Low signal";
                    alarmSignal.TimeStampHasValue = DateTime.Now.AddHours(8);
                    alarmSignal.TimeStampAlarm = DateTime.Now.AddHours(8);
                    alarmSignal.Type = 18;
                    

                    using (HistoryAlarmAction historyAlarmAction = new HistoryAlarmAction())
                    {
                        HistoryAlarmModel his = await historyAlarmAction.GetLatestAlarm(alarmSignal.SiteId);

                        bool isInsertAlarm = false;

                        if (his != null)
                        {
                            if (his.TimeStampAlarm != null)
                            {
                                //double diff = (alarmSignal.TimeStampAlarm.Value - his.TimeStampAlarm.Value).TotalMinutes;
                                //if (diff > 60)
                                //{
                                //    isInsertAlarm = true;
                                //}

                                if (his.Type != alarmSignal.Type)
                                {
                                    isInsertAlarm = true;
                                }
                            }
                            else
                            {
                                isInsertAlarm = true;

                            }
                        }
                        else
                        {
                            isInsertAlarm = true;

                        }

                        if (isInsertAlarm)
                        {
                            //alarmSignal.TimeStampAlarm = alarmSignal.TimeStampAlarm.Value.AddHours(8);
                            //alarmSignal.TimeStampHasValue = alarmSignal.TimeStampHasValue.Value.AddHours(8);
                            historyAlarmAction.InsertAlarm(alarmSignal);

                            // push notification
                            using (DeviceTokenAppAction deviceTokenAppAction = new DeviceTokenAppAction())
                            {
                                List<DeviceTokenAppModel> listToken = await deviceTokenAppAction.GetDeivceTokenApps();
                                if (listToken.Count > 0)
                                {
                                    using (NotificationAction notificationAction = new NotificationAction())
                                    {
                                        string contentPush = $"Channel {alarmSignal.ChannelName} with value: {signal} is {alarmSignal.Content}";
                                        await notificationAction.SubmitNotification(alarmSignal.Location, alarmSignal.Location , contentPush, listToken);
                                    }
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
            return log;

        }

        public async Task<List<LogKronheModel>> AnalyzeLogDataHronheMeter(Dictionary<string, JsonElement> Log)
        {
            List<LogKronheModel> list = new List<LogKronheModel>();
            WriteLogAction writeLogAction = new WriteLogAction();
            ConvertHexToDoubleAction convertAction = new ConvertHexToDoubleAction();

            try
            {
                foreach (var d in Log)
                {
                    List<string> data = JsonSerializer.Deserialize<List<string>>(d.Value.GetRawText());

                    List<string> reg = new List<string>();

                    for (int i = 2; i < data[0].Length; i += 8)
                    {
                        reg.Add(data[0].Substring(i, 8));
                    }
                    DateTime time = DateTime.Parse(data[1]).ToUniversalTime();
                    time = time.AddHours(8);

                    LogKronheModel log = new LogKronheModel();

                    if (reg.Count > 0)
                    {
                        double flow = convertAction.ConvertHexToDouble(reg[0]);

                        if (flow > 0)
                        {
                            log.ForwardFlow = flow;
                            log.ReverseFlow = 0;
                        }
                        else
                        {
                            log.ReverseFlow = flow;
                            log.ForwardFlow = 0;
                        }

                        log.TimeStamp = time;
                        log.NetIndex = convertAction.ConvertHexToDouble(reg[1]);
                        log.ForwardIndex = convertAction.ConvertHexToDouble(reg[2]);
                        log.ReverseIndex = convertAction.ConvertHexToDouble(reg[3]);
                        log.BatteryRemain = convertAction.ConvertHexToDouble(reg[5]);
                        log.Alarm = Convert.ToInt32(reg[4], 16);
                    }

                    list.Add(log);
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return list;
        }

        public async Task<RealTimeModel> AnalyzeDataRealTimeSUMeter(string realTimeString, string siteid, string location, string loggerid, int signal, double battery)
        {
            RealTimeModel el = new RealTimeModel();

            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                List<string> reg = new List<string>();

                for (int i = 2; i < realTimeString.Length; i += 4)
                {
                    reg.Add(realTimeString.Substring(i, 4));
                }

                if (reg.Count > 0)
                {

                    int direction = 0;
                    int rateReg = 0;
                    int unit = 0;
                    double rate = 0;
                    double flow = 0;
                    int flowRateReg = 0;
                    double flowRate = 0;

                    int status = Convert.ToInt32(reg[reg.Count -1], 16);

                    string s = Convert.ToString(status, 2); //Convert to binary in a string

                    int[] bits = s.PadLeft(16, '0') // Add 0's from left
                         .Select(c => int.Parse(c.ToString())) // convert each char to int
                         .ToArray();

                    Array.Reverse(bits);

                    direction = Convert.ToInt32(reg[0], 16);
                    rateReg = Convert.ToInt32(reg[2], 16);
                    unit = Convert.ToInt32(reg[3], 16);
                    flowRateReg = Convert.ToInt32(reg[13], 16);

                    if (rateReg == 0)
                    {
                        rate = 1;
                    }
                    else if (rateReg == 4)
                    {
                        rate = 0.1;
                    }
                    else if (rateReg == 2)
                    {
                        rate = 0.01;

                    }
                    else if (rateReg == 1)
                    {
                        rate = 0.001;
                    }

                    if (flowRateReg == 0)
                    {
                        flowRate = 0.1;
                    }
                    else if (flowRateReg == 1)
                    {
                        flowRate = 0.01;
                    }
                    else if (flowRateReg == 2)
                    {
                        flowRate = 0.001;
                    }
                    else if (flowRateReg == 3)
                    {
                        flowRate = 0.0001;
                    }

                    if (direction == 0)
                    {
                        direction = 1;
                    }
                    else
                    {
                        direction = -1;
                    }

                    flow = Convert.ToInt32(reg[1], 16) * rate * direction;

                    if (flow < 0)
                    {
                        el.ReverseFlow = Math.Abs(flow);
                        el.ForwardFlow = 0;
                    }
                    else
                    {
                        el.ForwardFlow = Math.Abs(flow);
                        el.ReverseFlow = 0;
                    }

                    el.NetIndex = (Convert.ToInt32(reg[4], 16) * Math.Pow(2, 32) + Convert.ToInt32(reg[5], 16) * Math.Pow(2, 16) + Convert.ToInt32(reg[6], 16)) * flowRate;
                    el.ForwardIndex = (Convert.ToInt32(reg[7], 16) * Math.Pow(2, 32) + Convert.ToInt32(reg[8], 16) * Math.Pow(2, 16) + Convert.ToInt32(reg[9], 16)) * flowRate;
                    el.ReverseIndex = (Convert.ToInt32(reg[10], 16) * Math.Pow(2, 32) + Convert.ToInt32(reg[11], 16) * Math.Pow(2, 16) + Convert.ToInt32(reg[12], 16)) * flowRate;

                    if (unit == 3)
                    {
                        el.ReverseFlow = el.ReverseFlow * 3600;
                        el.ForwardFlow = el.ForwardFlow * 3600;
                        el.NetIndex = el.NetIndex * 3600;
                        el.ForwardIndex = el.ForwardIndex * 3600;
                        el.ReverseIndex = el.ReverseIndex * 3600;
                    }

                    el.ReverseFlow = Math.Round(el.ReverseFlow, 2);
                    el.ForwardFlow = Math.Round(el.ForwardFlow, 2);
                    el.NetIndex = Math.Round(el.NetIndex, 2);
                    el.ForwardIndex = Math.Round(el.ForwardIndex, 2);
                    el.ReverseIndex = Math.Round(el.ReverseIndex, 2);
                    el.TimeStamp = DateTime.Now;
                    el.ModbusPowerSupplyDown = bits[0];
                    el.MemoryError = bits[1];
                    el.LowTransmitterVoltage = bits[2];
                    el.ReverseFlowWarning = bits[3];
                    el.DryingWarning = bits[4];
                    el.LowFlowMeterVoltage = bits[5];
                    el.CommunicationError = bits[6];
                    el.Unit = unit;

                    string content = "";
                    string channelId = "";
                    string channelName = "";
                    int type = 0;

                    if (el.ModbusPowerSupplyDown > 0)
                    {
                        content = "Modbus Power Supply Down";
                        channelId = $"{loggerid}_100";
                        channelName = "1.1 Power Supply Down";
                        type = 19;

                    }
                    else if (el.MemoryError > 0)
                    {
                        content = "Memory Error";
                        channelId = $"{loggerid}_101";
                        channelName = "1.2 Memmory Error";
                        type = 20;
                    }
                    else if (el.LowTransmitterVoltage > 0)
                    {
                        content = "Low Trasmitter Voltage";
                        channelId = $"{loggerid}_103";
                        channelName = "1.3 Low Transmitter Voltage";
                        type = 21;
                    }
                    else if (el.ReverseFlowWarning > 0)
                    {
                        content = "Reverse Flow Warning";
                        channelId = $"{loggerid}_104";
                        channelName = "1.4 Reverse Flow Warning";
                        type = 22;
                    }
                    else if (el.DryingWarning > 0)
                    {
                        content = "Drying Warning";
                        channelId = $"{loggerid}_105";
                        channelName = "1.5 Drying Warning";
                        type = 23;
                    }
                    else if(el.LowFlowMeterVoltage > 0)
                    {
                        content = "Low Flow Meter Voltage";
                        channelId = $"{loggerid}_106";
                        channelName = "1.6 Low Flow Meter Warning";
                        type = 24;
                    }
                    else if(el.CommunicationError > 0)
                    {
                        content = "Comunication Error";
                        channelId = $"{loggerid}_107";
                        channelName = "1.7 Comms Error";
                        type = 25;
                    }

                    if (content != "") {

                        HistoryAlarmModel alarm = new HistoryAlarmModel();
                        alarm.ChannelId = channelId;
                        alarm.ChannelName = channelName;
                        alarm.SiteId = siteid;
                        alarm.Location = location;
                        alarm.LoggerId = loggerid;
                        alarm.TimeStampAlarm = DateTime.Now;
                        alarm.TimeStampHasValue = DateTime.Now;
                        alarm.Type = type;
                        alarm.Content = content;

                        using(HistoryAlarmAction historyAlarmAction  = new HistoryAlarmAction())
                        {
                            HistoryAlarmModel his = await historyAlarmAction.GetLatestAlarm(alarm.SiteId);

                            bool isInsertAlarm = false;

                            if (his != null)
                            {
                                if (his.TimeStampAlarm != null)
                                {
                                   
                                    if(his.Type != alarm.Type)
                                    {
                                        isInsertAlarm = true;
                                    }
                                    
                                }
                                else
                                {
                                    isInsertAlarm = true;
                                   
                                }
                            }
                            else
                            {
                                isInsertAlarm = true;
                                
                            }

                            if (isInsertAlarm)
                            {
                                alarm.TimeStampAlarm = alarm.TimeStampAlarm.Value.AddHours(8);
                                alarm.TimeStampHasValue = alarm.TimeStampHasValue.Value.AddHours(8);
                                historyAlarmAction.InsertAlarm(alarm);

                                // push notification
                                using (DeviceTokenAppAction deviceTokenAppAction = new DeviceTokenAppAction()) {
                                    List<DeviceTokenAppModel> listToken = await deviceTokenAppAction.GetDeivceTokenApps();
                                    if(listToken.Count > 0)
                                    {
                                        using (NotificationAction notificationAction = new NotificationAction()) {
                                            string contentPush = $"Channel {alarm.ChannelName}  is {alarm.Content}";
                                            await notificationAction.SubmitNotification(alarm.Location, alarm.Location, contentPush, listToken);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // insert alarm for battery 
                    if (battery <= 3.5)
                    {
                        HistoryAlarmModel alarmBattery = new HistoryAlarmModel();
                        alarmBattery.SiteId = siteid;
                        alarmBattery.Location = location;
                        alarmBattery.LoggerId = loggerid;
                        alarmBattery.ChannelId = $"{loggerid}_05";
                        alarmBattery.ChannelName = "7. Battery Logger";
                        alarmBattery.Content = "";
                        alarmBattery.TimeStampHasValue = DateTime.Now.AddHours(8);
                        alarmBattery.TimeStampAlarm = DateTime.Now.AddHours(8);

                        if (battery >= 3.4)
                        {
                            alarmBattery.Content = "Low Battery";
                            alarmBattery.Type = 26;
                        }
                        else
                        {
                            alarmBattery.Content = "Out Of Battery";
                            alarmBattery.Type = 27;
                        }

                        using (HistoryAlarmAction historyAlarmAction = new HistoryAlarmAction())
                        {
                            HistoryAlarmModel his = await historyAlarmAction.GetLatestAlarm(alarmBattery.SiteId);

                            bool isInsertAlarm = false;

                            if (his != null)
                            {
                                if (his.TimeStampAlarm != null)
                                {
                                    //double diff = (alarmBattery.TimeStampAlarm.Value - his.TimeStampAlarm.Value).TotalMinutes;
                                    //if (diff > 60)
                                    //{
                                    //    isInsertAlarm = true;
                                    //}
                                    if(his.Type != alarmBattery.Type)
                                    {
                                        isInsertAlarm = true;
                                    }
                                }
                                else
                                {
                                    isInsertAlarm = true;

                                }
                            }
                            else
                            {
                                isInsertAlarm = true;

                            }

                            if (isInsertAlarm)
                            {
                                //alarmBattery.TimeStampAlarm = alarmBattery.TimeStampAlarm.Value.AddHours(8);
                                //alarmBattery.TimeStampHasValue = alarmBattery.TimeStampHasValue.Value.AddHours(8);
                                historyAlarmAction.InsertAlarm(alarmBattery);

                                // push notification
                                using (DeviceTokenAppAction deviceTokenAppAction = new DeviceTokenAppAction())
                                {
                                    List<DeviceTokenAppModel> listToken = await deviceTokenAppAction.GetDeivceTokenApps();
                                    if (listToken.Count > 0)
                                    {
                                        using (NotificationAction notificationAction = new NotificationAction())
                                        {
                                            string contentPush = $"Channel {alarmBattery.ChannelName} with value: {battery} is {alarmBattery.Content}";
                                            await notificationAction.SubmitNotification(alarmBattery.Location, alarmBattery.Location, contentPush, listToken);
                                        }
                                    }
                                }
                            }

                        }
                    }

                    // insert alarm for signal
                    if (signal < 20)
                    {
                        HistoryAlarmModel alarmSignal = new HistoryAlarmModel();
                        alarmSignal.SiteId = siteid;
                        alarmSignal.Location = location;
                        alarmSignal.LoggerId = loggerid;
                        alarmSignal.ChannelId = $"{loggerid}_07";
                        alarmSignal.ChannelName = "9. Signal";
                        alarmSignal.Content = "Low signal";
                        alarmSignal.TimeStampHasValue = DateTime.Now.AddHours(8);
                        alarmSignal.TimeStampAlarm = DateTime.Now.AddHours(8);
                        alarmSignal.Type = 28;


                        using (HistoryAlarmAction historyAlarmAction = new HistoryAlarmAction())
                        {
                            HistoryAlarmModel his = await historyAlarmAction.GetLatestAlarm(alarmSignal.SiteId);

                            bool isInsertAlarm = false;

                            if (his != null)
                            {
                                if (his.TimeStampAlarm != null)
                                {
                                    //double diff = (alarmSignal.TimeStampAlarm.Value - his.TimeStampAlarm.Value).TotalMinutes;
                                    //if (diff > 60)
                                    //{
                                    //    isInsertAlarm = true;
                                    //}

                                    if (his.Type != alarmSignal.Type)
                                    {
                                        isInsertAlarm = true;
                                    }
                                }
                                else
                                {
                                    isInsertAlarm = true;

                                }
                            }
                            else
                            {
                                isInsertAlarm = true;

                            }

                            if (isInsertAlarm)
                            {
                                //alarmSignal.TimeStampAlarm = alarmSignal.TimeStampAlarm.Value.AddHours(8);
                                //alarmSignal.TimeStampHasValue = alarmSignal.TimeStampHasValue.Value.AddHours(8);
                                historyAlarmAction.InsertAlarm(alarmSignal);

                                // push notification
                                using (DeviceTokenAppAction deviceTokenAppAction = new DeviceTokenAppAction())
                                {
                                    List<DeviceTokenAppModel> listToken = await deviceTokenAppAction.GetDeivceTokenApps();
                                    if (listToken.Count > 0)
                                    {
                                        using (NotificationAction notificationAction = new NotificationAction())
                                        {
                                            string contentPush = $"Channel {alarmSignal.ChannelName} with value: {signal} is {alarmSignal.Content}";
                                            await notificationAction.SubmitNotification(alarmSignal.Location, alarmSignal.Location, contentPush, listToken);
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
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return el;
        }

        public async Task<List<LogSUModel>> AnalyzeDataLog(List<string> logs, int unit)
        {

            List<LogSUModel> list = new List<LogSUModel>();

            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                foreach (string log in logs)
                {
                    LogSUModel el = new LogSUModel();

                    List<string> reg = new List<string>();

                    for (int i = 0; i < log.Length; i += 4)
                    {
                        reg.Add(log.Substring(i, 4));
                    }

                    if (reg.Count > 0)
                    {

                        int year = 0;
                        int month = 0;
                        int day = 0;
                        int hour = 0;
                        int minute = 0;
                        int flowRateReg = 0;
                        double flowRate = 0;

                        year = Convert.ToInt32(reg[0], 16);
                        month = Convert.ToInt32(reg[1].Substring(0, 2), 16);
                        day = Convert.ToInt32(reg[1].Substring(2, 2), 16);
                        hour = Convert.ToInt32(reg[2].Substring(0, 2), 16);
                        minute = Convert.ToInt32(reg[2].Substring(2, 2), 16);

                        flowRateReg = Convert.ToInt32(reg[12], 16);

                        if (flowRateReg == 0)
                        {
                            flowRate = 0.1;
                        }
                        else if (flowRateReg == 1)
                        {
                            flowRate = 0.01;
                        }
                        else if (flowRateReg == 2)
                        {
                            flowRate = 0.001;
                        }
                        else if (flowRateReg == 3)
                        {
                            flowRate = 0.0001;
                        }

                        el.TimeStamp = new DateTime(year, month, day, hour, minute, 0);
                        el.NetIndex = (Convert.ToInt32(reg[3], 16) * Math.Pow(2, 32) + Convert.ToInt32(reg[4], 16) * Math.Pow(2, 16) + Convert.ToInt32(reg[5], 16)) * flowRate;
                        el.ForwardIndex = (Convert.ToInt32(reg[6], 16) * Math.Pow(2, 32) + Convert.ToInt32(reg[7], 16) * Math.Pow(2, 16) + Convert.ToInt32(reg[8], 16)) * flowRate;
                        el.ReverseIndex = (Convert.ToInt32(reg[9], 16) * Math.Pow(2, 32) + Convert.ToInt32(reg[10], 16) * Math.Pow(2, 16) + Convert.ToInt32(reg[11], 16)) * flowRate;

                        if (unit == 3)
                        {
                            el.NetIndex = el.NetIndex * 3600;
                            el.ForwardIndex = el.ForwardIndex * 3600;
                            el.ReverseIndex = el.ReverseIndex * 3600;
                        }

                        el.NetIndex = Math.Round(el.NetIndex, 2);
                        el.ForwardIndex = Math.Round(el.ForwardIndex, 2);
                        el.ReverseIndex = Math.Round(el.ReverseIndex, 2);

                        list.Add(el);

                    }
                }

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return list;
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
                    Console.WriteLine("Disposing analyze data managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~AnalyzeDataAction()
        {
            Dispose(false);
        }
    }
}
