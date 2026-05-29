# Companion State Webserver Milestones

## Milestone 1 - First Useful Status Page

- [x] Service starts as sidecar
- [x] `/health` returns OK
- [x] `/state` shows server ready state and island id
- [x] `/players` shows active or recently seen player sessions
- [x] Browser page shows server and player state

## Milestone 2 - Reliable Event Timeline

- [x] Connect events are parsed correctly
- [x] Join events are parsed correctly
- [x] Expected disconnects are parsed correctly
- [x] Unexpected disconnects are highlighted
- [x] Log rotation does not break tailing

## Milestone 3 - Save Snapshot Awareness

- [x] Active world id is detected
- [x] Latest backup is detected
- [x] Backup age is shown
- [x] `WorldDescription.json` is exposed through API

## Milestone 4 - Rich State Proof

- [x] RocksDB checkpoint can be opened read-only
- Player/world/ship document keys are identified
- At least one useful state document is decoded
- Decision made on whether full companion-like state is feasible

## Milestone 5 - Operator Ready

- [x] Compose deployment documented
- [x] Webserver access secured or LAN-scoped
- [x] Smoke validation documented
- [x] Portainer deployment notes captured
