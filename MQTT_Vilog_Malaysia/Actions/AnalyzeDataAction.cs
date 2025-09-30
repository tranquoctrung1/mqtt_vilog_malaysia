﻿using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class AnalyzeDataAction : IDisposable
    {
        private bool disposed = false;

        public async Task<LogKronheModel> AnalyzeDataRealTimeHronheMeter(string realTimeString, DateTime time, string siteid, string location, string loggerid)
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
                        alarm.TimeStampHasValue = DateTime.Now.AddHours(7);
                        alarm.TimeStampAlarm = DateTime.Now.AddHours(7);
                        

                        if (log.Alarm == 1)
                        {
                            alarm.Content = "Active flow meansurement warning condition";
                            alarm.Type = 10;
                        }
                        else if (log.Alarm == 2)
                        {
                            alarm.Content = "Active < 10% battery warning condition";
                            alarm.Type = 11;
                        }
                        else if (log.Alarm == 4)
                        {
                            alarm.Content = "Active EEPROM error condition";
                            alarm.Type = 12;
                        }
                        else if (log.Alarm == 8)
                        {
                            alarm.Content = "Active communication error condition";
                            alarm.Type = 13;
                        }
                        else if (log.Alarm == 16)
                        {
                            alarm.Content = "Empty Pipe";
                            alarm.Type = 14;
                        }
                        else if (log.Alarm == 32)
                        {
                            alarm.Content = "Main power failure";
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
                                    if (diff > 60)
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
                                alarm.TimeStampAlarm = alarm.TimeStampAlarm.Value.AddHours(7);
                                alarm.TimeStampHasValue = alarm.TimeStampAlarm.Value.AddHours(7);
                                historyAlarmAction.InsertAlarm(alarm);
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
                    //time = time.AddHours(7);

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

        public async Task<RealTimeModel> AnalyzeDataRealTimeSUMeter(string realTimeString, string siteid, string location, string loggerid)
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

                    int status = int.Parse(reg[reg.Count - 1]);

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
                        content = "Modbus power supply down";
                        channelId = $"{loggerid}_100";
                        channelName = "1.1 Power Supply Down";
                        type = 16;

                    }
                    else if (el.MemoryError > 0)
                    {
                        content = "Memory error";
                        channelId = $"{loggerid}_101";
                        channelName = "1.2 Mem error";
                        type = 17;
                    }
                    else if (el.LowTransmitterVoltage > 0)
                    {
                        content = "Low trasmitter voltage";
                        channelId = $"{loggerid}_103";
                        channelName = "1.3 Low Transmitter voltage";
                        type = 18;
                    }
                    else if (el.ReverseFlowWarning > 0)
                    {
                        content = "Reverse flow waring";
                        channelId = $"{loggerid}_104";
                        channelName = "1.4 Reverse Flow warning";
                        type = 19;
                    }
                    else if (el.DryingWarning > 0)
                    {
                        content = "Drying waring";
                        channelId = $"{loggerid}_105";
                        channelName = "1.5 Drying warning";
                        type = 20;
                    }
                    else if(el.LowFlowMeterVoltage > 0)
                    {
                        content = "Low flow meter voltage";
                        channelId = $"{loggerid}_106";
                        channelName = "1.6 Low Flow Meter warning";
                        type = 21;
                    }
                    else if(el.CommunicationError > 0)
                    {
                        content = "Comunication error";
                        channelId = $"{loggerid}_107";
                        channelName = "1.7 Com error";
                        type = 22;
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
                                    double diff = (alarm.TimeStampAlarm.Value - his.TimeStampAlarm.Value).TotalMinutes;
                                    if (diff > 60)
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
                                alarm.TimeStampAlarm = alarm.TimeStampAlarm.Value.AddHours(7);
                                alarm.TimeStampHasValue = alarm.TimeStampAlarm.Value.AddHours(7);
                                historyAlarmAction.InsertAlarm(alarm);
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
