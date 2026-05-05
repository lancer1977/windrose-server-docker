# Server State Observability Implementation Notes

## Findings From Remote Log Review

Remote host:

```text
192.168.0.252
```

Observed server path:

```text
/home/lancer1977/game_servers/windrose/server-files
```

SMB equivalent:

```text
smb://192.168.0.252/gameservers/windrose/server-files
```

## Log Files

Current and rotated logs live at:

```text
R5/Saved/Logs/
```

The current file was:

```text
R5/Saved/Logs/R5.log
```

Rotated logs follow:

```text
R5-backup-YYYY.MM.DD-HH.MM.SS.log
```

## Container Log Behavior

The startup script tails the game log:

```bash
tail -F "$LOG_FILE" 2>/dev/null &
```

where:

```bash
LOG_FILE="$SERVER_FILES/R5/Saved/Logs/R5.log"
```

So Portainer and Docker logs are useful, but the actual source of truth remains the file under `server-files`.

## Useful Environment Knobs

Recommended diagnostic logging for discovery:

```env
DIAGNOSTIC_MODE=true
WINE_VERBOSE=false
SERVER_ARGS=-log -STDOUT -nullrhi -nosound -LogCmds="R5LogCoopProxy Verbose,R5LogNetCm Verbose,R5LogNetBL Verbose,R5LogDataKeeper Verbose,R5LogP2pGate Verbose,R5LogSocketSubsystem Verbose,LogNet Verbose"
```

Use diagnostic logging sparingly. It increases log volume and still may not expose companion-style map data.

## Known High-Value Markers

### Ready

```text
Host server is ready for owner to connect
```

### Server Settings

The registration block logs values such as:

```text
InviteCode
ServerName
WorldIslandId
MaxPlayerCount
P2pProxyAddress
UseDirectConnection
DirectConnectionServerPort
```

### Player Session

The logs expose account/session pairs:

```text
AccountId <account-id>. BLPlayerSessionId <session-id>
```

### Join

```text
LogNet: Join request: /Game/Maps/Lobby/R5ServerLobby?BLPlayerSessionId=<session-id>?Name=<client-name>
```

### Disconnect

```text
UR5CoopProxyServer::OnAccountDisconnected
```

Disconnect reasons observed include:

- `BL disconnected`
- `Go to lobby`
- `Connection reset by peer`
- P2P gate disconnects

## Save Data Findings

Live world path:

```text
R5/Saved/SaveProfiles/Default/RocksDB_v2/0.10.0/Worlds/<island-id>/
```

Backup path:

```text
R5/Saved/SaveProfiles/Default/RocksDB_v2_Backups/Worlds/<island-id>/
```

The latest backup ZIP contained:

- [x] RocksDB checkpoint files
- [x] `AdditionalRecordFiles/WorldDescription.json`

String scans of RocksDB `.sst` files found names including:

- `R5BLPlayerInWorld`
- `R5BLPlayer`
- `R5BLShip`
- `R5BLActor_BuildingBlock`
- `R5BLActor_MineralNode`
- `R5BLIslandChest`
- `Location`
- `Rotation`
- `Inventory`
- `Quest`

This suggests the save data is the best route for companion-like state, but decoding still needs proof.

## Implementation Bias

Start with a parser that can ship quickly:

- [ ] read-only mount of `server-files`
- [ ] log-derived server/player state
- [ ] JSON API
- [ ] small browser UI

Then add save decoding once the RocksDB format is understood.
