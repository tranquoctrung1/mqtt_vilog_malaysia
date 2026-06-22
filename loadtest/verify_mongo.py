#!/usr/bin/env python3
"""Check how many sites/channels/data the load test produced in the test DB."""
import argparse
from pymongo import MongoClient


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--uri", default="mongodb://127.0.0.1:27017/")
    ap.add_argument("--db", default="vilog_malaysia_loadtest")
    ap.add_argument("--drop", action="store_true", help="drop the test DB after reporting")
    args = ap.parse_args()

    c = MongoClient(args.uri)
    db = c[args.db]

    names = db.list_collection_names()
    sites = db["t_Sites"].count_documents({}) if "t_Sites" in names else 0
    channels = db["t_Channel_Configurations"].count_documents({}) if "t_Channel_Configurations" in names else 0
    data_cols = [n for n in names if n.startswith("t_Data_Logger_")]
    index_cols = [n for n in names if n.startswith("t_Index_Logger_")]
    alarms = db["t_History_Alarm"].count_documents({}) if "t_History_Alarm" in names else 0

    total_data_docs = 0
    for n in data_cols[:200]:  # sample cap
        total_data_docs += db[n].count_documents({})

    print(f"DB: {args.db}")
    print(f"  t_Sites:                 {sites}")
    print(f"  t_Channel_Configurations:{channels}")
    print(f"  t_Data_Logger_* colls:   {len(data_cols)}")
    print(f"  t_Index_Logger_* colls:  {len(index_cols)}")
    print(f"  t_History_Alarm docs:    {alarms}")
    print(f"  data docs (first 200 colls): {total_data_docs}")

    if args.drop:
        c.drop_database(args.db)
        print(f"Dropped test DB '{args.db}'.")


if __name__ == "__main__":
    main()
