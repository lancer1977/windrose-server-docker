# Windrose character clone safety note

Date: 2026-06-05
Scope: read-only inspection of Windrose save/player storage on Alienware dev/source stacks.

## Conclusion

Do not attempt a one-character file copy or blind RocksDB key clone yet.

Current evidence shows Windrose player/character state is game-managed inside RocksDB world databases, not stored as a simple one-file-per-character profile. The safe procedure for plugin/sidecar smoke testing is to use a copied dev stack plus a throwaway or non-main character, with whole SaveProfiles backup/rollback.

As of the 2026-06-05 `windrose2-dev` inspection, `Grittizban` and `Adventurer` were both observed on the same account id (`E65EF81A41AAF4BAC5BC979966825458`) but with different character/player ids (`0F98D8BA9EDE4EEFC99991252946228C` for `Grittizban`, `4CBE496B1B1D403312845036F5B47E83` for `Adventurer`). That is strong evidence the game is maintaining a larger account/character graph rather than a portable per-name save blob, so a safe selective copy still needs an offline graph decoder before any mutation.

## Evidence gathered

Stacks inspected read-only on 192.168.0.252:

- `/home/lancer1977/game_servers/windrose/server-files/R5/Saved/SaveProfiles`
- `/home/lancer1977/game_servers/windrose2/server-files/R5/Saved/SaveProfiles`
- `/home/lancer1977/game_servers/windrose2-dev/server-files/R5/Saved/SaveProfiles`

Observed save layout:

- live RocksDB trees under `Default/RocksDB/0.10.0/Worlds/<WORLD_ID>/`
- live RocksDB_v2 trees under `Default/RocksDB_v2/0.10.0/Worlds/<WORLD_ID>/`
- checkpoint backup zips under `Default/RocksDB_v2_Backups/Worlds/<WORLD_ID>/`
- many `.sst`, `MANIFEST-*`, `OPTIONS-*`, `CURRENT`, `IDENTITY`, `LOCK`, and `WorldDescription.json` files

Observed player-related markers:

- `R5BLPlayer`
- `R5BLPlayerInWorld`
- `R5BLAccount`
- `PlayerId`
- `BLPlayerSessionId`
- `AccountId`
- `SpawnType AsCharacter`
- `SpawnRecordId`
- `WorldLocation`

The markers appear in RocksDB SST/checkpoint data and runtime logs. They do not appear as standalone character files that can be copied independently.

Latest `windrose2-dev` readback also shows the sidecar can observe the current save tree only at the family/metadata level: `player-in-world-metadata` is still `metadata-only`, with no standalone player document decoded yet, while `api/saves/latest` reports `checkpointContainerFormat: RocksDB block-based SST` and a recent whole-`SaveProfiles` backup in place.

Runtime logs also show account/session reservation such as `Process AddPlayer`, `AccountId`, `BLPlayerSessionId`, and `ReserveCoopAccount`, which suggests the game binds live player state to account/session/profile records rather than to a loose character file.

## Cloneability assessment

### By file

Not safe / not found.

No obvious per-character file was found. The save layout is world-level RocksDB data plus world backups.

### By DB key

Not safe yet.

The relevant records appear to be RocksDB column-family/key/value records. Without the game schema, key graph, and player-ID/account-ID rekey rules, cloning a subset of SST data risks dangling references, duplicated IDs, or corrupted inventory/world/player records.

### By game-managed profile

Likely the only safe character-level path, but not yet exposed.

If Windrose or WindrosePlus exposes an in-game character duplication/export/import/admin flow later, that should be preferred over raw save editing. Until then, treat character identity as game-managed and use whole-world/whole-profile copy for test isolation.

## Recommended smoke-test procedure

Use this for Windrose plugin/sidecar mutation smokes.

1. Use `windrose2-dev` only.
   - Do not mutate production/main.
   - Random online players are read-only only.

2. Prefer a throwaway or non-main character.
   - Ask the operator/player to join dev with a non-main character.
   - Warn: this is dev-only and may corrupt or alter that character/world state.

3. Take a whole SaveProfiles backup before any mutation test.

