using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class HistorySendTimeAction : IDisposable
    {
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

        ~HistorySendTimeAction()
        {
            Dispose(false);
        }

        public async Task InsertHistorySendTime(HistorySendTimeModel his)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<HistorySendTimeModel>("t_historiesSendTime");

                await collection.InsertOneAsync(his);
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }

        public async Task<List<HistorySendTimeModel>> GetHistorySendTime(string loggerid)
        {
            List<HistorySendTimeModel> list = new List<HistorySendTimeModel>();
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<HistorySendTimeModel>("t_historiesSendTime");

                list = await collection.Find(h => h.LoggerId == loggerid)
                    .SortByDescending(h => h.TimeStamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return list;
        }
    }
}
