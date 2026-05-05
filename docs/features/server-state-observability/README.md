# Server State Observability

## Summary

This feature tracks how the Windrose dedicated server can expose useful runtime state from container logs, Unreal/R5 logs, and persisted save data.

The current server does not expose the same companion-app WebSocket surface that the Windows companion app provides. The practical path is to combine lightweight log parsing for server/player lifecycle with save-state inspection for richer world state.

## Status

- [x] Local Docker repo cloned to `/home/lancer1977/code/windrose-server-docker`
- [x] Remote server logs inspected under `/home/lancer1977/game_servers/windrose/server-files/R5/Saved/Logs`
- [x] Current log stream confirmed as `R5/Saved/Logs/R5.log`
- [x] Player lifecycle markers found in logs
- [x] World/save storage located under `R5/Saved/SaveProfiles/Default`
- [x] RocksDB checkpoint backups found under `RocksDB_v2_Backups`
- [ ] Companion-style webserver implemented
- [ ] RocksDB/checkpoint reader implemented
- [ ] Browser UI implemented

## Runtime Surfaces

### Container Logs

The container startup script tails:

```text
/home/steam/server-files/R5/Saved/Logs/R5.log
```

That means Portainer and `docker logs windrose` show most new lines from the game log after startup.

### Game Log

Host path:

```text
server-files/R5/Saved/Logs/R5.log
```

The log is useful for:

- [x] server boot and readiness
- [x] island id and server registration
- [x] invite code / server settings echoed during registration
- [x] player session ids
- [x] account ids
- [x] login and join events
- [x] disconnect events
- [x] save/backup cadence
- [x] resource usage reports

The log is not yet proven useful for:

- [ ] live player coordinates
- [ ] live ship coordinates
- [ ] complete inventory state
- [ ] full map reveal state
- [ ] companion-app WebSocket payloads

### Save Data

Host path:

```text
server-files/R5/Saved/SaveProfiles/Default
```

Important subpaths:

```text
RocksDB_v2/0.10.0/Worlds/<island-id>/
RocksDB_v2_Backups/Worlds/<island-id>/
```

The current server writes RocksDB state and periodic checkpoint ZIP backups. The latest backup contains a RocksDB checkpoint plus `AdditionalRecordFiles/WorldDescription.json`.

## Current Useful Log Markers

- Server initialized:

```text
UR5CoopProxyServer::Init
Server initialized. CurrentIslandId <id>
```

- Host ready:

```text
UR5CoopProxyServer::SetIsReadyForHostOwnerConnect
Host server is ready for owner to connect
```

- Player/account reservation:

```text
Process AddPlayer. AccountId <account-id>. BLPlayerSessionId <session-id>. IsBL true
```

- UE connection:

```text
OnUeConnect
Ue P2P connection created. Connect to local server. UeLocalPort 7777
```

- Login:

```text
LogNet: Login request: ?BLPlayerSessionId=<session-id>?Name=<client-name>
```

- Join:

```text
LogNet: Join request: /Game/Maps/Lobby/R5ServerLobby?BLPlayerSessionId=<session-id>?Name=<client-name>
```

- Disconnect:

```text
UR5CoopProxyServer::OnAccountDisconnected
Account disconnected. Inform Cm. AccountId <account-id>. BLPlayerSessionId <session-id>
```

## Proposed State Model

- [ ] `server`
  - [ ] ready state
  - [ ] current island id
  - [ ] invite code
  - [ ] server name
  - [ ] max players
  - [ ] direct connection settings
- [ ] `players`
  - [ ] account id
  - [ ] player session id
  - [ ] client name
  - [ ] connection phase
  - [ ] connected/disconnected timestamps
- [ ] `saves`
  - [ ] latest backup timestamp
  - [ ] latest backup path
  - [ ] world description summary
- [ ] `world`
  - [ ] world preset
  - [ ] island id
  - [ ] known entity counts
  - [ ] discovered player/ship position data if readable from RocksDB

## Next Step

Build a small sidecar service that starts with read-only access to `server-files`, tails `R5.log`, and exposes a JSON status endpoint. Add RocksDB/checkpoint parsing only after the log-derived status is stable.
