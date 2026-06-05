# Windrose V2 safe smoke matrix

Status: V2 complete / dev-server-only
Updated: 2026-06-05

## Safety boundary

All live validation stays on the dev stack only:

- Host: `192.168.0.252`
- Dev server container: `windrose2-dev`
- Dev state-web endpoint: `http://127.0.0.1:8782` from the dev host
- Public LAN readback endpoint: `http://192.168.0.252:8782`

Production/main-server mutation is out of scope. Player-bound mutation should use a non-main/throwaway character and must not target a random online player unless that player explicitly consented.

## Smoke modes

| Mode | Target | Allowed actions | Evidence | Block condition |
| --- | --- | --- | --- | --- |
| Offline/mock smoke | Local repo temp files | Install/config proof, dry-run payload validation | `scripts/smoke_windrose_sidecar_bridge.sh`; dry-run response with `executed=false` | local script missing or exits non-zero |
| Dev no-player smoke | `windrose2-dev` + state-web | Container status, plugin heartbeat/status, action capability readback | `docker ps`, `GET /api/plugin/status`, `GET /api/runtime/action-capabilities` | dev host unavailable or endpoint down |
| Operator non-main character smoke | Explicit operator-selected dev character | Read-only probes and later harmless V3 commands only | selected character name/id, sidecar/plugin logs, no unexpected mutation | no non-main character available |
| Consenting player smoke | Explicit consenting dev-server player | Read-only probes and later harmless V3 commands only | consent note, selected player, command/event trace, no corruption | no consent or unclear target |
| Random online dev-player probe | Random current dev-server player | Read-only metadata only | read-only query/log evidence; no command request | any mutation would be required |
| Sidecar/plugin-down failure smoke | Dev stack fault/degraded state | Read-only health/degraded-mode checks | safe denial/degraded response, no crash | test requires restart/mutation without approval |

## Commands verified for V2

Local install/config proof:

```bash
cd /home/lancer1977/code/windrose-server-docker
./scripts/smoke_windrose_sidecar_bridge.sh
```

Observed result on 2026-06-05: plugin copied into a disposable `windrose_plus_mods/windrose-sidecar-bridge` tree, bridge config written, Lua proof skipped because no Lua interpreter was installed locally.

Dev no-player status proof:

```bash
ssh hermes@192.168.0.252 'docker ps --format "{{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -i windrose'
ssh hermes@192.168.0.252 'curl -sS --max-time 10 http://127.0.0.1:8782/api/plugin/status'
ssh hermes@192.168.0.252 'curl -sS --max-time 10 http://127.0.0.1:8782/api/runtime/action-capabilities'
```

Observed result on 2026-06-05:

- `windrose-state-web-dev` was up with `0.0.0.0:8782->8781/tcp`.
- `windrose2-dev` was up and healthy with `0.0.0.0:7787->7777/udp` and `0.0.0.0:7788->7778/udp`.
- `api/plugin/status` returned `connected=true`, `status=started`, and `mode=dry-run-only`.
- `api/runtime/action-capabilities` returned `readOnly=true`, `knownCount=9`, `enabledCount=0`, and all known actions unsupported for live execution in this slice.

Read-only player/session proof:

```bash
ssh hermes@192.168.0.252 'curl -sS --max-time 10 http://127.0.0.1:8782/api/players'
ssh hermes@192.168.0.252 'curl -sS --max-time 10 http://127.0.0.1:8782/api/world/players'
ssh hermes@192.168.0.252 'curl -sS --max-time 10 http://127.0.0.1:8782/api/state | head -c 2000'
```

Observed result on 2026-06-05:

- `/api/players` returned one connected dev-server player/session with `phase=Joined` and `isConnected=true`; this was read-only state-web readback only.
- `/api/world/players` returned `readOnly=true`, `hasDecodedDocuments=false`, and `player-in-world-metadata` as metadata-only; no player document or character state was decoded or written.
- `/api/state` returned `serverName=Polyhydra Games Dev`, `inviteCode=dbcdevs`, `currentIslandId`, `isReady=true`, and save/checkpoint metadata.

Dry-run action proof:

```bash
ssh hermes@192.168.0.252 'curl -sS --max-time 10 -X POST http://127.0.0.1:8782/api/plugin/actions/dry-run -H "Content-Type: application/json" -d "{\"actionId\":\"windrose.spawn.dodo_swarm\",\"targetPlayer\":\"offline-mock\",\"count\":1,\"radiusMeters\":12,\"offsetMeters\":5,\"creatureId\":\"R5.Creature.Dodo\",\"creatureName\":\"Dodo\",\"summon\":{\"count\":1,\"radiusMeters\":12,\"offsetMeters\":5,\"selection\":\"random\",\"creaturePool\":[\"Dodo\",\"Wolf\"]}}"'
```

Observed result on 2026-06-05: `accepted=true`, `dryRun=true`, `executed=false`, `targetPlayer=offline-mock`, `outcome=validated-dry-run-only`.

## V2 conclusion

V2 is safe to close as a foundation/read-only smoke slice. The bridge is still dry-run-only; live mutation remains blocked behind later native-hook, dev-mode, consent, and approval cards.
