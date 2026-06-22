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
        // MongoClient is thread-safe and manages its own connection pool.
        // It MUST be a singleton — creating one per request exhausts sockets under load.
        private static readonly MongoClient _client;
        private static readonly IMongoDatabase _db;

        public MongoClient client;

        public IMongoDatabase db;

        private bool disposed = false;

        static Connect()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("settings.json")
               .AddEnvironmentVariables()
               .Build();

            Settings settings = config.GetRequiredSection("Settings").Get<Settings>();

            // Raise the connection pool so many concurrent workers don't queue on the default 100.
            var clientSettings = MongoClientSettings.FromConnectionString(settings.Host);
            clientSettings.MaxConnectionPoolSize = 500;
            clientSettings.MinConnectionPoolSize = 10;

            _client = new MongoClient(clientSettings);
            _db = _client.GetDatabase(settings.DBName);
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
            // Reuse the shared singleton client/database — no new connection pool per instance.
            client = _client;
            db = _db;
        }
    }
}
