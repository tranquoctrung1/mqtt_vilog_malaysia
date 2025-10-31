using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class DeviceTokenAppAction : IDisposable
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
                    Console.WriteLine("Disposing device token app...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~DeviceTokenAppAction()
        {

            Dispose(false);
        }

        public async Task<List<DeviceTokenAppModel>> GetDeivceTokenApps()
        {
            List<DeviceTokenAppModel> list = new List<DeviceTokenAppModel>();
            WriteLogAction writeLogAction = new WriteLogAction();
            try
            {

                Connect connect = new Connect();
                var collection = connect.db.GetCollection<DeviceTokenAppModel>("DeviceTokenApp");
                list = collection.FindAsync(_ => true).Result.ToList();

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return list;
        }
    }
}
