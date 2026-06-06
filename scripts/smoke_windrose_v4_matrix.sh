#!/bin/bash
# V4 smoke matrix checklist for the Windrose sidecar bridge.
# Defaults to a read-only/dry-run checklist. Optional endpoint validation
# checks the smoke-options payload without executing any live smoke.
set -euo pipefail

usage() {
    cat <<'EOF'
Usage: scripts/smoke_windrose_v4_matrix.sh [--dry-run] [--check-endpoint URL]

Render the Windrose V4 smoke matrix checklist.

Defaults:
  - read-only / dry-run only
  - no live smoke execution
  - no production targets

Optional validation:
  --check-endpoint URL   Fetch and validate GET /api/plugin/smoke-options
                         against the expected V4 matrix. Requires curl + python3.

The matrix covers:
  - offline/mock player
  - dev server with no player
  - operator non-main character
  - consenting dev player
  - random read-only dev player probe
  - sidecar/plugin-down failure
  - plugin reload (dev-only operational probe)
  - malformed command (invalid payload / unknown action)
EOF
}

CHECK_ENDPOINT=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        --help|-h)
            usage
            exit 0
            ;;
        --dry-run)
            shift
            ;;
        --check-endpoint)
            if [[ $# -lt 2 ]]; then
                echo "--check-endpoint requires a URL" >&2
                exit 2
            fi
            CHECK_ENDPOINT="$2"
            shift 2
            ;;
        *)
            echo "Unknown argument: $1" >&2
            usage >&2
            exit 2
            ;;
    esac
done

if [[ -n "$CHECK_ENDPOINT" ]]; then
    if ! command -v curl >/dev/null 2>&1; then
        echo "curl is required for --check-endpoint" >&2
        exit 1
    fi
    if ! command -v python3 >/dev/null 2>&1; then
        echo "python3 is required for --check-endpoint" >&2
        exit 1
    fi

    payload="$(curl -fsSL "$CHECK_ENDPOINT")"
    SMOKE_OPTIONS_JSON="$payload" python3 - "$CHECK_ENDPOINT" <<'PY'
import json
import os
import sys

url = sys.argv[1]
data = json.loads(os.environ["SMOKE_OPTIONS_JSON"])

expected_modes = {
    "offline-mock-player": {"readOnly": True, "approvalRequired": False, "blockIfMutationRequested": True},
    "dev-server-no-player": {"readOnly": True, "approvalRequired": False, "blockIfMutationRequested": True},
    "random-online-dev-player-read-only": {"readOnly": True, "approvalRequired": False, "blockIfMutationRequested": True},
    "operator-non-main-character": {"readOnly": False, "approvalRequired": True, "blockIfMutationRequested": False},
    "consenting-dev-player": {"readOnly": False, "approvalRequired": True, "blockIfMutationRequested": False},
    "sidecar-plugin-down-failure": {"readOnly": True, "approvalRequired": False, "blockIfMutationRequested": True},
    "plugin-reload": {"readOnly": False, "approvalRequired": False, "blockIfMutationRequested": False},
    "malformed-command": {"readOnly": True, "approvalRequired": False, "blockIfMutationRequested": True},
}

missing = []
if data.get("pluginId") != "windrose-sidecar-bridge":
    missing.append(f"pluginId=windrose-sidecar-bridge (got {data.get('pluginId')!r})")
if data.get("readOnly") is not True:
    missing.append(f"readOnly=true (got {data.get('readOnly')!r})")
if data.get("matrix") != "docs/roadmaps/windrose-runtime-control-surface/safe-smoke-harness-matrix.md":
    missing.append("matrix path mismatch")

global_rules = "\n".join(data.get("globalRules") or [])
for phrase in [
    "Dev server only for any smoke that touches a live Windrose runtime or player state.",
    "Player-bound smokes default to a non-main / throwaway character.",
    "Random player testing is read-only only unless explicit consent exists.",
    "Mutating smokes require approval, exact target identity, log capture, and a rollback or revert plan.",
]:
    if phrase not in global_rules:
        missing.append(f"missing global rule phrase: {phrase}")

modes = {mode.get("modeId"): mode for mode in (data.get("modes") or []) if isinstance(mode, dict)}
for mode_id, expectations in expected_modes.items():
    mode = modes.get(mode_id)
    if mode is None:
        missing.append(f"missing modeId: {mode_id}")
        continue
    for key, expected in expectations.items():
        if mode.get(key) is not expected:
            missing.append(f"{mode_id}.{key} expected {expected!r} got {mode.get(key)!r}")
    if not mode.get("allowedTarget"):
        missing.append(f"{mode_id}.allowedTarget missing")
    if not mode.get("evidence"):
        missing.append(f"{mode_id}.evidence missing")

if missing:
    print(f"Smoke-options validation failed for {url}", file=sys.stderr)
    for item in missing:
        print(f"- {item}", file=sys.stderr)
    sys.exit(1)

print(f"Smoke-options validation passed for {url}")
PY
fi

cat <<'EOF'
Windrose V4 smoke matrix checklist
==================================

Default safety posture
- Read-only or dry-run by default.
- No production or main-target smoke.
- Mutation only on approved dev stacks with explicit target identity.
- Player-bound mutation requires either a clearly non-main / throwaway target or recorded consent.
- Capture logs, timestamps, exact payload/command, and rollback/revert plan before any mutation.

Mode checklist
1) offline-mock-player
   - Allowed target: local fixture, disposable harness, or mocked player object
   - Evidence: harness output, fixture snapshot, pass/fail result
   - Block if: it needs a real server, real credentials, or real player state

2) dev-server-no-player
   - Allowed target: dev server with no connected players
   - Evidence: server/bridge status, manifest or health response, logs showing the read-only path
   - Block if: any step would mutate server state or the server is not a dev server

3) operator-non-main-character
   - Allowed target: dev server + clearly named throwaway / non-main character
   - Evidence: pre/post state, command log, rollback record, timestamps
   - Block if: the target is ambiguous, the character is primary, or the run would touch prod/main

4) consenting-dev-player
   - Allowed target: dev server + explicitly consenting player account
   - Evidence: consent record, pre/post state, logs, timestamps
   - Block if: consent is missing, not recorded, or the action would affect a non-consenting player

5) random-online-dev-player-read-only
   - Allowed target: any connected dev player, read-only probes only
   - Evidence: probe output, status response, confirmation that no writes occurred
   - Block if: the probe would write, prompt, grant items, teleport, or otherwise mutate state

6) sidecar-plugin-down-failure
   - Allowed target: dev stack or local harness with the plugin or sidecar intentionally disabled
   - Evidence: graceful failure message, degraded-mode behavior, no fallback write path
   - Block if: the harness auto-falls back to mutation or the failure test is pointed at prod/main

7) plugin-reload
   - Allowed target: approved dev stack or local harness while reloading the bridge/plugin boundary
   - Evidence: reload log, status after reload, no fallback write path
   - Block if: the reload targets prod/main or the harness silently falls back to a mutating path

8) malformed-command
   - Allowed target: dev harness or bridge endpoint with an intentionally invalid payload or unknown action
   - Evidence: rejected response, validation error, no action file written
   - Block if: the invalid command is redirected into a real mutation path or production target

EOF
