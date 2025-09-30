using MongoDB.Bson;
using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class HistoryAlarmAction : IDisposable
    {
        private bool disposed = false;
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
                    Console.WriteLine("Disposing data logger managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~HistoryAlarmAction()
        {
            Dispose(false);
        }

        public async Task<List<HistoryAlarmModel>> GetAlarms()
        {
            List<HistoryAlarmModel> list = new List<HistoryAlarmModel>();
            WriteLogAction writeLogAction = new WriteLogAction();
            try
            {

                Connect connect = new Connect();
                var collection = connect.db.GetCollection<HistoryAlarmModel>("t_History_Alarm");
                list = collection.FindAsync(_ => true).Result.ToList();
                
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return list;
        }

        public async Task<HistoryAlarmModel> GetLatestAlarm(string siteid)
        {
            HistoryAlarmModel el = new HistoryAlarmModel();
            WriteLogAction writeLogAction = new WriteLogAction();
            try
            {

                Connect connect = new Connect();
                var collection = connect.db.GetCollection<HistoryAlarmModel>("t_History_Alarm");
                el = collection.FindAsync(h => h.SiteId == siteid).Result.ToList().OrderByDescending(h => h.TimeStampAlarm).FirstOrDefault();

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return el;
        }

        public async void InsertAlarm(HistoryAlarmModel his)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<HistoryAlarmModel>("t_History_Alarm");

                await collection.InsertOneAsync(his);
                

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }
    }
}
