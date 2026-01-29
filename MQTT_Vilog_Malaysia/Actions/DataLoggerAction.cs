using MongoDB.Bson;
using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class DataLoggerAction : IDisposable
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

        ~DataLoggerAction()
        {
            Dispose(false);
        }

        public async void CreateDataLoggerCollection(string channelid, bool isIndex)
        {
            WriteLogAction writeLogAction = new WriteLogAction();


            try
            {

                Connect connect = new Connect();

                string collectionName = $"t_Data_Logger_{channelid}";
                if (isIndex) {
                    collectionName = $"t_Index_Logger_{channelid}";
                }

                var filter = new BsonDocument(collectionName, collectionName);
                var collections = await connect.db.ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = filter});

                var exists = await collections.AnyAsync();

                if(!exists)
                {
                    await connect.db.CreateCollectionAsync(collectionName);

                    var collection = connect.db.GetCollection<DataLoggerModel>(collectionName);

                    var indexKeys = Builders<DataLoggerModel>.IndexKeys.Ascending("TimeStamp");
                    var indexModel = new CreateIndexModel<DataLoggerModel>(indexKeys);
                    await collection.Indexes.CreateOneAsync(indexModel);
                }
                
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }

        public async Task RenameDataLogger(string oldChannelId, string newChannelId)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                string oldCollectionName = $"t_Data_Logger_{oldChannelId}";
                string newCollectionName = $"t_Data_Logger_{newChannelId}";

                // Check old collection exists
                var filterOld = new BsonDocument("name", oldCollectionName);
                var oldCollections = await connect.db
                    .ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = filterOld });

                if (!await oldCollections.AnyAsync())
                {
                    // Không tồn tại collection cũ → không rename
                    return;
                }

                // Rename collection (dropTarget = true nếu collection mới đã tồn tại)
                await connect.db.RenameCollectionAsync(
                    oldCollectionName,
                    newCollectionName,
                    new RenameCollectionOptions { DropTarget = false }
                );
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.ToString());
            }
        }

        public async Task RenameIndexLogger(string oldChannelId, string newChannelId)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                string oldCollectionName = $"t_Index_Logger_{oldChannelId}";
                string newCollectionName = $"t_Index_Logger_{newChannelId}";

                // Check old collection exists
                var filterOld = new BsonDocument("name", oldCollectionName);
                var oldCollections = await connect.db
                    .ListCollectionNamesAsync(new ListCollectionNamesOptions { Filter = filterOld });

                if (!await oldCollections.AnyAsync())
                {
                    // Không tồn tại collection cũ → không rename
                    return;
                }

                // Rename collection (dropTarget = true nếu collection mới đã tồn tại)
                await connect.db.RenameCollectionAsync(
                    oldCollectionName,
                    newCollectionName,
                    new RenameCollectionOptions { DropTarget = false }
                );
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.ToString());
            }
        }
        public async Task<DateTime?> GetCurrentTimeStampDataLogger(string channelid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            Nullable<DateTime> time = null;

            try
            {

                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>("t_Data_Logger_" + channelid);

                var result = collection.AsQueryable().OrderByDescending(s => s.TimeStamp).FirstOrDefault();

                if (result != null)
                {
                    time = result.TimeStamp;
                }
                else
                {
                    time = null;
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }


            return time;
        }

        public async Task<DateTime?> GetFirstTimeStampDataLogger(string channelid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            Nullable<DateTime> time = null;

            try
            {

                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>("t_Data_Logger_" + channelid);

                var result = collection.AsQueryable().OrderBy(s => s.Id).FirstOrDefault();

                if (result != null)
                {
                    time = result.TimeStamp;
                }
                else
                {
                    time = null;
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }


            return time;
        }

        public async Task<DateTime?> GetCurrentTimeStampIndexLogger(string channelid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            Nullable<DateTime> time = null;

            try
            {

                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>("t_Index_Logger_" + channelid);

                var result = collection.AsQueryable().OrderByDescending(s => s.Id).FirstOrDefault();

                if (result != null)
                {
                    time = result.TimeStamp;
                }
                else
                {
                    time = null;
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }


            return time;
        }

        public async Task<DateTime?> GetFirstTimeStampIndexLogger(string channelid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            Nullable<DateTime> time = null;

            try
            {

                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>("t_Index_Logger_" + channelid);

                var result = collection.AsQueryable().OrderBy(s => s.Id).FirstOrDefault();

                if (result != null)
                {
                    time = result.TimeStamp;
                }
                else
                {
                    time = null;
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }


            return time;
        }

        public async Task<DataLoggerModel> GetLastValueDataLogger(string channelid)
        {
            DataLoggerModel value = new DataLoggerModel();
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>("t_Data_Logger_" + channelid);

                var result = collection.AsQueryable().OrderByDescending(s => s.Id).FirstOrDefault();

                if (result != null)
                {
                    value = result;
                }
                else
                {
                    value = null;
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return value;
        }

        public async Task<DataLoggerModel> GetLastValueIndexLogger(string channelid)
        {
            DataLoggerModel value = new DataLoggerModel();
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>("t_Index_Logger_" + channelid);

                var result = collection.AsQueryable().OrderByDescending(s => s.Id).FirstOrDefault();

                if (result != null)
                {
                    value = result;
                }
                else
                {
                    value = null;
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return value;
        }

        public async Task<int> InsertDataLogger(List<DataLoggerModel> list, string channelid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            int nRows = 0;

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>("t_Data_Logger_" + channelid);

                collection.InsertMany(list);

                nRows = list.Count;
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return nRows;
        }

        public async Task<int> InsertIndexLogger(List<DataLoggerModel> list, string channelid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            int nRows = 0;

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>("t_Index_Logger_" + channelid);

                collection.InsertMany(list);

                nRows = list.Count;
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return nRows;
        }

    }
}