```bash
set -euo pipefail
STACK=/home/lancer1977/game_servers/windrose2-dev
STAMP=$(date +%Y%m%d-%H%M%S)
BACKUP=$STACK/backups/saveprofiles-before-character-smoke-$STAMP.tar.gz
mkdir -p "$STACK/backups"
cd "$STACK"
docker compose stop windrose

tar -C "$STACK/server-files/R5/Saved" -czf "$BACKUP" SaveProfiles

docker compose up -d windrose
printf 'backup=%s\n' "$BACKUP"
```

4. Verify dev stack health before testing.

```bash
cd /home/lancer1977/game_servers/windrose2-dev
docker inspect windrose2-dev --format '{{.State.Health.Status}}'
curl -fsS http://127.0.0.1:8782/health
curl -fsS http://127.0.0.1:8782/api/plugin/status
curl -fsS http://127.0.0.1:8782/api/plugin/smoke-options
```

5. Run sidecar/plugin smoke in the least risky order.

- no-player dry-run/readback
- random dev player read-only probe only
- consenting operator throwaway/non-main character mutation
- verify result readback and logs

6. Roll back whole SaveProfiles if anything looks wrong.

```bash
set -euo pipefail
STACK=/home/lancer1977/game_servers/windrose2-dev
BACKUP=/path/to/saveprofiles-before-character-smoke-YYYYMMDD-HHMMSS.tar.gz
cd "$STACK"
docker compose stop windrose
rm -rf "$STACK/server-files/R5/Saved/SaveProfiles"
tar -C "$STACK/server-files/R5/Saved" -xzf "$BACKUP"
chown -R 1000:1000 "$STACK/server-files/R5/Saved/SaveProfiles" || true
docker compose up -d windrose
```

7. Verify rollback.

```bash
cd /home/lancer1977/game_servers/windrose2-dev
docker inspect windrose2-dev --format '{{.State.Health.Status}}'
docker logs --tail=160 windrose2-dev | grep -E 'LoadedIslandData|IslandId|Save backup|Error|Exception' || true
curl -fsS http://127.0.0.1:8782/health
curl -fsS http://127.0.0.1:8782/api/plugin/status
```

## Follow-up card recommendation

Create a separate research card before attempting character-level cloning:

Title: Windrose save graph: map player/account/character RocksDB records for clone-safe dev fixtures

Acceptance:

- inspect only offline copies/checkpoint zips, never live production data
- identify exact RocksDB column families and keys for `R5BLPlayer`, `R5BLPlayerInWorld`, account/session linkage, inventory, and spawn/world references
- prove whether a full player record graph can be exported/imported into an isolated throwaway dev world
- include corruption rollback proof using whole SaveProfiles backup
- do not ship a clone script unless it passes throwaway-dev restore/boot/login validation

## Focused Grittizban -> Adventurer fixture pass

A follow-up read-only pass specifically searched the source/dev save and log trees for `Grittizban` and `Adventurer` on Alienware (`192.168.0.252`). No save mutation was attempted.

### Character/account observations

- `Grittizban` is the real/source character and was observed in `windrose`, `windrose2`, and one prior `windrose2-dev` log.
- `Adventurer` is the test/destination character and was observed in current `windrose2-dev` logs.
- Both names were observed under the same account id in server logs. This means a clone workflow must not copy account/session identity from one name to the other; the account binding is already shared and game-managed.
- Distinct player ids were observed:
  - `Grittizban`: `0F98D8BA9EDE4EEFC99991252946228C`
  - `Adventurer`: `4CBE496B1B1D403312845036F5B47E83`
- `Grittizban` appeared on the `windrose` active world `F3B27E1F83434AF5A1BBA9B40E848A42` and the `windrose2` / prior-dev world `8D23C893C50A4DAF6390E4E698FC5C8E`.
- Current `windrose2-dev` active save worlds are under `Default/RocksDB/0.10.0/Worlds/F3B27E1F83434AF5A1BBA9B40E848A42` and `Default/RocksDB_v2/0.10.0/Worlds/F3B27E1F83434AF5A1BBA9B40E848A42`.

