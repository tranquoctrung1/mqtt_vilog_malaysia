using MQTT_Vilog_Malaysia.Actions;
using MQTT_Vilog_Malaysia.Models;
using MongoDB.Driver;
using MQTT_Vilog_Malaysia.ConnectDB;
using System.Text.Json;

namespace MQTT_Vilog_Malaysia.Tests;

/// <summary>
/// Full pipeline integration test using real topic/payload from note.txt.
/// Verifies: HistorySendTime insert, Site provisioning, Channel provisioning, SU meter data insert.
/// Run: dotnet test --filter "Category=Integration"
/// Requires: MongoDB running on localhost:27017
/// </summary>
public class PipelineIntegrationTests
{
    // Data from note.txt
    private const string Topic = "Vilog_AIS25EM00136_25069_PUB";
    private const string LoggerId = "25069";
    private const string Location = "AIS25EM00136";
    private const int Signal = 31;
    private const double Battery = 3.662;
    private const string RawPayload = @"{""IMEI"":""868977089654727"",""IMSI"":""502161082149724"",""Model"":""RS485-CB"",""Payload"":""01000000000004000000000287138e0000028ac90200000003b57400020000000007ea030f070000000287138e0000028ac90200000003b57400020000000107ea030f0100000002844ff400000288056700000003b57300020000000107ea030e1300000002810d3400000284c1f000000003b4bc00020000000107ea030e0d00000002810d3400000284c1f000000003b4bc00020000000107ea030e0700000002810d3400000284c1f000000003b4bc00020000000107ea030e01000000027f83410000028337fc00000003b4bb00020000000107ea030d13000000027c275e0000027fdb8500000003b42700020000000107ea030d0d000000027c275e0000027fdb8500000003b42700020000000107ea030d07000000027c27070000027fdb2400000003b41d00020000000107ea030d01000000027c27070000027fdb2400000003b41d00020000000107ea030c13000000027c140b0000027fc82800000003b41d00020000000107ea030c0d0000000278a3330000027c575000000003b41d00020000000107ea030c070000000276afc20000027a63de00000003b41c00020000000107ea030c010000000276afc20000027a63de00000003b41c000200000001"",""interrupt"":0,""interrupt_level"":0,""battery"":3.662,""signal"":31,""time"":""2026-03-16T18:29:08Z"",""latitude"":0.000000,""longitude"":0.000000,""gps_time"":""1970-01-01T00:00:00Z""}";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Step1_HistorySendTime_IsInserted()
    {
        var his = new HistorySendTimeModel
        {
            SiteId = LoggerId,
            Location = Location,
            LoggerId = LoggerId,
            TimeStamp = DateTime.Now
        };

        using var action = new HistorySendTimeAction();
        await action.InsertHistorySendTime(his);

        using var verify = new HistorySendTimeAction();
        var list = await verify.GetHistorySendTime(LoggerId);

        Console.WriteLine($"[Step1] t_historiesSendTime records for {LoggerId}: {list.Count}");
        Assert.True(list.Count > 0, "No record in t_historiesSendTime after insert.");
        Assert.Equal(LoggerId, list[0].LoggerId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Step2_Site_IsProvisionedOrAlreadyExists()
    {
        using var siteAction = new SiteAction();
        var existing = await siteAction.GetSite(LoggerId);

        if (existing.Count == 0)
        {
            var site = new SiteModel
            {
                SiteId = LoggerId,
                IMEI = "868977089654727",
                Location = Location,
                LoggerId = LoggerId,
                Longitude = 0,
                Latitude = 0,
                DisplayGroup = "Vilog",
                StartHour = 0,
                StartDay = 1,
                IsDisplay = true,
                InterVal = 5,
                TypeMeter = "SU"
            };
            await siteAction.InsertSite(site);
            Console.WriteLine($"[Step2] Site {LoggerId} provisioned (new).");
        }
        else
        {
            Console.WriteLine($"[Step2] Site {LoggerId} already exists. TypeMeter={existing[0].TypeMeter}");
        }

        using var verify = new SiteAction();
        var list = await verify.GetSite(LoggerId);
        Assert.True(list.Count > 0, "Site not found in t_Sites.");
        Assert.Equal("SU", list[0].TypeMeter);
        Assert.Equal(LoggerId, list[0].LoggerId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Step3_Channels_AreProvisionedOrAlreadyExist()
    {
        using var channelAction = new ChannelConfigAction();
        var existing = await channelAction.GetChannelByLoggerId(LoggerId);

        if (existing.Count == 0)
        {
            var channels = BuildSUChannels(LoggerId);
            await channelAction.InsertChannelConfigsBulk(channels);
            Console.WriteLine($"[Step3] Provisioned {channels.Count} channels for {LoggerId}.");
        }
        else
        {
            Console.WriteLine($"[Step3] {existing.Count} channels already exist for {LoggerId}.");
        }

        using var verify = new ChannelConfigAction();
        var list = await verify.GetChannelByLoggerId(LoggerId);
        Console.WriteLine($"[Step3] Total channels in DB: {list.Count}");
        Assert.True(list.Count >= 14, $"Expected at least 14 channels for SU meter, got {list.Count}.");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Step4_SUMeterData_IsInsertedFromPayload()
    {
        // Ensure site + channels exist first
        await EnsureSiteAndChannels();

        var payload = JsonSerializer.Deserialize<PayloadMQTTModel>(RawPayload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(payload);
        Console.WriteLine($"[Step4] Payload length: {payload.Payload.Length} chars (expect >50 for SU)");
        Assert.True(payload.Payload.Length > 50, "Payload too short to be SU meter.");
        Assert.True(payload.Payload.Length >= 66, "Payload too short to extract realTimeString.");

        string realTimeString = payload.Payload.Substring(0, 66);
        var logChunks = new List<string>();
        for (int i = 66; i + 60 <= payload.Payload.Length; i += 60)
            logChunks.Add(payload.Payload.Substring(i, 60));

        Console.WriteLine($"[Step4] realTimeString={realTimeString.Substring(0, 10)}... logChunks={logChunks.Count}");

        using var handleDataAction = new HandleDataAction();
        await handleDataAction.HandleDataSUMeter(
            realTimeString, logChunks,
            LoggerId, LoggerId, Location,
            Signal, Battery);

        Console.WriteLine("[Step4] HandleDataSUMeter completed.");

        // Verify data was inserted in at least one t_Data_Logger_ collection
        var db = new Connect().db;
        var collectionNames = (await db.ListCollectionNamesAsync()).ToList();
        var dataCollections = collectionNames.Where(n => n.StartsWith($"t_Data_Logger_{LoggerId}")).ToList();
        var indexCollections = collectionNames.Where(n => n.StartsWith($"t_Index_Logger_{LoggerId}")).ToList();

        Console.WriteLine($"[Step4] t_Data_Logger_ collections: {string.Join(", ", dataCollections)}");
        Console.WriteLine($"[Step4] t_Index_Logger_ collections: {string.Join(", ", indexCollections)}");

        Assert.True(dataCollections.Count > 0, "No t_Data_Logger_ collections found for this logger.");

        long totalRecords = 0;
        foreach (var colName in dataCollections)
        {
            var col = db.GetCollection<MongoDB.Bson.BsonDocument>(colName);
            long count = await col.CountDocumentsAsync(MongoDB.Driver.FilterDefinition<MongoDB.Bson.BsonDocument>.Empty);
            Console.WriteLine($"[Step4]   {colName}: {count} records");
            totalRecords += count;
        }

        Assert.True(totalRecords > 0, "No records inserted into any t_Data_Logger_ collection.");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Step5_ChannelConfig_LastValue_IsUpdated()
    {
        // Ensure full pipeline has run (Steps 1-4 prerequisites)
        await EnsureSiteAndChannels();

        var payload = JsonSerializer.Deserialize<PayloadMQTTModel>(RawPayload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        string realTimeString = payload!.Payload.Substring(0, 66);
        var logChunks = new List<string>();
        for (int i = 66; i + 60 <= payload.Payload.Length; i += 60)
            logChunks.Add(payload.Payload.Substring(i, 60));

        using var handleDataAction = new HandleDataAction();
        await handleDataAction.HandleDataSUMeter(
            realTimeString, logChunks,
            LoggerId, LoggerId, Location,
            Signal, Battery);

        // Query MongoDB directly (bypass cache) to verify LastValue was written
        var db = new Connect().db;
        var col = db.GetCollection<ChannelConfigModel>("t_Channel_Configurations");
        var channels = await col.Find(c => c.LoggerId == LoggerId).ToListAsync();

        var forwardFlow = channels.FirstOrDefault(c => c.ForwardFlow == true);
        Console.WriteLine($"[Step5] ForwardFlow channel: {forwardFlow?.ChannelId}, LastValue={forwardFlow?.LastValue}, TimeStamp={forwardFlow?.TimeStamp}");

        Assert.NotNull(forwardFlow);
        Assert.NotNull(forwardFlow!.LastValue);
        Assert.NotNull(forwardFlow.TimeStamp);
    }

    // --- Helpers ---

    private async Task EnsureSiteAndChannels()
    {
        using var siteAction = new SiteAction();
        var existing = await siteAction.GetSite(LoggerId);
        if (existing.Count == 0)
        {
            await siteAction.InsertSite(new SiteModel
            {
                SiteId = LoggerId, IMEI = "868977089654727", Location = Location,
                LoggerId = LoggerId, Longitude = 0, Latitude = 0, DisplayGroup = "Vilog",
                StartHour = 0, StartDay = 1, IsDisplay = true, InterVal = 5, TypeMeter = "SU"
            });
        }

        using var channelAction = new ChannelConfigAction();
        var existingCh = await channelAction.GetChannelByLoggerId(LoggerId);
        if (existingCh.Count == 0)
            await channelAction.InsertChannelConfigsBulk(BuildSUChannels(LoggerId));
    }

    private static List<ChannelConfigModel> BuildSUChannels(string loggerId)
    {
        ChannelConfigModel Ch(string id, string name, string unit,
            bool fwd = false, bool rev = false, bool other = false) => new()
        {
            ChannelId = $"{loggerId}_{id}",
            ChannelName = name, LoggerId = loggerId, Unit = unit,
            ForwardFlow = fwd, ReverseFlow = rev, OtherChannel = other,
            Pressure1 = false, Pressure2 = false,
            TimeStamp = null, LastValue = null, IndexTimeStamp = null, LastIndex = null
        };

        return new List<ChannelConfigModel>
        {
            Ch("02",  "2.1 Forward Flow",           "m3/h", fwd: true),
            Ch("03",  "2.2 Reverse Flow",            "m3/h", rev: true),
            Ch("98",  "2.3 Forward Totalizer",       "m3"),
            Ch("99",  "2.4 Reverse Totalizer",       "m3"),
            Ch("100", "1.1 Power Supply Down",       "-",  other: true),
            Ch("101", "1.2 Memmory Error",           "-",  other: true),
            Ch("103", "1.3 Low Transmitter Voltage", "-",  other: true),
            Ch("104", "1.4 Reverse Flow Warning",    "-",  other: true),
            Ch("105", "1.5 Drying Warning",          "-",  other: true),
            Ch("106", "1.6 Low Flow Meter Warning",  "-",  other: true),
            Ch("107", "1.7 Comms Error",             "-",  other: true),
            Ch("108", "2.5 Net Totalizer",           "m3"),
            Ch("109", "2.6 Signal",                  "-"),
            Ch("110", "2.7 Battery",                 "V"),
        };
    }
}
