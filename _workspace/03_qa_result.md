# QA Result — Level Meter Feature Integration Check

**Date:** 2026-06-28
**Verifier:** vilog-qa
**Build:** `dotnet build MQTT_Vilog_Malaysia/MQTT_Vilog_Malaysia.csproj` → **Build succeeded, 0 Warnings, 0 Errors** (PASS)

## Verdict
Core implementation matches the plan and the discriminator/channel/routing logic is correct. Build is clean. Two functional gaps and one missing-test gap noted below — none block the happy path, but 4.5/4.6 (battery/signal alarms) are unimplemented for Level.

---

## Checklist (10 items)

| # | Item | Result | Evidence |
|---|------|--------|----------|
| 1 | Parse: drop 2 chars → hex → int → /1000.0 → round 3dp | PASS | `AnalyzeDataAction.cs:884-885` (`payload.Substring(2)`, `Math.Round(Convert.ToInt32(hex,16)/1000.0, 3)`) and log variant `:905-906`. Null/empty guarded by `payload != null && payload.Length >= 4`. |
| 2 | `_200` Level, `_05` battery, `_07` signal | PASS | `HandleDataAction.cs:910-912`, `953-955` |
| 3 | New-device routing `Length <= 8` → Level else Kronhe | PASS | `Subscribe.cs:548` (`<= 50` outer) → `:550` (`<= 8` Level) / `:652` else Kronhe |
| 4 | Existing-device `TypeMeter == "Level"` → Level else Kronhe | PASS | `Subscribe.cs:884` (`<= 50` outer) → `:886` Level / `:908` else Kronhe |
| 5 | Site `TypeMeter = "Level"` on provisioning | PASS | `Subscribe.cs:567` |
| 6 | Channels `_200` (unit="m", OtherChannel=false), `_05` (BatLoggerChannel=true), `_07` | PASS | `_200` `:581/:590`; `_05` `:598 Unit="V"`, `:607 BatLoggerChannel=true`; `_07` `:615 Unit="-"` |
| 7 | OverTime: timestamps reconstructed, `GetCurrentTimeStampDataLogger` filter applied | PASS | `HandleDataAction.cs:964-977` — offset from `realtime` vs device time, dedup vs `currentTime.Value.AddHours(7)` (matches Kronhe pattern) |
| 8 | History logs inserted into `_200` | PASS | `HandleDataAction.cs:924` / `:977`. `InsertDataLogger` → `UpsertByTimeStamp` (`DataLoggerAction.cs:406`) gives idempotent upsert (covers plan 4.2 duplicate case) |
| 9 | `UpdateUsedForImei` called in Level paths | PASS | new-device `Subscribe.cs:650`; existing-device `Subscribe.cs:906` |
| 10 | Build passes | PASS | 0 errors / 0 warnings |

---

## Gaps & Findings

### GAP-1 (functional, medium) — No battery/signal alarms for Level meter
Plan edge cases **4.5** (battery <= 3.4 → low-battery alarm) and **4.6** (signal > 0 && < 20 → low-signal alarm) are **NOT implemented** for the Level path. The Kronhe/SU realtime analyzers raise these alarms inline (`AnalyzeDataRealTimeHronheMeter` lines 160-312; SU 630-781), but `AnalyzeDataRealTimeLevelMeter` (`AnalyzeDataAction.cs:878-888`) only parses the value — it raises no alarms, and `HandleDataLevelMeter[OverTime]` never invokes any alarm logic. Level devices will therefore silently fail to produce low-battery / low-signal alarms. Recommend porting the battery/signal alarm blocks (Types 16/18 style) into the Level path if alarm parity is required.

### GAP-2 (test coverage) — Unit tests from plan §1 & §2 not written
The plan defines xUnit tests for `AnalyzeDataRealTimeLevelMeter` (§1: 1.1–1.8) and `AnalyzeLogDataLevelMeter` (§2: 2.1–2.6) and exit criteria §6 require them, but no Level test file exists in `MQTT_Vilog_Malaysia.Tests` (only Notification/HistorySendTime/Pipeline/Diag tests present). The parse methods are pure and directly unit-testable as written (`AnalyzeDataRealTimeLevelMeter` is synchronous, no DB) — the plan's note (line 39) to extract a shared `ConvertHexToDoubleAction`-style helper was not done, but is not required for testability. Tests should be added to satisfy exit criteria.

### NOTE-1 (consistency, low) — Realtime-only message does not write a `_200` history row
Plan scenario **3.2** expects `_200` to receive a doc at the message TimeStamp for a realtime `"01ffff"`. In the implementation the realtime Level value only updates the channel-config last value via `BulkUpdateValues`; only the `Log` history array is inserted into the `t_Data_Logger_*_200` collection (`HandleDataAction.cs:919-925`). A message with no history entries will not create a `_200` data row. This mirrors the existing Kronhe behavior exactly (realtime flow → channel config only; history → data logger), so it is consistent with the codebase, but reviewers expecting 3.2 literally should be aware. Battery/signal realtime values ARE written to `_05`/`_07` data loggers (`:916-917`).

### Verified OK
- Length boundary (plan 4.4): `<= 8` Level vs `> 8` Kronhe correct at new-device branch.
- Null/empty/short payload (plan 4.7 / 1.5 / 1.6): guarded by length check + try/catch → `WriteErrorLog`; no throw, no bad row.
- Duplicate upsert (plan 4.2): `UpsertByTimeStamp` keyed by TimeStamp → single row.
- OverTime future-year routing (plan 4.3): `time.Year > DateTime.Now.Year` routes to `...OverTime` variant (`Subscribe.cs:638`, `:894`).
- Max value (plan 4.1): `0xffff = 65535 / 1000 = 65.535`, fits int32, `Math.Round(...,3)` → 65.535.

---

## Recommendation
Ship-able for the happy path (build clean, routing/parse/provisioning correct). Before sign-off on full plan: (a) decide whether GAP-1 alarm parity is in-scope — if yes, implement; (b) add the §1/§2 unit tests to meet exit criteria §6.
