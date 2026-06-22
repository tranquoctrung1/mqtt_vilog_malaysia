#!/usr/bin/env bash
# End-to-end load test:
#   1. start a LOCAL mosquitto broker (Docker)
#   2. run the app pointed at localhost + a throwaway test DB (env overrides, prod settings.json untouched)
#   3. publish N device messages
#   4. wait for the worker queue to drain, report Mongo counts
#   5. tear everything down
#
# Usage:
#   bash loadtest/run_loadtest.sh [DEVICES] [METER] [CLIENTS] [WORKERS] [QUEUE]
# Defaults: 10000 kronhe 50 16 20000
set -u

DEVICES="${1:-10000}"
METER="${2:-kronhe}"
CLIENTS="${3:-50}"
WORKERS="${4:-16}"
QUEUE="${5:-20000}"
PORT="${6:-1884}"   # avoid clashing with other brokers on 1883

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJ="$ROOT/MQTT_Vilog_Malaysia"
DLL="$PROJ/bin/Debug/net8.0/MQTT_Vilog_Malaysia.dll"
APP_LOG="$SCRIPT_DIR/app.log"
TEST_DB="vilog_malaysia_loadtest"
CONTAINER="vilog-mqtt-test"
FIFO="$SCRIPT_DIR/.appin"

cleanup() {
  echo "--- cleanup ---"
  [ -n "${APP_PID:-}" ] && kill "$APP_PID" 2>/dev/null
  pkill -f "time.sleep(987654)" 2>/dev/null
  docker rm -f "$CONTAINER" >/dev/null 2>&1
}
trap cleanup EXIT

echo "=== 1. build ==="
dotnet build "$PROJ/MQTT_Vilog_Malaysia.csproj" -c Debug -nologo -v q || exit 1
rm -rf "$PROJ/Error" "$PROJ/Log"   # clean stale logs for accurate reporting

echo "=== 2. start mosquitto (docker, port $PORT) ==="
docker rm -f "$CONTAINER" >/dev/null 2>&1
MSYS_NO_PATHCONV=1 docker run -d --name "$CONTAINER" -p "$PORT:1883" eclipse-mosquitto:2 \
  sh -c "printf 'listener 1883 0.0.0.0\nallow_anonymous true\nmax_queued_messages 1000000\n' > /tmp/m.conf && exec mosquitto -c /tmp/m.conf" \
  >/dev/null || { echo "docker run failed"; exit 1; }
for i in $(seq 1 40); do
  if python -c "import socket;socket.create_connection(('127.0.0.1',$PORT),1).close()" 2>/dev/null; then
    echo "broker up"; break
  fi
  sleep 0.5
done

echo "=== 3. start app (localhost, test DB=$TEST_DB, workers=$WORKERS, queue=$QUEUE) ==="
rm -f "$APP_LOG"
# Keep stdin open with a real pipe (MSYS FIFO emulation gives spurious EOF) so the
# app's Console.ReadLine() blocks and the process stays alive for the whole test.
cd "$PROJ" || exit 1
Settings__IpMQTT=127.0.0.1 \
Settings__Port="$PORT" \
Settings__DBName="$TEST_DB" \
Settings__WorkerCount="$WORKERS" \
Settings__QueueCapacity="$QUEUE" \
Settings__IpCheck="http://127.0.0.1:9/api" \
dotnet "$DLL" < <(python -c "import time;time.sleep(987654)") > "$APP_LOG" 2>&1 &
APP_PID=$!
cd "$ROOT"

# wait for subscription
for i in $(seq 1 40); do
  grep -q "MQTT client subscribed" "$APP_LOG" 2>/dev/null && { echo "app subscribed"; break; }
  if ! kill -0 "$APP_PID" 2>/dev/null; then echo "app exited early:"; cat "$APP_LOG"; exit 1; fi
  sleep 0.5
done

echo "=== 4. publish $DEVICES $METER messages ==="
python "$SCRIPT_DIR/publish_load.py" --host 127.0.0.1 --port "$PORT" \
  --devices "$DEVICES" --clients "$CLIENTS" --meter "$METER" --qos 1

echo "=== 5. wait for queue drain ==="
prev=-1; stable=0; maxram=0; maxthreads=0
for i in $(seq 1 200); do
  # sample app RAM/threads (peak)
  samp=$(powershell.exe -NoProfile -Command "\$p=Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { \$_.CommandLine -like '*MQTT_Vilog_Malaysia.dll*' } | Select-Object -First 1; if(\$p){ \$pr=Get-Process -Id \$p.ProcessId; '{0} {1}' -f [math]::Round(\$pr.WorkingSet64/1MB),\$pr.Threads.Count }" 2>/dev/null | tr -d '\r')
  ram=$(echo "$samp" | awk '{print $1}'); thr=$(echo "$samp" | awk '{print $2}')
  [ -n "$ram" ] && [ "$ram" -gt "$maxram" ] 2>/dev/null && maxram=$ram
  [ -n "$thr" ] && [ "$thr" -gt "$maxthreads" ] 2>/dev/null && maxthreads=$thr
  cur=$(python - "$TEST_DB" <<'PY' 2>/dev/null
import sys
from pymongo import MongoClient
db=MongoClient("mongodb://127.0.0.1:27017/")[sys.argv[1]]
print(db["t_Sites"].count_documents({}) if "t_Sites" in db.list_collection_names() else 0)
PY
)
  cur="${cur:-0}"
  echo "  t_Sites=$cur"
  # done when all devices provisioned, or count stops growing for 8 consecutive checks (~16s)
  if [ "$cur" -ge "$DEVICES" ]; then echo "all $DEVICES provisioned"; break; fi
  if [ "$cur" = "$prev" ] && [ "$cur" -gt 0 ]; then
    stable=$((stable+1)); [ "$stable" -ge 8 ] && { echo "stalled at $cur (stable)"; break; }
  else
    stable=0
  fi
  prev="$cur"
  sleep 2
done

echo "=== 6. results ==="
echo "Peak app RAM=${maxram}MB, peak Threads=${maxthreads}"
python "$SCRIPT_DIR/verify_mongo.py" --db "$TEST_DB"

echo "--- app memory / threads (find dotnet running our dll) ---"
powershell.exe -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='dotnet.exe'\" | Where-Object { \$_.CommandLine -like '*MQTT_Vilog_Malaysia.dll*' } | ForEach-Object { \$p = Get-Process -Id \$_.ProcessId; '{0} RAM={1}MB Threads={2}' -f \$_.ProcessId, [math]::Round(\$p.WorkingSet64/1MB), \$p.Threads.Count }" 2>/dev/null

echo "--- app error log tail ---"
tail -n 15 "$PROJ/Error/"*.txt 2>/dev/null || echo "(no error log)"

echo
echo "Test DB '$TEST_DB' kept for inspection. Drop with:"
echo "  python loadtest/verify_mongo.py --db $TEST_DB --drop"
