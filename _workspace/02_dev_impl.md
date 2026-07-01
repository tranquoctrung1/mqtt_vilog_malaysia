# Level Meter Support — Implementation Summary

Date: 2026-06-28
Build: `dotnet build MQTT_Vilog_Malaysia/MQTT_Vilog_Malaysia.csproj` → 0 Errors, 70 pre-existing Warnings.

## Feature
Adds a new `Level` meter type to the MQTT ingestion pipeline, alongside SU and Kronhe.
Payload format = same JSON as Kronhe, but the realtime/history hex `Payload` is short
(e.g. `"01ffff"`). Parse: drop first 2 chars → hex → int → / 1000.0 → Level (m, 3 dp).

## Discriminator
- New device branch: `Payload.Length <= 8` → Level; else → Kronhe (inside the existing
  `Payload.Length <= 50` block).
- Existing device branch: `site.TypeMeter == "Level"` → Level; else → Kronhe.

## Channels (Level)
- `_200` Level, unit "m", OtherChannel=false
- `_05` Logger Battery, unit "V", BatLoggerChannel=true
- `_07` Signal, unit "-"

TypeMeter stored as `"Level"`.

## Files changed

### MQTT_Vilog_Malaysia/Models/LogLevelModel.cs
No change (already had TimeStamp + Level).

### MQTT_Vilog_Malaysia/Actions/AnalyzeDataAction.cs
Added (before Dispose):
- `LogLevelModel AnalyzeDataRealTimeLevelMeter(string payload, DateTime time)`
- `Task<List<LogLevelModel>> AnalyzeLogDataLevelMeter(Dictionary<string, JsonElement> Log)`

### MQTT_Vilog_Malaysia/Actions/HandleDataAction.cs
Added (before Dispose):
- `Task HandleDataLevelMeter(...)` — realtime + history insert, channel value bulk update,
  inserts battery (_05), signal (_07), and level history (_200).
- `Task HandleDataLevelMeterOverTime(...)` — same but reconstructs timestamps from
  device-vs-realtime offset and dedups against current `_200` timestamp.

### MQTT_Vilog_Malaysia/MQTT/Subscribe.cs
- Change A (new-device, ~line 548): wrapped the Kronhe `else if (Payload.Length <= 50)`
  with `if (Payload.Length <= 8) { Level: insert site TypeMeter="Level" + 3 channels
  (_200/_05/_07) + HandleDataLevelMeter[OverTime] + UpdateUsedForImei } else { existing Kronhe }`.
- Change B (existing-device, ~line 779): wrapped Kronhe handling with
  `if (site.TypeMeter == "Level") { HandleDataLevelMeter[OverTime] + UpdateUsedForImei }
  else { existing Kronhe }`.

## Notes
- Time logic mirrors Kronhe: `DateTime time = DateTime.Parse(dataObjects.time.ToString()).AddHours(8)`;
  `time.Year > DateTime.Now.Year` routes to the OverTime variant.
- All referenced helpers verified to exist: `ChannelConfigAction.BulkUpdateValues`,
  `DataLoggerAction.InsertDataLogger`, `DataLoggerAction.GetCurrentTimeStampDataLogger`.
