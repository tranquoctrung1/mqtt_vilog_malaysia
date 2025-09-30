using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class ChannelConfigAction: IDisposable
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
                    Console.WriteLine("Disposing channel managed resources...");
                }

                // Giải phóng unmanaged resources ở đây (nếu có)

                disposed = true;
            }
        }

        ~ChannelConfigAction()
        {
            Dispose(false);
        }


        public async Task<List<ChannelConfigModel>> GetChannelByLoggerId(string loggerid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            List<ChannelConfigModel> list = new List<ChannelConfigModel>();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");

                list = collection.Find(c => c.LoggerId == loggerid).ToList();
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return list;

        }

        public async Task<List<ChannelConfigModel>> GetChannels()
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            List<ChannelConfigModel> list = new List<ChannelConfigModel>();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");

                list = collection.Find(_ => true).ToList();
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return list;

        }

        public async void InsertChannelConfig(ChannelConfigModel channelConfig)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");

                List<ChannelConfigModel> check = collection.FindAsync(c => c.ChannelId == channelConfig.ChannelId).Result.ToList();

                if(check.Count <= 0 )
                {
                    await collection.InsertOneAsync(channelConfig);
                }

            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

        }

        public async Task<long> UpdateValueAction(string channelid, DataLoggerModel values)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            long nRows = 0;

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");


                var filter = Builders<ChannelConfigModel>.Filter.Eq("ChannelId", channelid);

                var update = Builders<ChannelConfigModel>.Update.Set("TimeStamp", values.TimeStamp)
                    .Set("LastValue", values.Value);

                var result = collection.UpdateOne(filter, update);

                nRows = result.ModifiedCount;
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return nRows;
        }

        public async Task<long> UpdateIndexValueAction(string channelid, DataLoggerModel values)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            long nRows = 0;

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");


                var filter = Builders<ChannelConfigModel>.Filter.Eq("ChannelId", channelid);

                var update = Builders<ChannelConfigModel>.Update
               .Set("IndexTimeStamp", values.TimeStamp)
               .Set("LastIndex", values.Value);

                var result = collection.UpdateOne(filter, update);

                nRows = result.ModifiedCount;
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return nRows;
        }
    }
}
