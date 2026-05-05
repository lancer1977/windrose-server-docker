# Companion State Webserver Risks

## Technical Risks

- [ ] Logs may never expose live coordinates or map state.
- [ ] RocksDB values may use Unreal binary serialization that is difficult to decode outside the game.
- [ ] Reading live RocksDB files could be unsafe if done directly.
- [ ] Backup ZIP cadence may be too slow for live map-like behavior.
- [ ] Companion-app protocol may not match dedicated-server data.
- [ ] Windrose updates may change log strings or save formats.

## Operational Risks

- [ ] Verbose logging can create large logs quickly.
- [ ] Public exposure could leak account ids, invite codes, or player activity.
- [ ] A sidecar with write access to `server-files` could damage saves if implemented carelessly.
- [ ] Parsing based on exact log strings can break after game updates.

## Mitigations

- [ ] Keep observer mounts read-only.
- [ ] Prefer checkpoint ZIPs over live RocksDB reads.
- [ ] Redact sensitive ids in the UI by default.
- [ ] Keep raw event capture separate from parsed state.
- [ ] Add parser fixtures from real logs.
- [ ] Treat companion-like map state as a proof-of-concept until decoding is proven.
