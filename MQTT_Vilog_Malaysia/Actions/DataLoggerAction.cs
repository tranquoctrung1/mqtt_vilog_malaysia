using MongoDB.Bson;
using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MQTT_Vilog_Malaysia.Actions
{
    public class DataLoggerAction : IDisposable
    {

        private bool disposed = false;

        // Remember collections we've already ensured (index created) this process,
        // so repeat messages skip the round-trip entirely.
        private static readonly ConcurrentDictionary<string, bool> _ensuredCollections
            = new ConcurrentDictionary<string, bool>();
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

        public async Task CreateDataLoggerCollection(string channelid, bool isIndex)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            string collectionName = isIndex ? $"t_Index_Logger_{channelid}" : $"t_Data_Logger_{channelid}";

            // Already ensured this process -> skip (no DB round-trip)
            if (_ensuredCollections.ContainsKey(collectionName))
            {
                return;
            }

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<DataLoggerModel>(collectionName);

                // CreateOne auto-creates the collection if missing and is idempotent if the
                // index already exists. Removes the expensive ListCollectionNames + CreateCollection.
                var indexKeys = Builders<DataLoggerModel>.IndexKeys.Ascending("TimeStamp");
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<DataLoggerModel>(
                    indexKeys, new CreateIndexOptions { Unique = true }));

                _ensuredCollections[collectionName] = true;
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

        // Ensure the TimeStamp index exists for a collection, once per process (cached).
        // Insert auto-creates the collection; this adds the index lazily on first write.
        private async Task EnsureIndex(IMongoCollection<DataLoggerModel> collection, string collectionName)
        {
            if (_ensuredCollections.ContainsKey(collectionName)) return;
            try
            {
                var keys = Builders<DataLoggerModel>.IndexKeys.Ascending("TimeStamp");
                await collection.Indexes.CreateOneAsync(new CreateIndexModel<DataLoggerModel>(
                    keys, new CreateIndexOptions { Unique = true }));
            }
            catch { /* index race / already exists - ignore */ }
            _ensuredCollections[collectionName] = true;
        }

        // Upsert keyed on TimeStamp so re-delivered/duplicate messages (MQTT QoS1
        // at-least-once redelivery, device resending its log buffer) overwrite the
        // existing row instead of inserting a duplicate document.
        private async Task<int> UpsertByTimeStamp(IMongoCollection<DataLoggerModel> collection, List<DataLoggerModel> list)
        {
            if (list == null || list.Count == 0) return 0;

            // De-dupe within the same batch too: keep the last value per TimeStamp,
            // since BulkWrite cannot upsert the same key twice in one call.
            var byTimeStamp = list
                .Where(x => x.TimeStamp.HasValue)
                .GroupBy(x => x.TimeStamp.Value)
                .Select(g => g.Last());

            // Use UpdateOneModel ($set + $setOnInsert) instead of ReplaceOneModel so that
            // upsert-inserts get a freshly generated _id rather than ObjectId.Empty.
            // ReplaceOneModel with IsUpsert=true embeds the model's _id in the inserted doc —
            // all new DataLoggerModel instances share _id=ObjectId.Empty, causing a duplicate-key
            // error on _id for every insert after the first one.
            var models = byTimeStamp.Select(item =>
            {
                var filter = Builders<DataLoggerModel>.Filter.Eq(x => x.TimeStamp, item.TimeStamp);
                var update = Builders<DataLoggerModel>.Update
                    .SetOnInsert(x => x.Id, ObjectId.GenerateNewId())
                    .SetOnInsert(x => x.TimeStamp, item.TimeStamp)
                    .Set(x => x.Value, item.Value);
                return (WriteModel<DataLoggerModel>)new UpdateOneModel<DataLoggerModel>(filter, update) { IsUpsert = true };
            }).ToList();

            if (models.Count == 0) return 0;

            var result = await collection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false });
            return (int)(result.InsertedCount + result.Upserts.Count + result.ModifiedCount);
        }

        public async Task<int> InsertDataLogger(List<DataLoggerModel> list, string channelid)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            int nRows = 0;

            try
            {
                Connect connect = new Connect();

                string name = "t_Data_Logger_" + channelid;
                var collection = connect.db.GetCollection<DataLoggerModel>(name);
                await EnsureIndex(collection, name);

                nRows = await UpsertByTimeStamp(collection, list);
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

                string name = "t_Index_Logger_" + channelid;
                var collection = connect.db.GetCollection<DataLoggerModel>(name);
                await EnsureIndex(collection, name);

                nRows = await UpsertByTimeStamp(collection, list);
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

            return nRows;
        }

    }
}
