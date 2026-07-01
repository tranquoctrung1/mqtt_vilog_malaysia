---
name: vilog-debug
description: >
  Systematic debugging guide for MQTT_Vilog_Malaysia pipeline issues. Use when tracing
  MQTT message flow, payload parsing failures, MongoDB write anomalies, duplicate records,
  missing data, meter type misclassification (SU/Kronhe/Level), channel provisioning errors,
  OverTime timestamp drift issues, or worker queue bottlenecks.
  Triggers on: data missing, duplicate data, channel provisioning error, payload parse failure,
  worker bottleneck, "why isn't data coming in", "debug", "trace", "not inserting",
  "wrong values", "data gap", "wrong timestamp", "OverTime", "Level meter not working",
  "channel _200 empty", "meter type wrong".
---

# Vilog Debug — Systematic Pipeline Tracing

Select diagnosis steps based on problem type.

---

## 1. Data Missing from MongoDB

**Check 1: MQTT receive confirmation**
- `Subscribe.cs` — logging inside `Handle_Received_Application_Message()`
- Verify `IpMQTT` and `Port` in `settings.json`
- Set `LogRawPayload: true` → check `./Log/{topic}/` files created

**Check 2: Queue saturation**
- `QueueCapacity` default 20000; `BoundedChannelFullMode.Wait` applies backpressure (blocks, not drops)
- High device count → increase `WorkerCount` in settings.json

**Check 3: Topic parsing**
- Format: `Vilog_{Location}_{LoggerId}_{TYPE}` (split by `_`, index 2 = LoggerId)
- Malformed topic → `SiteAction` lookup fails silently

**Check 4: Site/Channel not provisioned**
- Verify LoggerId in `t_Sites`; check `TypeMeter` field ("SU" | "Kronhe" | "Level")
- Verify channels in `t_Channel_Configurations` (count: SU=11, Kronhe=7, Level=3)
- Auto-provisioning triggers only on first message (site not in `t_Sites`)

---

## 2. Duplicate Data in Collections

**Check 1: TimeStamp unique index**
```javascript
db.getCollection("t_Data_Logger_{channelId}").getIndexes()
// Must have: unique: true on TimeStamp
```

**Check 2: Upsert code (DataLoggerAction.cs)**
- `UpsertByTimeStamp()` uses `UpdateOneModel` with `IsUpsert=true`
- `SetOnInsert(x => x.Id, ObjectId.GenerateNewId())` — prevents ObjectId.Empty duplicate-key error
- In-batch deduplication: `GroupBy(x => x.TimeStamp).Select(g => g.Last())`

**Check 3: MQTT QoS1 re-delivery**
- QoS1 = at-least-once → re-delivery expected; correct upsert prevents duplicates

---

## 3. Meter Type Misclassification

**Detection in `Subscribe.cs:ProcessMessageAsync`:**
```csharp
if (dataObjects.Payload.Length > 50)      // → SU meter
else if (dataObjects.Payload.Length <= 8)  // → Level meter
else                                        // → Kronhe meter (9–50 chars)
```

**Check:** Enable `LogRawPayload: true` → inspect `./Log/` → measure actual payload length.

**Level vs Kronhe boundary:** payload length 1–8 = Level, 9–50 = Kronhe.

---

## 4. OverTime Timestamp Drift

**Trigger condition:** `time.Year > DateTime.Now.Year` — device reports future year.

**Correction formula (Subscribe.cs / HandleDataAction.cs):**
```csharp
DateTime now = DateTime.Now.AddHours(8);
DateTime realtime = realtimeData.TimeStamp.AddHours(8);
double diff = Math.Abs((realtime - timeLog).TotalSeconds);
DateTime realTimeLog = now.AddSeconds(-diff);
realTimeLog = new DateTime(realTimeLog.Year, realTimeLog.Month, realTimeLog.Day,
                           realTimeLog.Hour, item.TimeStamp.Minute, 0);
```

**Check:** If logs have wrong timestamps, verify this correction path is active (OverTime variant called).
**Check:** Duplicate guard uses `GetCurrentTimeStampDataLogger()` — only inserts if `realTimeLog > currentMax`.

---

## 5. Channel Config Cache Issue

- `ChannelConfigAction.cs` — 30s TTL in-memory cache
- Config changes take up to 30s to reflect
- Force invalidation: restart the app

---

## 6. Worker Bottleneck Diagnosis

- Increase `WorkerCount` in settings.json (default: `ProcessorCount × 2`, min 4)
- Increase `QueueCapacity` (default 20000)
- MongoDB connection pool: `MaxConnectionPoolSize: 500` (check `Connect.cs`)
- Add `Stopwatch` inside `ProcessMessageAsync()` to measure per-message time

---

## 7. Level Meter Specific Issues

**Channel not populated (`_200` empty):**
- Verify payload length ≤ 8 (if 9+, routed to Kronhe instead)
- Check `AnalyzeDataAction.AnalyzeDataRealTimeLevelMeter()` parsing
- Check `LogLevelModel.Level` is not null

**Battery/Signal for Level meter:**
- `_05` = Logger Battery (V), `_07` = Signal
- Written alongside level data in `HandleDataLevelMeter()`

---

## 8. Kronhe Timezone Issue

- Kronhe: `+8` hours applied to all timestamps (`realtimeData.TimeStamp.AddHours(8)`)
- Data timestamps off by 8 hours → this is the cause
- Check `HandleDataKronheMeter()` — all `TimeStamp = item.TimeStamp.AddHours(8)`

---

## Quick Diagnostic Commands

```bash
# Build check
dotnet build

# Integration tests (requires MongoDB)
dotnet test --filter "Category=Integration"

# All tests
dotnet test

# Enable raw payload logging (settings.json)
# "LogRawPayload": true  →  JSON files to ./Log/{topic}/
```

---

## Diagnosis Output Format

```markdown
## Root Cause
filename:line_number — exact problem

## Data Flow Trace
1. MQTT receive: [OK / Error]
2. Queue stage: [OK / Saturated]
3. Meter detection: [SU / Kronhe / Level — payload length: N]
4. Parse stage: [OK / Error — reason]
5. DB write stage: [OK / Error — reason]

## Fix Direction
Concrete fix for vilog-dev
```
