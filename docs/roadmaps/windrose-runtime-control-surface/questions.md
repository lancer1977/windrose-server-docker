# Windrose Runtime Control Surface Questions

## Chat

- What exact UE4SS or native method can deliver a server-side chat message?
- Can a message be sent without impersonating a player?
- Should this be a global announcement, a system message, or a player-targeted chat line?

## Spawn / Entity Control

- What UE4 function actually creates the desired entity or enemy?
- Does the game already expose a safe admin or debug spawn hook?
- What limits are needed so a spawn tool cannot destabilize the server?

## Mutation Contract

- Which runtime actions are safe enough to support long term?
- Which actions should stay experimental or operator-only?
- Which actions belong in WindrosePlus and which belong in ChannelCheevos?

## Integration

- How should ChannelCheevos approve or revoke live mutation privileges?
- What should Hermes display versus what should Windrose execute directly?
- Do we need separate contracts for chat, spawn, and world mutation, or one shared operator action model?
