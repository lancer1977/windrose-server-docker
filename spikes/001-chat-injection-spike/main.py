#!/usr/bin/env python3
"""Throwaway feasibility probe for WindrosePlus chat injection.

This script fetches the current WindrosePlus docs and checks whether a
first-class chat send command is documented.
"""

from __future__ import annotations

import re
import sys
import urllib.request

URLS = {
    "admin": "https://raw.githubusercontent.com/humangenome/WindrosePlus/main/WindrosePlus/Scripts/modules/admin.lua",
    "commands": "https://raw.githubusercontent.com/humangenome/WindrosePlus/main/docs/commands.md",
    "scripting": "https://raw.githubusercontent.com/humangenome/WindrosePlus/main/docs/scripting-guide.md",
}

TERMS = ["wp.say", "chat", "broadcast", "announcement", "ConsoleCommand", "UWorld::Exec"]


def fetch(url: str) -> str:
    with urllib.request.urlopen(url, timeout=30) as resp:
        return resp.read().decode("utf-8", "replace")


def main() -> int:
    docs = {name: fetch(url) for name, url in URLS.items()}

    print("WindrosePlus chat-injection probe")
    print("================================")
    for name, text in docs.items():
        print(f"\n[{name}]")
        for term in TERMS:
            hits = len(re.findall(re.escape(term), text, re.I))
            if hits:
                print(f"  {term}: {hits}")

    admin = docs["admin"]
    deferred = "wp.kick / wp.netid / wp.say are deferred to v1.3.0" in admin
    if deferred:
        print("\nResult: wp.say is explicitly deferred in the current docs.")
        print("Result: Lua-only UE4SS is not sufficient for chat injection.")
        print("Result: A native UE4SS C++ mod is still the likely path if chat injection is required.")
        return 0

    print("\nResult: no explicit deferred note found; further investigation needed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