### Active save marker observations

Read-only string scans found player ids and character-related markers inside active RocksDB/SST files, not in standalone character files.

Examples from `windrose2-dev`:

- `RocksDB_v2/0.10.0/Worlds/F3B27E1F83434AF5A1BBA9B40E848A42/1403068.sst` contained the `Adventurer` player id.
- `RocksDB_v2/0.10.0/Worlds/F3B27E1F83434AF5A1BBA9B40E848A42/1403163.sst` contained the `Adventurer` player id plus `R5BLPlayerInWorld` / `MapFog` / `ScenarioSave` style context.
- `RocksDB_v2/0.10.0/Worlds/F3B27E1F83434AF5A1BBA9B40E848A42/1402365.sst` contained both the `Grittizban` and `Adventurer` player ids, showing that player data can co-reside inside one SST file.
- Other active SST files contained `Inventory`, `Equipment`, `Inventory.Item.Attribute.Level`, `ScenarioSave`, `WorldLocation`, `MapFog`, and many actor/building records interleaved with player markers.

### Cloneability decision for this pass

- By file: not safely cloneable. Both player records can appear in the same SST files and there is no one-file-per-character boundary.
- By raw SST edit: explicitly unsafe. SST files are immutable RocksDB table files and string offsets are not a validated write path.
- By RocksDB key/value export/import: not safe yet. The exact column families, key formats, rekey rules, and dependency graph for player/account/character/inventory/spawn records are not yet decoded.
- By game-managed/admin/plugin operation: still the preferred future path, but no game-managed stats/xp/inventory transfer operation is currently proven.

### Fixture decision

Do not copy stats, XP, inventory, equipment, hotbar, or spawn/location data from `Grittizban` to `Adventurer` yet. The read-only evidence distinguishes the two player ids, but it does not prove a safe portable subset. Copying values now would risk duplicating or damaging player identity, inventory ownership, map/scenario state, or world references.

The safe smoke-test option remains: use `Adventurer` as the non-main dev character for plugin/sidecar smoke tests on `windrose2-dev`, after a whole `SaveProfiles` backup, and only through validated game/plugin operations. Random online players remain read-only only.

### Commands used in this pass

```bash
ssh lancer1977@192.168.0.252 'python3 - <<PY
# read-only scan of windrose, windrose2, windrose2-dev logs and SaveProfiles
# searched for Grittizban, Adventurer, account/player ids, R5BLPlayer,
# R5BLPlayerInWorld, inventory/equipment/xp/level markers
PY'
```

Local/remote RocksDB command-line tools were checked and were not available (`ldb`, `rocksdb_ldb`, `sst_dump` were not installed), so this pass intentionally stayed at log parsing plus read-only byte/string marker scans.

## Current recommendation

For now: do whole SaveProfiles backup/copy, use `windrose2-dev`, and test with `Adventurer` or another non-main/throwaway character. Do not raw-clone one character by file or DB key until the save graph is mapped with real RocksDB tooling and proven on a disposable dev world.

The offline record-graph extractor now surfaces `R5BLPlayer`, `R5BLPlayerInWorld`, `R5BLAccount`, account/session ids, stats/progression, inventory/equipment/hotbar, and spawn/location references from copied checkpoint backups, but the current evidence still reports mixed identity + portable markers in the same SST entry, so selective character cloning remains blocked.

Next narrow card: upgrade the extractor from byte/string marker classification to real RocksDB key/value decoding with column-family enumeration, key-shape reporting, and disposable-world export/import proof. Until that exists, the decoded marker graph is useful evidence, but it is not sufficient to copy `Grittizban` stats/xp/inventory onto `Adventurer`.

## 2026-06-05 RocksDB tooling/manipulation exploration

A later safe exploration pass took a fresh whole-`SaveProfiles` backup before doing any work:

- backup: `/home/lancer1977/game_servers/windrose2-dev/backups/saveprofiles-before-manipulation-explore-20260605-135954.tar.gz`
- sha256: `0349faac2ea411f1a2d07e054b934d5b887114d80416e4124bf28b1f4f8e99c6`
- lab copy: `/home/lancer1977/game_servers/windrose2-dev/explore/rocksdb-manipulation-20260605-140013`

