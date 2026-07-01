# QA Test Plan — Level Meter Feature

**Project:** MQTT_Vilog_Malaysia (Vilog Malaysia MQTT pipeline)
**Feature under test:** Level meter ingestion (`TypeMeter = "Level"`)
**Target methods (new):** `AnalyzeDataAction.AnalyzeDataRealTimeLevelMeter`, `AnalyzeDataAction.AnalyzeLogDataLevelMeter`
**Test project:** `MQTT_Vilog_Malaysia.Tests` (xUnit — `[Fact]`, `Assert.*`)
**Date:** 2026-06-28

---

## 0. Context & Conventions

### Parse rule (single sample)
```
raw "01ffff"  ->  drop first 2 chars ("01")  ->  "ffff"
            ->  Convert.ToInt32("ffff", 16) = 65535
            ->  65535 / 1000.0 = 65.535  (Level, metres)
```
- Payload string is 6 chars (1 status byte + 2 value bytes, hex-encoded).
- Realtime field is the top-level value string; history entries are `["<6charhex>", "<ISO8601 UTC timestamp>"]`.
- Battery and signal arrive as standard JSON top-level fields (same handling as Kronhe/SU).

### Channel mapping (assert in integration tests)
| Channel suffix | Meaning | Unit |
|----------------|---------|------|
| `_200` | Level | m |
| `_05`  | Battery | V |
| `_07`  | Signal | - |

### Routing rule
- **New device:** `Payload.Length <= 8` => Level meter (set `TypeMeter = "Level"`).
- **Existing device:** `site.TypeMeter == "Level"` => Level meter path.

### Test patterns to follow (from `NotificationActionTests.cs`)
- One assertion-focused behavior per `[Fact]`; descriptive `Method_Condition_Expected` names.
- Use a `Testable<Action>` subclass that overrides external I/O (HTTP / Mongo / file) so unit tests stay pure. The Level-meter parse methods should be made unit-testable the same way the notification path was (no live MongoDB / no network for pure-parse tests).
- For floating point, assert with tolerance: `Assert.Equal(expected, actual, 3)` (3 decimal places) to avoid binary-rounding flakiness.

> NOTE for implementer: keep the hex->double conversion in a small pure helper (mirroring `ConvertHexToDoubleAction`) so it can be unit tested directly without DB side effects. Reuse the existing `ConvertHexToDoubleAction` style.

---

## 1. Unit tests — `AnalyzeDataRealTimeLevelMeter`

Pure parse of the realtime value string into a Level value. No DB writes expected on the parse assertions.

| # | Test name | Input payload | Expected Level (m) | Notes |
|---|-----------|---------------|--------------------|-------|
| 1.1 | `RealTimeLevel_MaxValue_Returns65_535` | `"01ffff"` | `65.535` | 0xffff = 65535 / 1000 |
| 1.2 | `RealTimeLevel_Zero_Returns0` | `"010000"` | `0.0` | lower bound |
| 1.3 | `RealTimeLevel_One_Returns0_001` | `"010001"` | `0.001` | smallest non-zero step |
| 1.4 | `RealTimeLevel_9999_Returns9_999` | `"01270f"` | `9.999` | 0x270f = 9999 / 1000 |
| 1.5 | `RealTimeLevel_NullPayload_NoException` | `null` | no throw; returns default/empty model (Level unset or 0) | must be swallowed like other parse paths (try/catch -> WriteErrorLog) |
| 1.6 | `RealTimeLevel_EmptyPayload_NoException` | `""` | no throw; returns default | empty string edge |

Assertions:
- For 1.1–1.4: `Assert.Equal(expected, model.Level, 3)`.
- For 1.5–1.6: wrap call in `Assert.Null(Record.Exception(...))` (or `await` equivalent); assert the returned model is non-null and Level defaults (0 / unset). Confirm no DB call attempted.

Additional defensive cases (optional but recommended):
- 1.7 `RealTimeLevel_StatusBytePreserved_OnlyValueParsed` — e.g. `"FFffff"` still yields `65.535` (first 2 chars dropped regardless of status byte).
- 1.8 `RealTimeLevel_LowercaseUppercaseHex_Equivalent` — `"01ABcd"` == `"01abCD"` parse equally (hex case-insensitive).

---

## 2. Unit tests — `AnalyzeLogDataLevelMeter`

Parses history dictionary/list where each entry = `["<6charhex>", "<ISO timestamp>"]` into a list of Level log models. Mirror structure of `AnalyzeLogDataHronheMeter` (skips entries with `data.Count < 2`, parses `data[1]` to UTC).

| # | Test name | Input | Expected |
|---|-----------|-------|----------|
| 2.1 | `LogLevel_SingleEntry_ParsesValueAndTime` | one entry `["01ffff","2026-04-17T04:21:55Z"]` | list count 1; Level = 65.535; TimeStamp = 2026-04-17 04:21:55 UTC |
| 2.2 | `LogLevel_MultipleEntries_ParsesAll` | three entries (`"010001"`, `"01270f"`, `"01ffff"` with distinct timestamps) | list count 3; values `0.001`, `9.999`, `65.535` in order; timestamps preserved |
| 2.3 | `LogLevel_EntryWithCountLessThan2_Skipped` | mix of valid entry + entry missing timestamp (`["01ffff"]`) | only valid entries returned; short entry skipped, no throw |
| 2.4 | `LogLevel_Timestamp_ParsedToUtc` | entry with `"2026-04-17T04:21:55Z"` | `TimeStamp.Kind == DateTimeKind.Utc`; equals `DateTime.Parse(...).ToUniversalTime()` |
| 2.5 | `LogLevel_EmptyDictionary_ReturnsEmptyList` | empty input | empty list; no throw |
| 2.6 | `LogLevel_MalformedEntry_NoExceptionPropagates` | entry with non-hex value or null | swallowed via try/catch -> WriteErrorLog; method returns partial/empty list |

