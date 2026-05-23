# Companion State Webserver Milestones

## Milestone 1 - First Useful Status Page

- [ ] Service starts as sidecar
- [ ] `/health` returns OK
- [ ] `/state` shows server ready state and island id
- [ ] `/players` shows active or recently seen player sessions
- [ ] Browser page shows server and player state

## Milestone 2 - Reliable Event Timeline

- [ ] Connect events are parsed correctly
- [ ] Join events are parsed correctly
- [ ] Expected disconnects are parsed correctly
- [ ] Unexpected disconnects are highlighted
- [ ] Log rotation does not break tailing

## Milestone 3 - Save Snapshot Awareness

- [ ] Active world id is detected
- [ ] Latest backup is detected
- [ ] Backup age is shown
- [ ] `WorldDescription.json` is exposed through API

## Milestone 4 - Rich State Proof

- [ ] RocksDB checkpoint can be opened read-only
- [ ] Player/world/ship document keys are identified
- [ ] At least one useful state document is decoded
- [ ] Decision made on whether full companion-like state is feasible

## Milestone 5 - Operator Ready

- [x] Compose deployment documented
- [x] Webserver access secured or LAN-scoped
- [x] Smoke validation documented
- [x] Portainer deployment notes captured
