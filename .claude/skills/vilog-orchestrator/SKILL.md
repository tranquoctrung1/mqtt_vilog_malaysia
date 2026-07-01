---
name: vilog-orchestrator
description: >
  Orchestrates all development workflows for MQTT_Vilog_Malaysia — an IoT MQTT data ingestion
  system (C# .NET 8, MQTTnet, MongoDB). Use this skill for: fixing bugs in MQTT message
  processing or MongoDB storage, adding new meter types or channels, implementing new alarm logic,
  validating MongoDB data integrity, reviewing data pipeline changes, updating alarm thresholds,
  debugging duplicate data or missing records, adding Level/SU/Kronhe meter support, and any
  feature involving meter handling or channel provisioning.
  Triggers on: "bug in data", "duplicate records", "new meter", "add channel", "alarm update",
  "data not coming in", "MQTT processing", "MongoDB validation", "Level meter", "SU meter",
  "Kronhe meter", "OverTime", "timestamp drift", "channel provisioning", "run again", "re-run",
  "fix this", "implement", "feature request", "re-diagnose", "update", "add support for".
---

# Vilog Malaysia — Development Orchestrator

## Execution Mode

- **Bug Fix**: Pipeline (analyst → dev → qa)
- **Feature Add**: Hybrid fan-out (dev + qa in parallel) → fan-in (qa integration check)
- **Data Check**: Single agent (qa or analyst)

All Agent calls use `model: "opus"`. All agents must read their definition file at `.claude/agents/{name}.md` at the start of execution.

---

## Phase 0: Context Check

Check if `_workspace/` directory exists:
- Not found → **initial run** (proceed to Phase 1)
- Found + user requests partial update → **partial re-run** (affected phase only)
- Found + new request → move `_workspace/` to `_workspace_prev/`, then **new run**

Create `_workspace/` if not exists.

---

## Phase 1: Workflow Classification

Classify the user request:

| Type | Keywords | Workflow |
|------|----------|----------|
| **Bug Fix** | bug, error, duplicate, data missing, not inserting, exception, wrong value | A |
| **Feature Add** | new feature, add meter type, add channel, alarm logic, implement, add support | B |
| **Data Check** | verify data, MongoDB check, query collection, integrity | C |

If unclear, ask the user.

---

## Workflow A: Bug Fix (Pipeline)

### A-1. Analyst Diagnosis

```
Agent(
  subagent_type: "general-purpose",
  model: "opus",
  prompt: "Read .claude/agents/vilog-analyst.md for your role, knowledge, and protocols. Then investigate the following issue: [symptom description]. Perform root cause analysis. Output: filename:line_number, data flow trace, fix direction. Save to _workspace/01_analyst_diagnosis.md."
)
```

Verify artifact exists, then proceed to A-2.

### A-2. Dev Fix

```
Agent(
  subagent_type: "general-purpose",
  model: "opus",
  prompt: "Read .claude/agents/vilog-dev.md for your role, file locations, and implementation patterns. Read _workspace/01_analyst_diagnosis.md for the fix direction. Implement the fix. Save changed files + summary to _workspace/02_dev_changes.md."
)
```

### A-3. QA Verification

```
Agent(
  subagent_type: "general-purpose",
  model: "opus",
  prompt: "Read .claude/agents/vilog-qa.md for your role and validation checklist. Read _workspace/02_dev_changes.md for what changed. Run or write integration tests covering the fix. Save test results to _workspace/03_qa_result.md."
)
```

QA fails → re-run A-1 with failure output appended to symptom.

---

## Workflow B: Feature Add (Hybrid Fan-out/Fan-in)

### B-1. Dev Implementation + QA Test Prep (parallel)

```python
# Run in parallel (run_in_background: true for both)

Agent(
  subagent_type: "general-purpose",
  model: "opus",
  run_in_background: true,
  prompt: "Read .claude/agents/vilog-dev.md for your role, file locations, and patterns. Implement the following feature: [feature spec]. Follow the provisioning pattern in Subscribe.cs (site insert → channel config insert → handle data call). Save implementation summary to _workspace/02_dev_impl.md."
)

Agent(
  subagent_type: "general-purpose",
  model: "opus",
  run_in_background: true,
  prompt: "Read .claude/agents/vilog-qa.md for your role and validation checklist. Write test scenarios for the following feature: [feature spec]. Include: data insertion validation, channel count validation, duplicate prevention. Save to _workspace/03_qa_plan.md."
)
```

### B-2. QA Integration Check (Fan-in)

```
Agent(
  subagent_type: "general-purpose",
  model: "opus",
  prompt: "Read .claude/agents/vilog-qa.md for your role and validation checklist. Read _workspace/02_dev_impl.md (what was implemented) and _workspace/03_qa_plan.md (test plan). Run integration tests. Save results to _workspace/03_qa_result.md."
)
```

Failure → switch to Workflow A-2 (fix iteration with failure output).

---

## Workflow C: Data Check (Single Agent)

```
Agent(
  subagent_type: "general-purpose",
  model: "opus",
  prompt: "Read .claude/agents/vilog-qa.md for your role and validation checklist. Check MongoDB data integrity for [target: collection name / meter type / channel]. Report anomalies and their likely cause. Save to _workspace/03_qa_datacheck.md."
)
```

Anomalies found → switch to Workflow A (Bug Fix) with anomaly report as symptom.

---

## Data Transfer Protocol

| File | Created by | Consumed by |
|------|------------|-------------|
| `_workspace/01_analyst_diagnosis.md` | vilog-analyst | vilog-dev |
| `_workspace/02_dev_changes.md` | vilog-dev | vilog-qa |
| `_workspace/02_dev_impl.md` | vilog-dev (B) | vilog-qa |
| `_workspace/03_qa_plan.md` | vilog-qa (B prep) | vilog-qa (B integration) |
| `_workspace/03_qa_result.md` | vilog-qa | orchestrator |
| `_workspace/03_qa_datacheck.md` | vilog-qa | orchestrator |

---

## Error Handling

- **Analyst fails**: ask vilog-qa to query MongoDB directly (`db.t_Data_Logger_{channelId}.find()`), then retry A-1
- **Dev build error**: pass `dotnet build` output back to dev agent and retry once
- **QA test failure**: pass failure reason back to analyst (A-1) for re-diagnosis
- **2+ consecutive failures**: request manual intervention from user

---

## Test Scenarios

**Normal flow — Bug Fix (duplicate inserts):**
1. User: "SU meter data has duplicate inserts"
2. Analyst: traces `DataLoggerAction.UpsertByTimeStamp()` → finds ObjectId.Empty issue
3. Dev: fixes upsert using `SetOnInsert(x => x.Id, ObjectId.GenerateNewId())`
4. QA: runs `dotnet test --filter "Category=Integration"` → passes

**Normal flow — Feature Add (Level meter):**
1. User: "Add Level meter support for channel _200"
2. Dev (parallel): implements provisioning + HandleDataLevelMeter
3. QA (parallel): writes test scenarios for _200 channel
4. QA integration: runs tests → verifies _200 data + no duplicates

**Error flow — MongoDB unavailable:**
1. QA throws `MongoConnectionException`
2. Report to user: check MongoDB service and connection string in settings.json
3. After user confirms → re-run QA phase

---

## Completion Report

After each workflow:
1. Summarize changes/validation results to user
2. Ask if further improvement needed