The lab extracted the latest checkpoint zip and built a matching RocksDB 10.4.2 tool image (`windrose-rocksdb-tools:10.4.2`) because Ubuntu's packaged `ldb`/`sst_dump` could enumerate column families but could not read these newer SSTs. The checkpoint uses RocksDB's shared-checksum backup shape, so the lab created scratch symlinks from `Checkpoint/private/1/<number>.sst` to `Checkpoint/shared_checksum/<number>_...sst` before opening the DB. This was done only inside the extracted checkpoint lab, never against the live save tree.

Real column families observed by `ldb list_column_families`:

- `default`
- `R5LargeObjects`
- `R5BLIsland`
- `R5BLBuilding`
- `R5BLIslandChest`
- `R5BLCrop`
- `R5BLActor_DamageableFoliage`
- `R5BLActor_DialogueActor`
- `R5BLActor_Drop`
- `R5BLActor_ExplodingBarrel`
- `R5BLDynamicGenericActor`
- `R5BLStaticGenericActor`
- `R5BLIslandShipDock`
- `R5BLActor_PickupResource`
- `R5BLPlayerInWorld`
- `R5BLActor_DigNode`
- `R5BLActor_DigVolume`
- `R5BLActor_MineralNode`
- `R5BLResourceSpawnPoint`
- `R5BLGameplaySpawner`
- `R5BLActorScenarioSave`
- `R5BLActor_BuildingBlock`

Important decoded shape:

- `R5BLPlayerInWorld` contains 13 records keyed by player-in-world document GUIDs, not by character name.
- The current Adventurer runtime log maps `Adventurer` to player id `4CBE496B1B1D403312845036F5B47E83` on island `F3B27E1F83434AF5A1BBA9B40E848A42`.
- The current lab found the Adventurer player id inside `R5BLPlayerInWorld` record key `734A988FA64A4DA1D9A6C95EC8E7ACF6`.
- Prior Grittizban logs map `Grittizban` to player id `0F98D8BA9EDE4EEFC99991252946228C`. The lab found that player id inside `R5BLPlayerInWorld` record key `47F1059AA5AD4353A0A9FC88839B383A`.
- The Grittizban/source record is large (`1,728,368` bytes) and includes `PlayerId`, `XP`, many `SpawnRecordId` references, `WorldLocation`, `MapFog`, `ScenarioSave`, and many quest markers.
- The Adventurer/destination record is much smaller (`1,786` bytes) and includes `PlayerId`, a few `SpawnRecordId` references, `WorldLocation`, `MapFog`, `ScenarioSave`, and quest markers.
- `R5LargeObjects` contains a very large record (`key 1`, `1,247,232` bytes) with `XP` and `Equipment` markers, so player manipulation may depend on large-object indirection rather than only a single `R5BLPlayerInWorld` row.

Candidate manipulation model, not approved for live use:

1. Stop the target dev server and work only on a disposable world copy.
2. Export the source `R5BLPlayerInWorld` value and destination `R5BLPlayerInWorld` value with matching RocksDB 10.4.2 tooling.
3. Treat the destination record key and destination `PlayerId` as identity that must be preserved.
4. If attempting a throwaway-only experiment, copy portable sections from source to destination only after field-level parsing proves which nested blocks are safe. Blindly copying the full source value over the destination key is unsafe because it would duplicate source `_guid`, `PlayerId`, spawn references, quest/map/scenario state, and possible large-object pointers.
5. Do not touch live `windrose2-dev`, production, or a player's main character until the disposable copy has survived restore, boot, login, readback, and rollback validation.

Current manipulation decision: still blocked for live/dev-player mutation. We now have real column-family and key/value evidence, but not enough field-level schema to safely edit Adventurer. The next safe step is a field parser for `R5BLPlayerInWorld` values plus reference tracing into `R5LargeObjects`, followed by a disposable-world no-op rewrite test before any semantic stat/xp/inventory change.
