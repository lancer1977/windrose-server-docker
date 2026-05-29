# Windrose Runtime Control Surface Phases

## Phase 1 - Capability Inventory

Goal: confirm what WindrosePlus already exposes versus what still needs reverse-engineering.

- [x] Confirm the observer layer is read-only
- [x] Confirm WindrosePlus has an RCON command registry
- [x] Confirm `registerHookWhenAvailable` exists
- [x] Confirm custom commands can be added with `API.registerCommand(...)`
- [x] Confirm `wp.tp` exists
- [x] Confirm `wp.speed` exists
- [x] Confirm `wp.reload` exists
- [x] Confirm `wp.mapgen` / `wp.mapexport` exist
- [x] Confirm `wp.say` is deferred / not yet a stable API
- [x] Confirm there is no documented spawn command in the reviewed docs

## Phase 2 - External Broadcast Spike

Goal: prove whether a safe external broadcast path exists.

- [x] Inspect WindrosePlus hooks for an existing chat or announcement API
- [x] Search for native methods that can send server-side messages
- [x] Confirm the current Lua-only surface cannot send chat without a native hook
- [x] Confirm the reviewed upstream docs do not expose a first-class chat-send API
- [x] Record the command name, arguments, permission model, and rollback behavior if a future native hook makes it possible

## Phase 3 - Spawn / Entity Injection Spike

Goal: prove whether the server can spawn a controlled entity, NPC, or enemy through WindrosePlus.

- [x] Inspect hooks and functions that create actors or spawn pawns
- [x] Search for existing mod examples that spawn entities
- [x] Attempt a minimal proof-of-concept spawn path in a disposable test environment
- [x] Decide whether spawn belongs in a mod, a command, or a native extension
- [x] Record any safe spawn contract, including limits and audit logging

## Phase 4 - Controlled World Mutation Contract

Goal: define the smallest safe set of live mutations we are willing to support.

- [x] Choose the initial supported mutation types
- [x] Define operator-only permissions
- [x] Define audit logging for every mutation
- [x] Define failure and rollback behavior
- [x] Define the separation between read-only state-web and write-capable WindrosePlus paths

## Phase 5 - Operator Integration

Goal: expose approved runtime actions to external operator clients without making them the transport layer.

- [x] Define a read-only runtime control-surface summary endpoint for operator clients
- [x] Define the ChannelCheevos hub/API contract for approved actions
- [x] Define how Hermes surfaces approval, rejection, and revocation
- [x] Define the client-side identity/session boundaries for Windrose instances
- [x] Add documentation for who can request, who can approve, and who can execute
- [x] Keep the mutation path auditable and revocable

The control-plane ownership draft lives in `operator-contract.md` and should be treated as the canonical design note for future implementation work.
