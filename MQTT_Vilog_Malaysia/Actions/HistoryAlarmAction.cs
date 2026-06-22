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

        // Create the alarm lookup index once per process (idempotent)
        private static bool _indexEnsured = false;
        private static readonly object _indexLock = new object();

        public HistoryAlarmAction()
        {
            if (_indexEnsured) return;
            lock (_indexLock)
            {
                if (_indexEnsured) return;
                try
                {
                    Connect connect = new Connect();
                    var collection = connect.db.GetCollection<HistoryAlarmModel>("t_History_Alarm");
                    var keys = Builders<HistoryAlarmModel>.IndexKeys
                        .Ascending(h => h.SiteId)
                        .Ascending(h => h.Type)
                        .Descending(h => h.TimeStampAlarm);
                    collection.Indexes.CreateOne(new CreateIndexModel<HistoryAlarmModel>(keys));
                }
                catch
                {
                    // Index creation failure must not block alarm processing
                }
                _indexEnsured = true;
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
                list = await collection.Find(_ => true).ToListAsync();

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
                // Server-side sort + limit 1 (uses index) instead of loading all docs into RAM
                el = await collection.Find(h => h.SiteId == siteid)
                    .SortByDescending(h => h.TimeStampAlarm)
                    .Limit(1)
                    .FirstOrDefaultAsync();

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return el;
        }

        // Latest alarm of a specific type for a site — used to de-duplicate per alarm source
        public async Task<HistoryAlarmModel> GetLatestAlarmByType(string siteid, int type)
        {
            HistoryAlarmModel el = null;
            WriteLogAction writeLogAction = new WriteLogAction();
            try
            {
                Connect connect = new Connect();
                var collection = connect.db.GetCollection<HistoryAlarmModel>("t_History_Alarm");
                // Server-side sort + limit 1 (uses index) instead of loading all docs into RAM
                el = await collection.Find(h => h.SiteId == siteid && h.Type == type)
                    .SortByDescending(h => h.TimeStampAlarm)
                    .Limit(1)
                    .FirstOrDefaultAsync();
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
