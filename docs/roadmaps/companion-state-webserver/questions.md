# Companion State Webserver Questions

## Scope

- [ ] Is the first deliverable a JSON API, browser dashboard, OBS overlay, or all three?
- [ ] Should this live in this repo or a separate `windrose-state-webserver` repo?
- [ ] Should the sidecar be generic for other Unreal dedicated servers?

## State

- [ ] Which player state matters first: online/offline, name, account id, location, ship, inventory?
- [ ] How fresh does the state need to be?
- [ ] Is backup-cadence state enough for the first map view?
- [ ] Should historical state be stored or only current state?

## Deployment

- [ ] Which port should the webserver expose?
- [ ] Should Portainer own deployment?
- [ ] Should this be reachable over `windrose.gaming.tools` or LAN-only?
- [ ] What auth layer is acceptable for local use?

## Integration

- [ ] Should the API mimic the companion app WebSocket if observed?
- [ ] Should Channel Cheevos consume the endpoint?
- [ ] Should an OBS browser source consume the endpoint directly?
