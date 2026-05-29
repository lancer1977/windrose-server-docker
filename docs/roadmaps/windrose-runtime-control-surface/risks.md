# Windrose Runtime Control Surface Risks

## Technical Risks

- Chat broadcast may require native methods that Lua-only UE4SS cannot call.
- Spawn or entity creation may require engine-specific hooks that are brittle across game updates.
- Overly broad mutation tooling could destabilize the server or break save data.
- A write-capable API can become a security boundary if permissions are not explicit.

## Operational Risks

- Operators may assume a capability is production-safe just because it exists in a dev mod.
- Without audit logging, live actions are hard to trace after the fact.
- If the write layer and read layer are mixed, the observer UI can become harder to trust.
- If approval/revocation is not explicit, Hermes or other clients could become unintended control planes.

## Mitigations

- Keep the observer stack read-only.
- Put every runtime mutation behind an explicit command, hook, or API.
- Log each live action with actor, target, reason, and timestamp.
- Treat proof-of-concept features as experimental until they are documented and tested.
- Keep ChannelCheevos as the approval/control authority and WindrosePlus as the execution surface.
