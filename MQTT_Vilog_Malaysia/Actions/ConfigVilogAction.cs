using MongoDB.Bson;
using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class ConfigVilogAction: IDisposable
    {
        private bool disposed = false;
        public async Task<ConfigVilogModel> GetConfigVilog(string siteid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            ConfigVilogModel el = new ConfigVilogModel();
            try
            {

                Connect connect = new Connect();


                var collection = connect.db.GetCollection<ConfigVilogModel>("t_ConfigVilog");

                el = collection.Find(x => x.siteId == siteid && x.isComplete == false).FirstOrDefault();

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return el;
        }

        public async Task<ConfigVilogModel> GetConfigVilogByOldSiteId(string oldSiteId)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            ConfigVilogModel el = new ConfigVilogModel();
            try
            {

                Connect connect = new Connect();


                var collection = connect.db.GetCollection<ConfigVilogModel>("t_ConfigVilog");

                el = collection.Find(x => x.oldSiteId == oldSiteId && x.isComplete == false).FirstOrDefault();

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return el;
        }

        public async void UpdateConfigVilog(string oldSiteId)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            ConfigVilogModel el = new ConfigVilogModel();
            try
            {

                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ConfigVilogModel>("t_ConfigVilog");

                await collection.UpdateOneAsync(
                    Builders<ConfigVilogModel>.Filter.Eq("oldSiteId", oldSiteId),
                    Builders<ConfigVilogModel>.Update.Set("isComplete", true)
                );

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
                    Console.WriteLine("Disposing config vilog managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~ConfigVilogAction()
        {
            Dispose(false);
        }
    }
}
