using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class SiteAction : IDisposable
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
                    Console.WriteLine("Disposing site managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~SiteAction()
        {
            Dispose(false);
        }
        public async Task<List<SiteModel>> GetSite()
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            List<SiteModel> list = new List<SiteModel>();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<SiteModel>("t_Sites");

                list = collection.Find(s => s.LoggerId != "").ToList();
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }


            return list;
        }

        public async Task<List<SiteModel>> GetSite(string loggerid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            List<SiteModel> list = new List<SiteModel>();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<SiteModel>("t_Sites");

                list = collection.Find(s => s.LoggerId  == loggerid).ToList();
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }


            return list;
        }

        public async void InsertSite(SiteModel site)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            int nRow = 0;

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<SiteModel>("t_Sites");

                List<SiteModel> check =  collection.FindAsync(s => s.SiteId == site.SiteId).Result.ToList();

                if (check.Count <= 0) { 
                    await collection.InsertOneAsync(site);
                }

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }
        public async void UpdateSite(ConfigVilogModel site)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<SiteModel>("t_Sites");

                var filter = Builders<SiteModel>.Filter.And(
                    Builders<SiteModel>.Filter.Eq(x => x.SiteId, site.oldSiteId),
                    Builders<SiteModel>.Filter.Eq(x => x.Location, site.oldLocation)
                );

                var update = Builders<SiteModel>.Update
                    .Set(x => x.SiteId, site.siteId)
                    .Set(x => x.Location, site.location);

                await collection.UpdateOneAsync(
                   filter, update
                );

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }
    }
}
