using MQTT_Vilog_Malaysia.ConnectDB;
using MQTT_Vilog_Malaysia.Actions;
using MQTT_Vilog_Malaysia.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Text.Json;

namespace MQTT_Vilog_Malaysia.Tests;
public class DiagTests
{
    private const string LoggerId = "25069";
    private const string Location = "AIS25EM00136";
    private const int Signal = 31;
    private const double Battery = 3.662;
    private const string RawPayload = @"{""IMEI"":""868977089654727"",""IMSI"":""502161082149724"",""Model"":""RS485-CB"",""Payload"":""01000000000004000000000287138e0000028ac90200000003b57400020000000007ea030f070000000287138e0000028ac90200000003b57400020000000107ea030f0100000002844ff400000288056700000003b57300020000000107ea030e1300000002810d3400000284c1f000000003b4bc00020000000107ea030e0d00000002810d3400000284c1f000000003b4bc00020000000107ea030e0700000002810d3400000284c1f000000003b4bc00020000000107ea030e01000000027f83410000028337fc00000003b4bb00020000000107ea030d13000000027c275e0000027fdb8500000003b42700020000000107ea030d0d000000027c275e0000027fdb8500000003b42700020000000107ea030d07000000027c27070000027fdb2400000003b41d00020000000107ea030d01000000027c27070000027fdb2400000003b41d00020000000107ea030c13000000027c140b0000027fc82800000003b41d00020000000107ea030c0d0000000278a3330000027c575000000003b41d00020000000107ea030c070000000276afc20000027a63de00000003b41c00020000000107ea030c010000000276afc20000027a63de00000003b41c000200000001"",""interrupt"":0,""interrupt_level"":0,""battery"":3.662,""signal"":31,""time"":""2026-03-16T18:29:08Z"",""latitude"":0.000000,""longitude"":0.000000,""gps_time"":""1970-01-01T00:00:00Z""}";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PrintCollectionCounts()
    {
        var db = new Connect().db;
        var names = (await db.ListCollectionNamesAsync()).ToList();
        var target = names.Where(n => n.Contains("25069")).OrderBy(n => n).ToList();
        Console.WriteLine("=== Collections for 25069 ===");
        foreach (var name in target)
        {
            var col = db.GetCollection<BsonDocument>(name);
            long count = await col.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            Console.WriteLine($"{name}: {count}");
        }
    }

