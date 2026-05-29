#!/usr/bin/env python3
"""Throwaway feasibility probe for WindrosePlus spawn / entity injection.

This script fetches the current WindrosePlus docs and checks for a
first-class spawn command or any obvious entity-creation surface.
"""

from __future__ import annotations

import re
import sys
import urllib.request

URLS = {
    "admin": "https://raw.githubusercontent.com/humangenome/WindrosePlus/main/WindrosePlus/Scripts/modules/admin.lua",
    "commands": "https://raw.githubusercontent.com/humangenome/WindrosePlus/main/docs/commands.md",
    "scripting": "https://raw.githubusercontent.com/humangenome/WindrosePlus/main/docs/scripting-guide.md",
    "main": "https://raw.githubusercontent.com/humangenome/WindrosePlus/main/WindrosePlus/Scripts/main.lua",
}

TERMS = ["spawn", "summon", "creature", "entity", "actor", "pawn", "wp.creatures", "wp.entities"]


def fetch(url: str) -> str:
    with urllib.request.urlopen(url, timeout=30) as resp:
        return resp.read().decode("utf-8", "replace")


def main() -> int:
    docs = {name: fetch(url) for name, url in URLS.items()}

    print("WindrosePlus spawn/entity probe")
    print("================================")
    for name, text in docs.items():
        print(f"\n[{name}]")
        for term in TERMS:
            hits = len(re.findall(re.escape(term), text, re.I))
            if hits:
                print(f"  {term}: {hits}")

    commands = docs["commands"]
    if "wp.creatures" in commands or "wp.entities" in commands:
        print("\nResult: entity counts exist as diagnostics.")
    else:
        print("\nResult: entity diagnostics were not found in the docs fetched.")

    admin = docs["admin"]
    if re.search(r"spawn|summon", admin, re.I):
        print("Result: spawn-like terms exist in admin.lua, but this spike did not confirm a first-class spawn command.")
    else:
        print("Result: no spawn-like terms were found in admin.lua.")

    print("Result: no documented first-class spawn/summon API was confirmed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
