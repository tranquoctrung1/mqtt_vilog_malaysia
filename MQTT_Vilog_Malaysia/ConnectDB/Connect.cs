using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConfigClass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.ConnectDB
{
    public class Connect : IDisposable
    {
        public MongoClient client;

        public IMongoDatabase db;

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
                    Console.WriteLine("Disposing connect managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~Connect()
        {
            Dispose(false);
        }


        public Connect()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("settings.json")
               .AddEnvironmentVariables()
               .Build();

            Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

            client = new MongoClient(settings.Host);

            db = client.GetDatabase(settings.DBName);
        }
    }
}
