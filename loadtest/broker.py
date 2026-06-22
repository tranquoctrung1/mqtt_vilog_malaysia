#!/usr/bin/env python3
"""Minimal local MQTT broker (amqtt) for load testing. Anonymous, tcp/1883."""
import asyncio
import logging

from amqtt.broker import Broker

logging.basicConfig(level=logging.WARNING)

CONFIG = {
    "listeners": {
        "default": {"type": "tcp", "bind": "0.0.0.0:1883", "max_connections": 0},
    },
    "sys_interval": 0,
    "auth": {"allow-anonymous": True},
    "topic_check": {"enabled": False},
}


async def main():
    broker = Broker(CONFIG)
    await broker.start()
    print("BROKER_READY", flush=True)
    while True:
        await asyncio.sleep(3600)


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        pass