Assertions:
- Value: `Assert.Equal(expected, log.Level, 3)`.
- Time: `Assert.Equal(DateTimeKind.Utc, log.TimeStamp.Kind)` and exact equality against parsed UTC.
- Count assertions via `Assert.Equal(n, result.Count)`.

---

## 3. Integration test scenarios (manual / describe-only)

Require a live (or test) MongoDB and the full message-handling path. Document and execute manually; do not run in the pure-unit suite.

| # | Scenario | Setup | Expected outcome |
|---|----------|-------|------------------|
| 3.1 | New device with Level payload routes to Level path | Unknown device; realtime payload length <= 8 (e.g. `"01ffff"`) | Device provisioned with `TypeMeter = "Level"`; processed by Level path, NOT Kronhe/SU. No Kronhe register-split (which needs >= 6 regs of 8 chars). |
| 3.2 | Level value lands in `_200` | Send `"01ffff"` for logger `<loggerid>` | `t_Data_Logger_<loggerid>_200` gets a doc with value `65.535` at the message TimeStamp. |
| 3.3 | Battery lands in `_05` | Top-level battery field present | `t_Data_Logger_<loggerid>_05` gets the battery (V) value. |
| 3.4 | Signal lands in `_07` | Top-level signal field present | `t_Data_Logger_<loggerid>_07` gets the signal value. |
| 3.5 | Existing device TypeMeter=Level routes correctly | Pre-existing `site.TypeMeter == "Level"`, any valid payload | Routed to Level path by site type (not by length heuristic); value to `_200`. |
| 3.6 | Channel provisioning | First-ever message for a new Level logger | `_200`, `_05`, `_07` channels/collections auto-created (matching existing provisioning behavior for other meter types). |
| 3.7 | History batch ingestion | Payload carries history array of N Level entries | N rows inserted into `t_Data_Logger_<loggerid>_200`, each at its own parsed UTC timestamp; index collection `t_Index_Logger_*` consistent (no dup — see ea7b2b8 fix). |
| 3.8 | Overtime handling adjusts timestamps | Message/history with timestamps requiring the OverTime path | Timestamps adjusted per existing OverTime logic before insert; values unchanged. |

Verification queries (per logger):
- `db.t_Data_Logger_<loggerid>_200.find().sort({TimeStamp:1})` — confirm Level values & count.
- `db.t_Data_Logger_<loggerid>_05.find()` / `_07.find()` — battery / signal.
- Confirm no rows written to any Kronhe/SU-specific collection for the Level device.

---

## 4. Edge cases

| # | Case | Input / Trigger | Expected |
|---|------|-----------------|----------|
| 4.1 | Max value boundary | `"01ffff"` -> 65535 | `65.535` exactly; no overflow (fits int32). |
| 4.2 | Duplicate message upsert | Same Level reading + same TimeStamp sent twice | Upsert keyed by TimeStamp -> single row in `_200` (no duplicate). Aligns with commit ea7b2b8 "prevent duplicate inserts". |
| 4.3 | Future-year timestamp -> OverTime | History/realtime timestamp in a future year | Routed through OverTime adjustment path; row still inserted with corrected timestamp, no crash. |
| 4.4 | Routing length boundary | Payload length exactly 8 vs 9 | `<= 8` => Level; `> 8` => other meter type. Verify off-by-one at the `8` boundary. |
| 4.5 | Battery alarm interaction | Level device with `battery <= 3.4` | Low-battery alarm/log raised consistent with existing rule (battery < 3.4 logs announcement; see commit e119e64) on `_05`. |
| 4.6 | Signal alarm interaction | `signal > 0 && signal < 20` | Low-signal alarm path fires on `_07` as for other meters. |
| 4.7 | Short/garbage payload | `"01"`, `"0"`, non-hex `"01zzzz"` | No exception; error logged via `WriteErrorLog`; no bad row inserted. |

---

## 5. Test data quick reference

| Hex (raw) | Value bytes | Int | Level (m) |
|-----------|-------------|-----|-----------|
| `01ffff` | `ffff` | 65535 | 65.535 |
| `010000` | `0000` | 0 | 0.000 |
| `010001` | `0001` | 1 | 0.001 |
| `01270f` | `270f` | 9999 | 9.999 |

History sample: `["01ffff", "2026-04-17T04:21:55Z"]` -> Level 65.535 @ 2026-04-17T04:21:55Z (UTC).

---

## 6. Coverage / exit criteria

- [ ] All Section 1 unit tests pass (4 value cases + null/empty no-throw).
- [ ] All Section 2 unit tests pass (single, multiple, skip-short, UTC parse).
- [ ] Float assertions use 3-decimal tolerance.
- [ ] No unit test touches live MongoDB / network (I/O overridden via Testable subclass).
- [ ] Section 3 integration scenarios executed manually against test DB and signed off.
- [ ] Section 4 edge cases verified, especially 4.2 (no duplicate) and 4.4 (length boundary).