    /// <summary>
    /// Deep trace: drop all 25069 logger data, run HandleDataSUMeter once, count results.
    /// Prints AnalyzeDataLog count, GetLastValueIndexLogger result, and final DB record counts.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Trace_SUMeter_LogInsertCount()
    {
        var db = new Connect().db;

        // 1. Drop all t_Data_Logger_25069_* and t_Index_Logger_25069_* collections for clean state
        var allNames = (await db.ListCollectionNamesAsync()).ToList();
        foreach (var name in allNames.Where(n => n.Contains("25069") &&
            (n.StartsWith("t_Data_Logger_") || n.StartsWith("t_Index_Logger_"))))
        {
            await db.DropCollectionAsync(name);
            Console.WriteLine($"[Trace] Dropped: {name}");
        }

        // 2. Ensure site + channels
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
        {
            await channelAction.InsertChannelConfigsBulk(BuildSUChannels(LoggerId));
        }

        // 3. Parse payload
        var payload = JsonSerializer.Deserialize<PayloadMQTTModel>(RawPayload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        string realTimeString = payload!.Payload.Substring(0, 66);
        var logChunks = new List<string>();
        for (int i = 66; i + 60 <= payload.Payload.Length; i += 60)
            logChunks.Add(payload.Payload.Substring(i, 60));

        Console.WriteLine($"[Trace] logChunks.Count = {logChunks.Count}");

        // 4. Probe AnalyzeDataLog directly
        using var analyzeAction = new AnalyzeDataAction();
        var logList = await analyzeAction.AnalyzeDataLog(logChunks, 0);
        Console.WriteLine($"[Trace] AnalyzeDataLog returned {logList.Count} items");
        for (int i = 0; i < logList.Count; i++)
            Console.WriteLine($"[Trace]   list[{i}]: TimeStamp={logList[i].TimeStamp}, FwdIdx={logList[i].ForwardIndex}");

        // 5. Probe GetLastValueIndexLogger before HandleDataSUMeter
        using var dataLoggerAction = new DataLoggerAction();
        var lastIndex = await dataLoggerAction.GetLastValueIndexLogger($"{LoggerId}_02");
        if (lastIndex == null)
            Console.WriteLine("[Trace] GetLastValueIndexLogger: null (empty collection)");
        else
            Console.WriteLine($"[Trace] GetLastValueIndexLogger: TimeStamp={lastIndex.TimeStamp}, Value={lastIndex.Value}");

        // 6. Run HandleDataSUMeter
        using var handleDataAction = new HandleDataAction();
        await handleDataAction.HandleDataSUMeter(
            realTimeString, logChunks,
            LoggerId, LoggerId, Location,
            Signal, Battery);

        Console.WriteLine("[Trace] HandleDataSUMeter complete.");

        // 7. Count records in each relevant collection
        var newNames = (await db.ListCollectionNamesAsync()).ToList();
        var target = newNames.Where(n => n.Contains("25069")).OrderBy(n => n).ToList();
        Console.WriteLine("[Trace] === Final collection counts ===");
        foreach (var name in target)
        {
            var col = db.GetCollection<BsonDocument>(name);
            long count = await col.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            Console.WriteLine($"[Trace] {name}: {count}");
        }
    }

    /// <summary>
    /// Kronhe meter trace: drop all 25121 data, run HandleDataKronheMeter once, count results.
    /// 12 log entries expected → 12 records per flow/totalizer channel.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Trace_KronheMeter_LogInsertCount()
    {
        const string KronheLoggerId = "25121";
        const string KronheLocation = "AIS22EM00069";
        const double KronheBattery = 3.661;
        const int KronheSignal = 23;
        const string KronheRawPayload = @"{""IMEI"":""868977089863252"",""IMSI"":""502161082177816"",""Model"":""RS485-CB"",""Payload"":""0140ca31c746a2865346a2865e3cbcdff00000000042124dab"",""interrupt"":0,""interrupt_level"":0,""battery"":3.661,""signal"":23,""time"":""2026-06-27T18:05:14Z"",""latitude"":0.000000,""longitude"":0.000000,""gps_time"":""1970-01-01T00:00:00Z"",""1"":[""0140cc7c2646a285c246a285ce3cbcdff00000000042124dab"",""2026-06-27T18:02:24Z""],""2"":[""0140d9d41246a2785746a278633cbcdff00000000042124e68"",""2026-06-27T17:02:24Z""],""3"":[""0140d114ec46a26b6746a26b733cbcdff00000000042124f25"",""2026-06-27T16:02:24Z""],""4"":[""0140c10ba846a25e8f46a25e9b3cbcdff00000000042124fe3"",""2026-06-27T15:02:24Z""],""5"":[""0140ebd87246a2517046a2517c3cbcdff000000000421250a3"",""2026-06-27T14:02:24Z""],""6"":[""0140c3edae46a2452746a245333cbcdff00000000042125164"",""2026-06-27T13:02:24Z""],""7"":[""014105935646a236ae46a236ba3cbcdff00000000042125227"",""2026-06-27T12:02:24Z""],""8"":[""0140fdb4d646a227bd46a227c93cbcdff000000000421252ed"",""2026-06-27T11:02:24Z""],""9"":[""0140f233f646a217e246a217ed3cbcdff000000000421253b4"",""2026-06-27T10:02:24Z""],""10"":[""0140fc26fb46a2078846a207943cbcdff0000000004212547b"",""2026-06-27T09:02:24Z""],""11"":[""0140cc9a4d46a1fa0446a1fa0f3cbcdff00000000042125543"",""2026-06-27T08:02:24Z""],""12"":[""0140a6f0ee46a1ed8246a1ed8e3cbcdff0000000004212560a"",""2026-06-27T07:02:24Z""]}";

        var db = new Connect().db;

        // 1. Drop all data/index collections for this logger
        var allNames = (await db.ListCollectionNamesAsync()).ToList();
        foreach (var name in allNames.Where(n => n.Contains(KronheLoggerId) &&
            (n.StartsWith("t_Data_Logger_") || n.StartsWith("t_Index_Logger_"))))
        {
            await db.DropCollectionAsync(name);
            Console.WriteLine($"[Kronhe] Dropped: {name}");
        }

        // 2. Ensure site + channels (9 Kronhe channels)
        using var siteAction = new SiteAction();
        var existingSite = await siteAction.GetSite(KronheLoggerId);
        if (existingSite.Count == 0)
        {
            await siteAction.InsertSite(new SiteModel
            {
                SiteId = KronheLoggerId, IMEI = "868977089863252", Location = KronheLocation,
                LoggerId = KronheLoggerId, Longitude = 0, Latitude = 0, DisplayGroup = "Vilog",
                StartHour = 0, StartDay = 1, IsDisplay = true, InterVal = 5, TypeMeter = "Kronhe"
            });
        }
        using var channelAction = new ChannelConfigAction();
        var existingCh = await channelAction.GetChannelByLoggerId(KronheLoggerId);
        if (existingCh.Count == 0)
        {
            await channelAction.InsertChannelConfigsBulk(BuildKronheChannels(KronheLoggerId));
        }
        Console.WriteLine($"[Kronhe] Channels: {(await channelAction.GetChannelByLoggerId(KronheLoggerId)).Count}");

        // 3. Parse payload
        var payload = JsonSerializer.Deserialize<PayloadMQTTModel>(KronheRawPayload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(payload);
        Console.WriteLine($"[Kronhe] Payload length: {payload.Payload.Length} (≤50 = Kronhe)");
        Assert.True(payload.Payload.Length <= 50);
        Console.WriteLine($"[Kronhe] Log entries (AdditionalData): {payload.AdditionalData?.Count}");

        // 4. Probe AnalyzeLogDataHronheMeter
        using var analyzeAction = new AnalyzeDataAction();
        var logList = await analyzeAction.AnalyzeLogDataHronheMeter(payload.AdditionalData);
        Console.WriteLine($"[Kronhe] AnalyzeLogDataHronheMeter returned {logList.Count} items");
        foreach (var item in logList)
            Console.WriteLine($"[Kronhe]   TimeStamp={item.TimeStamp}, FwdFlow={item.ForwardFlow:F3}, FwdIdx={item.ForwardIndex}");

        // 5. Run HandleDataKronheMeter
        DateTime time = payload.time.AddHours(8);
        Console.WriteLine($"[Kronhe] time={time} (year>{DateTime.Now.Year}? {time.Year > DateTime.Now.Year})");

        using var handleDataAction = new HandleDataAction();
        await handleDataAction.HandleDataKronheMeter(
            payload.Payload, payload.AdditionalData, time,
            KronheLoggerId, KronheBattery, KronheLoggerId, KronheLocation, KronheSignal);

        Console.WriteLine("[Kronhe] HandleDataKronheMeter complete.");

        // 6. Count final records
        var newNames = (await db.ListCollectionNamesAsync()).ToList();
        var target = newNames.Where(n => n.Contains(KronheLoggerId)).OrderBy(n => n).ToList();
        Console.WriteLine("[Kronhe] === Final collection counts ===");
        long total = 0;
        foreach (var name in target)
        {
            var col = db.GetCollection<BsonDocument>(name);
            long count = await col.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            Console.WriteLine($"[Kronhe] {name}: {count}");
            total += count;
        }
        Console.WriteLine($"[Kronhe] Total records: {total}");
        Assert.True(total > 0, "No records inserted.");
    }

    // ─── Level Meter ────────────────────────────────────────────────────────────

    /// <summary>
    /// Unit tests for Level meter parse logic (no DB required).
    /// Payload "01ffff": drop 2 chars → "ffff" → 65535 → /1000 = 65.535 m.
    /// </summary>
    [Fact]
    public async Task LevelMeter_ParseRealtime_ReturnsCorrectLevel()
    {
        using var analyze = new AnalyzeDataAction();
        var time = new DateTime(2026, 4, 17, 4, 23, 59, DateTimeKind.Utc);

        var result = await analyze.AnalyzeDataRealTimeLevelMeter("01ffff", time, "s1", "loc", "lg1", 25, 3.7);

        Assert.Equal(65.535, result.Level);
        Assert.Equal(time, result.TimeStamp);
    }

    [Fact]
    public async Task LevelMeter_ParseRealtime_ZeroLevel()
    {
        using var analyze = new AnalyzeDataAction();
        var result = await analyze.AnalyzeDataRealTimeLevelMeter("010000", DateTime.UtcNow, "s1", "loc", "lg1", 25, 3.7);
        Assert.Equal(0.0, result.Level);
    }

    [Fact]
    public async Task LevelMeter_ParseRealtime_SmallestNonZero()
    {
        using var analyze = new AnalyzeDataAction();
        var result = await analyze.AnalyzeDataRealTimeLevelMeter("010001", DateTime.UtcNow, "s1", "loc", "lg1", 25, 3.7);
        Assert.Equal(0.001, result.Level);
    }

    [Fact]
    public async Task LevelMeter_ParseLog_10Entries_AllCorrect()
    {
        using var analyze = new AnalyzeDataAction();
        var payload = JsonSerializer.Deserialize<PayloadMQTTModel>(LevelRawPayload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(payload);

        var logs = await analyze.AnalyzeLogDataLevelMeter(payload.AdditionalData);
        Assert.Equal(10, logs.Count);
        Assert.All(logs, l => Assert.Equal(65.535, l.Level));
    }

    /// <summary>
    /// Integration: drop LVTEST001 data, run HandleDataLevelMeter, verify records in _200/_05/_07.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Trace_LevelMeter_LogInsertCount()
    {
        const string LvLoggerId = "LVTEST001";
        const string LvLocation = "VILOGLEVELTEST";
        const double LvBattery = 3.654;
        const int LvSignal = 26;

        var db = new Connect().db;

        // 1. Drop all data collections for this logger
        var allNames = (await db.ListCollectionNamesAsync()).ToList();
        foreach (var name in allNames.Where(n => n.Contains(LvLoggerId) &&
            (n.StartsWith("t_Data_Logger_") || n.StartsWith("t_Index_Logger_"))))
        {
            await db.DropCollectionAsync(name);
            Console.WriteLine($"[Level] Dropped: {name}");
        }

        // 2. Ensure site + channels
        using var siteAction = new SiteAction();
        var existingSite = await siteAction.GetSite(LvLoggerId);
        if (existingSite.Count == 0)
        {
            await siteAction.InsertSite(new SiteModel
            {
                SiteId = LvLoggerId, IMEI = "860813071516152", Location = LvLocation,
                LoggerId = LvLoggerId, Longitude = 0, Latitude = 0, DisplayGroup = "Vilog",
                StartHour = 0, StartDay = 1, IsDisplay = true, InterVal = 5, TypeMeter = "Level"
            });
        }
        using var channelAction = new ChannelConfigAction();
        var existingCh = await channelAction.GetChannelByLoggerId(LvLoggerId);
        if (existingCh.Count == 0)
        {
            await channelAction.InsertChannelConfigsBulk(BuildLevelChannels(LvLoggerId));
        }
        Console.WriteLine($"[Level] Channels: {(await channelAction.GetChannelByLoggerId(LvLoggerId)).Count}");

        // 3. Parse payload
        var payload = JsonSerializer.Deserialize<PayloadMQTTModel>(LevelRawPayload,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(payload);
        Console.WriteLine($"[Level] Payload: '{payload.Payload}' length={payload.Payload.Length} (≤8 = Level)");
        Assert.True(payload.Payload.Length <= 8);
        Console.WriteLine($"[Level] Log entries (AdditionalData): {payload.AdditionalData?.Count}");

        // 4. Probe AnalyzeDataRealTimeLevelMeter
        using var analyzeAction = new AnalyzeDataAction();
        DateTime time = payload.time.AddHours(8);
        var rt = await analyzeAction.AnalyzeDataRealTimeLevelMeter(payload.Payload, time, LvLoggerId, LvLocation, LvLoggerId, LvSignal, LvBattery);
        Console.WriteLine($"[Level] Realtime Level={rt.Level} m, TimeStamp={rt.TimeStamp}");
        Assert.Equal(65.535, rt.Level);

        // 5. Probe AnalyzeLogDataLevelMeter
        var logList = await analyzeAction.AnalyzeLogDataLevelMeter(payload.AdditionalData);
        Console.WriteLine($"[Level] AnalyzeLogDataLevelMeter returned {logList.Count} items");
        foreach (var item in logList)
            Console.WriteLine($"[Level]   TimeStamp={item.TimeStamp}, Level={item.Level}");
        Assert.Equal(10, logList.Count);

        // 6. Run HandleDataLevelMeter
        Console.WriteLine($"[Level] time={time} (year>{DateTime.Now.Year}? {time.Year > DateTime.Now.Year})");
        using var handleDataAction = new HandleDataAction();
        if (time.Year > DateTime.Now.Year)
            await handleDataAction.HandleDataLevelMeterOverTime(payload.Payload, payload.AdditionalData, time, LvLoggerId, LvBattery, LvLoggerId, LvLocation, LvSignal);
        else
            await handleDataAction.HandleDataLevelMeter(payload.Payload, payload.AdditionalData, time, LvLoggerId, LvBattery, LvLoggerId, LvLocation, LvSignal);
        Console.WriteLine("[Level] HandleDataLevelMeter complete.");

        // 7. Count final records
        var newNames = (await db.ListCollectionNamesAsync()).ToList();
        var target = newNames.Where(n => n.Contains(LvLoggerId)).OrderBy(n => n).ToList();
        Console.WriteLine("[Level] === Final collection counts ===");
        long total = 0;
        foreach (var name in target)
        {
            var col = db.GetCollection<BsonDocument>(name);
            long count = await col.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            Console.WriteLine($"[Level] {name}: {count}");
            total += count;
        }
        Console.WriteLine($"[Level] Total records: {total}");
        Assert.True(total > 0, "No records inserted.");

        // Verify _200 has 10 history + possibly 1 realtime (upsert) → at least 1
        var levelCol = db.GetCollection<BsonDocument>($"t_Data_Logger_{LvLoggerId}_200");
        long levelCount = await levelCol.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Console.WriteLine($"[Level] t_Data_Logger_{LvLoggerId}_200 count={levelCount}");
        Assert.True(levelCount >= 10, $"Expected ≥10 level records, got {levelCount}");
    }

    // Payload from image: Vilog_VILOGLEVELTEST_001_PUB
    // Realtime "01ffff" → Level = 65.535 m, 10 history entries
    private const string LevelRawPayload = @"{""IMEI"":""860813071516152"",""IMSI"":""502197701825780"",""Model"":""RS485-CB"",""Payload"":""01ffff"",""interrupt"":0,""interrupt_level"":0,""battery"":3.654,""signal"":26,""time"":""2026-04-17T04:23:59Z"",""latitude"":0,""longitude"":0,""gps_time"":""1970-01-01T00:00:00Z"",""1"":[""01ffff"",""2026-04-17T04:21:55Z""],""2"":[""01ffff"",""2026-04-17T04:16:55Z""],""3"":[""01ffff"",""2026-04-17T04:11:55Z""],""4"":[""01ffff"",""2026-04-17T04:06:55Z""],""5"":[""01ffff"",""2026-04-17T04:01:55Z""],""6"":[""01ffff"",""2026-04-17T03:56:55Z""],""7"":[""01ffff"",""2026-04-17T03:51:55Z""],""8"":[""01ffff"",""2026-04-17T03:46:55Z""],""9"":[""01ffff"",""2026-04-17T03:41:55Z""],""10"":[""01ffff"",""2026-04-17T03:36:55Z""]}";

    private static List<ChannelConfigModel> BuildLevelChannels(string loggerId)
    {
        ChannelConfigModel Ch(string id, string name, string unit,
            bool batLogger = false) => new()
        {
            ChannelId = $"{loggerId}_{id}",
            ChannelName = name, LoggerId = loggerId, Unit = unit,
            ForwardFlow = false, ReverseFlow = false, OtherChannel = false,
            BatLoggerChannel = batLogger,
            Pressure1 = false, Pressure2 = false,
            TimeStamp = null, LastValue = null, IndexTimeStamp = null, LastIndex = null
        };

        return new List<ChannelConfigModel>
        {
            Ch("200", "1. Level",          "m"),
            Ch("05",  "2. Logger Battery", "V", batLogger: true),
            Ch("07",  "3. Signal",         "-"),
        };
    }

    private static List<ChannelConfigModel> BuildKronheChannels(string loggerId)
    {
        ChannelConfigModel Ch(string id, string name, string unit,
            bool fwd = false, bool rev = false, bool other = false,
            bool batLogger = false) => new()
        {
            ChannelId = $"{loggerId}_{id}",
            ChannelName = name, LoggerId = loggerId, Unit = unit,
            ForwardFlow = fwd, ReverseFlow = rev, OtherChannel = other,
            BatLoggerChannel = batLogger,
            Pressure1 = false, Pressure2 = false,
            TimeStamp = null, LastValue = null, IndexTimeStamp = null, LastIndex = null
        };

        return new List<ChannelConfigModel>
        {
            Ch("02",  "1. Forward Flow",         "m3/h", fwd: true),
            Ch("03",  "2. Reverse Flow",          "m3/h", rev: true),
            Ch("98",  "3. Forward Totalizer",     "m3"),
            Ch("99",  "4. Reverse Totalizer",     "m3"),
            Ch("100", "5. Net Totalizer",         "m3"),
            Ch("101", "6. Alarm",                 "-",  other: true),
            Ch("05",  "7. Logger Battery",        "V",  batLogger: true),
            Ch("06",  "8. Meter Battery Capacity","Ah"),
            Ch("07",  "9. Signal",                "-"),
        };
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
