using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Models;
using System;
using System.Collections.Concurrent;
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

        // Process-wide cache of channel config per loggerid. Channel config changes rarely,
        // but is read on every incoming message — caching cuts one DB query per message.
        private static readonly ConcurrentDictionary<string, (DateTime cachedAt, List<ChannelConfigModel> list)> _channelCache
            = new ConcurrentDictionary<string, (DateTime, List<ChannelConfigModel>)>();
        private static readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(30);

        private static void InvalidateChannelCache(string loggerid)
        {
            if (!string.IsNullOrEmpty(loggerid))
            {
                _channelCache.TryRemove(loggerid, out _);
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

            // Serve from cache if fresh
            if (_channelCache.TryGetValue(loggerid, out var cached)
                && (DateTime.UtcNow - cached.cachedAt) < _cacheTtl)
            {
                return cached.list;
            }

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");

                list = await collection.Find(c => c.LoggerId == loggerid).ToListAsync();

                if (list.Count > 0)
                {
                    _channelCache[loggerid] = (DateTime.UtcNow, list);
                }
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

        public async Task InsertChannelConfig(ChannelConfigModel channelConfig)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");

                // Atomic insert-if-absent: avoids check-then-insert race between concurrent message threads
                var filter = Builders<ChannelConfigModel>.Filter.Eq(c => c.ChannelId, channelConfig.ChannelId);
                var update = Builders<ChannelConfigModel>.Update
                    .SetOnInsert(c => c.ChannelId, channelConfig.ChannelId)
                    .SetOnInsert(c => c.LoggerId, channelConfig.LoggerId)
                    .SetOnInsert(c => c.ChannelName, channelConfig.ChannelName)
                    .SetOnInsert(c => c.Unit, channelConfig.Unit)
                    .SetOnInsert(c => c.Pressure1, channelConfig.Pressure1)
                    .SetOnInsert(c => c.Pressure2, channelConfig.Pressure2)
                    .SetOnInsert(c => c.ForwardFlow, channelConfig.ForwardFlow)
                    .SetOnInsert(c => c.ReverseFlow, channelConfig.ReverseFlow)
                    .SetOnInsert(c => c.IndexTimeStamp, channelConfig.IndexTimeStamp)
                    .SetOnInsert(c => c.LastIndex, channelConfig.LastIndex)
                    .SetOnInsert(c => c.LastValue, channelConfig.LastValue)
                    .SetOnInsert(c => c.TimeStamp, channelConfig.TimeStamp)
                    .SetOnInsert(c => c.OtherChannel, channelConfig.OtherChannel)
                    .SetOnInsert(c => c.BatMetterChannel, channelConfig.BatMetterChannel)
                    .SetOnInsert(c => c.BatLoggerChannel, channelConfig.BatLoggerChannel);

                await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });

                InvalidateChannelCache(channelConfig.LoggerId);
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

        }

        // Bulk upsert many channels in one round-trip (provisioning a new device).
        // Uses SetOnInsert so existing channels are never overwritten (race-safe).
        public async Task InsertChannelConfigsBulk(List<ChannelConfigModel> channels)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            if (channels == null || channels.Count == 0) return;

            try
            {
                Connect connect = new Connect();
                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");

                var models = new List<WriteModel<ChannelConfigModel>>();
                foreach (var c in channels)
                {
                    var filter = Builders<ChannelConfigModel>.Filter.Eq(x => x.ChannelId, c.ChannelId);
                    var update = Builders<ChannelConfigModel>.Update
                        .SetOnInsert(x => x.ChannelId, c.ChannelId)
                        .SetOnInsert(x => x.LoggerId, c.LoggerId)
                        .SetOnInsert(x => x.ChannelName, c.ChannelName)
                        .SetOnInsert(x => x.Unit, c.Unit)
                        .SetOnInsert(x => x.Pressure1, c.Pressure1)
                        .SetOnInsert(x => x.Pressure2, c.Pressure2)
                        .SetOnInsert(x => x.ForwardFlow, c.ForwardFlow)
                        .SetOnInsert(x => x.ReverseFlow, c.ReverseFlow)
                        .SetOnInsert(x => x.IndexTimeStamp, c.IndexTimeStamp)
                        .SetOnInsert(x => x.LastIndex, c.LastIndex)
                        .SetOnInsert(x => x.LastValue, c.LastValue)
                        .SetOnInsert(x => x.TimeStamp, c.TimeStamp)
                        .SetOnInsert(x => x.OtherChannel, c.OtherChannel)
                        .SetOnInsert(x => x.BatMetterChannel, c.BatMetterChannel)
                        .SetOnInsert(x => x.BatLoggerChannel, c.BatLoggerChannel);

                    models.Add(new UpdateOneModel<ChannelConfigModel>(filter, update) { IsUpsert = true });
                }

                await collection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false });

                foreach (var c in channels) InvalidateChannelCache(c.LoggerId);
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }
        }

        // One round-trip for many channel value/index updates (instead of N separate UpdateOne).
        public async Task BulkUpdateValues(List<(string channelId, DataLoggerModel value, bool isIndex)> updates)
        {
            WriteLogAction writeLogAction = new WriteLogAction();

            if (updates == null || updates.Count == 0) return;

            try
            {
                Connect connect = new Connect();
                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");

                var models = new List<WriteModel<ChannelConfigModel>>();
                foreach (var u in updates)
                {
                    var filter = Builders<ChannelConfigModel>.Filter.Eq("ChannelId", u.channelId);
                    var update = u.isIndex
                        ? Builders<ChannelConfigModel>.Update.Set("IndexTimeStamp", u.value.TimeStamp).Set("LastIndex", u.value.Value)
                        : Builders<ChannelConfigModel>.Update.Set("TimeStamp", u.value.TimeStamp).Set("LastValue", u.value.Value);
                    models.Add(new UpdateOneModel<ChannelConfigModel>(filter, update));
                }

                await collection.BulkWriteAsync(models, new BulkWriteOptions { IsOrdered = false });
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

        public async Task UpdateChannelConfigVilog(ConfigVilogModel site)
        {
            WriteLogAction writeLogAction = new WriteLogAction();
            DataLoggerAction dataLoggerAction = new DataLoggerAction();

            try
            {
                Connect connect = new Connect();

                var collection = connect.db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");

                List<ChannelConfigModel> channels = await GetChannelByLoggerId(site.oldSiteId);

                // Rename moves channels from oldSiteId to siteId — invalidate both cache keys
                InvalidateChannelCache(site.oldSiteId);
                InvalidateChannelCache(site.siteId);

                if (channels.Count > 0)
                {
                    foreach (var channel in channels)
                    {
                        if (channel.ChannelId != "")
                        {

                            string[] channelSplit = channel.ChannelId.Split("_");
                            string channelNumber = channelSplit.Length > 1 ? channelSplit[channelSplit.Length - 1] : "";


                            var filter = Builders<ChannelConfigModel>.Filter.Eq("ChannelId", channel.ChannelId);
                            var update = Builders<ChannelConfigModel>.Update
                            .Set(x => x.ChannelId, $"{site.siteId}_{channelNumber}")
                            .Set(x => x.LoggerId, site.siteId);

                            await collection.UpdateOneAsync(filter, update);
                            await dataLoggerAction.RenameDataLogger(channel.ChannelId, $"{site.siteId}_{channelNumber}");
                            await dataLoggerAction.RenameIndexLogger(channel.ChannelId, $"{site.siteId}_{channelNumber}");
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                await writeLogAction.WriteErrorLog(ex.Message);
            }

        }
    }
}
