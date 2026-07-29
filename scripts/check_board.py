#!/usr/bin/env python3
"""Check Svarkor control board for Djurspel jobs."""
import json, subprocess, os

# Load env from shell — do NOT read the secret file directly
result = subprocess.run(
    ["bash", "-c", "source ~/.hermes/.secrets/mcproxy.env && curl -s http://localhost:8310/api/v1/jobs"],
    capture_output=True, text=True
)
data = json.loads(result.stdout)
jobs = data.get("jobs", data)

for j in jobs:
    title = str(j.get("title", ""))
    status = j.get("status", "")
    jid = j.get("id", "?")
    parent = j.get("parent_id", "")
    created = str(j.get("created_at", ""))[:10]
    print(f"ID={jid} status={status:25s} parent={parent} created={created} title={title[:80]}")
