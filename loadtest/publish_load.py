#!/usr/bin/env python3
"""
Load-test publisher for MQTT_Vilog_Malaysia.

Simulates many Vilog devices publishing telemetry at once, to stress the
subscriber's worker queue / Mongo write path.

SAFETY: defaults to 127.0.0.1 broker. Never point this at the production
broker (vilog.viwater.vn) — it creates sites/channels in MongoDB.

Topic format expected by the app:  Vilog_{location}_{loggerid}_{suffix}
loggerid must NOT contain '_'. We use zero-padded numbers.

Examples:
  python publish_load.py --devices 10000 --clients 50 --meter kronhe
  python publish_load.py --devices 2000 --meter su --host 127.0.0.1
"""
import argparse
import json
import threading
import time
from datetime import datetime, timezone

import paho.mqtt.client as mqtt


def kronhe_payload(loggerid: str) -> dict:
    # 6 registers x 8 hex chars, parsed from offset 2 => total len 50 (Kronhe branch: Payload.Length <= 50)
    reg = "00000000"          # IEEE-754 float bits -> 0.0 (ConvertHexToDouble)
    realtime = "00" + reg * 6  # 2 header chars + 48 = 50
    # one history-log entry: [hexRegs(>=50), isoTime]  (AnalyzeLogDataHronheMeter)
    log_hex = "00" + reg * 6
    log_time = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    return {
        "IMEI": loggerid,
        "IMSI": "",
        "Model": "loadtest-kronhe",
        "Payload": realtime,
        "battery": 3.9,
        "signal": 25,
        "time": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        # JsonExtensionData -> AdditionalData; key "1" = one log record
        "1": [log_hex, log_time],
    }


def su_payload(loggerid: str) -> dict:
    # realtime: 66 chars => 16 regs of 4 (parsed from offset 2). zeros are valid.
    realtime = "00" + "0000" * 16  # 2 + 64 = 66
    # one log chunk of 60 chars (15 regs of 4). Encode a valid date to avoid DateTime throw.
    # reg0=year(2025=0x07E9), reg1=month/day(01/01), reg2=hour/min(00/00), rest zeros
    log_chunk = "07E9" + "0101" + "0000" + "0000" * 12  # 15 regs * 4 = 60
    payload = realtime + log_chunk  # 126
    return {
        "IMEI": loggerid,
        "IMSI": "",
        "Model": "loadtest-su",
        "Payload": payload,
        "battery": 3.9,
        "signal": 25,
        "time": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    }


def build_payload(meter: str, loggerid: str) -> str:
    d = kronhe_payload(loggerid) if meter == "kronhe" else su_payload(loggerid)
    return json.dumps(d)


def worker(idx, ids, args, counters, lock):
    client = mqtt.Client(client_id=f"loadtest-{idx}", clean_session=True)
    client.connect(args.host, args.port, keepalive=60)
    client.loop_start()
    sent = 0
    delay = (1.0 / args.rate) if args.rate > 0 else 0.0
    infos = []
    for loggerid in ids:
        topic = f"Vilog_{args.location}_{loggerid}_PUB"
        payload = build_payload(args.meter, loggerid)
        info = client.publish(topic, payload, qos=args.qos)
        if args.qos > 0:
            infos.append(info)
        sent += 1
        if delay:
            time.sleep(delay)
    # for QoS>0, ensure broker acked everything before disconnecting
    for info in infos:
        try:
            info.wait_for_publish(timeout=30)
        except Exception:
            pass
    client.loop_stop()
    client.disconnect()
    with lock:
        counters["sent"] += sent


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--port", type=int, default=1883)
    ap.add_argument("--devices", type=int, default=10000, help="unique logger ids")
    ap.add_argument("--clients", type=int, default=50, help="parallel publisher connections")
    ap.add_argument("--meter", choices=["kronhe", "su"], default="kronhe")
    ap.add_argument("--location", default="LOADTEST")
    ap.add_argument("--qos", type=int, choices=[0, 1, 2], default=0)
    ap.add_argument("--rate", type=float, default=0.0, help="msgs/sec per client (0 = max burst)")
    ap.add_argument("--prefix", default="LT", help="logger id prefix (no underscore)")
    args = ap.parse_args()

    if "viwater" in args.host:
        raise SystemExit("Refusing to publish to a production-looking host. Use a local broker.")

    ids = [f"{args.prefix}{i:06d}" for i in range(args.devices)]
    # split across clients
    chunks = [ids[i::args.clients] for i in range(args.clients)]

    counters = {"sent": 0}
    lock = threading.Lock()
    threads = []

    print(f"Publishing {args.devices} {args.meter} msgs via {args.clients} clients "
          f"to {args.host}:{args.port} (qos={args.qos}, rate={'max' if args.rate==0 else args.rate})")
    t0 = time.time()
    for idx, chunk in enumerate(chunks):
        if not chunk:
            continue
        t = threading.Thread(target=worker, args=(idx, chunk, args, counters, lock))
        t.start()
        threads.append(t)
    for t in threads:
        t.join()
    dt = time.time() - t0

    print(f"Done. sent={counters['sent']} in {dt:.2f}s "
          f"=> {counters['sent']/dt:.0f} msg/s")


if __name__ == "__main__":
    main()
