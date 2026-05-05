# Server State Observability Questions

## Product

- [ ] Should the first UI be an operator dashboard, stream overlay, or developer JSON API?
- [ ] Which state needs to be live within seconds?
- [ ] Which state can be snapshot-based from backup ZIPs?
- [ ] Should player/account ids be shown, redacted, or mapped to nicknames?

## Technical

- [ ] Can Windrose+ dashboard APIs be reused instead of building a full parser?
- [ ] Can RocksDB values be decoded with standard tooling?
- [ ] What serialization format is used inside RocksDB values?
- [ ] Are player and ship coordinates persisted often enough for useful display?
- [ ] Are map reveal and marker states stored in a readable document?
- [ ] Is the companion app protocol simple JSON over WebSocket?

## Deployment

- [ ] Should the webserver run inside the existing Windrose container or as a sidecar?
- [ ] What LAN port should the webserver use?
- [ ] Should access be LAN-only, password protected, or behind existing reverse proxy auth?
- [ ] Should the state snapshot persist across container restarts?
