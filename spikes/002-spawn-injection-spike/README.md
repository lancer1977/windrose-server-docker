# Spike 002: Enemy / Entity Spawn Feasibility

## Question

Can WindrosePlus create a controlled enemy, NPC, or other spawned entity through a documented command or hook?

## Why this matters

If spawning is supported, stream interactions could drive live events, waves, or controlled encounters from outside the game server.

## Research approach

- Inspect WindrosePlus docs for any spawn, summon, or entity-creation command.
- Look for hooks or native-method references that suggest a spawn path.
- Treat this as throwaway feasibility work, not production design.

## Evidence reviewed

- `WindrosePlus/Scripts/modules/admin.lua`
- `docs/commands.md`
- `docs/scripting-guide.md`
- `WindrosePlus/Scripts/main.lua`

## Findings

- The docs contain `wp.creatures` and `wp.entities`, but those are observation/diagnostic commands that count existing world objects.
- I did not find a documented first-class spawn or summon command in the reviewed docs.
- The admin docs show many runtime mutations, but spawn is not one of the documented stable admin actions.
- The current docs make it reasonable to suspect a native hook could eventually expose spawn behavior, but no such hook was confirmed in this spike.

## Verdict: INVALIDATED

The current documented WindrosePlus surface does not provide a supported spawn or entity-creation command.

## What worked

- The docs make a clean separation between world inspection and world mutation.
- `wp.creatures` and `wp.entities` are useful diagnostics and prove that world-state enumeration is already available.

## What didn't

- No first-class spawn/summon command was found.
- No concrete entity-creation hook was confirmed in the reviewed surface.

## Surprise

The docs have richer world diagnostics than expected, but those diagnostics stop at counting and listing existing world objects rather than creating new ones.

## Recommendation for the real build

- Keep spawn behavior out of the observer layer.
- If spawn is still desired, the next step is a native hook investigation or a mod example that explicitly creates actors/pawns.
- Do not promise spawn in the operator contract until a safe command or hook is proven.
