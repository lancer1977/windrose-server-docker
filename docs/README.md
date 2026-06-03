# Windrose Server Docker Docs

## Entry Points

- [`Features`](features/README.md) — feature-focused docs and capability maps.
- [`Roadmaps`](roadmaps/README.md) — phased backlog pages and completion notes.

## Features

- [Server State Observability](features/server-state-observability/README.md) - log, save, and checkpoint surfaces for exposing dedicated-server state.
- [Windrose Runtime Control Surface Map](features/server-state-observability/runtime-control-surface.md) - what WindrosePlus can observe, mutate, and still cannot do yet.
- [Windrose Runtime Possibility Atlas](roadmaps/windrose-runtime-control-surface/possibility-atlas.md) - the canonical inventory of current, deferred, and speculative runtime actions.
- [Windrose Runtime Control Surface Roadmap](roadmaps/windrose-runtime-control-surface/README.md) - the backlog for chat, spawn, mutation, and operator integration proof work.
- [Windrose Runtime Control Surface Execution Path](roadmaps/windrose-runtime-control-surface/execution-path.md) - feasible Lua/RCON and shared-contract slices.
- [Windrose Runtime Control Surface Operator Contract](roadmaps/windrose-runtime-control-surface/operator-contract.md) - the durable control-plane boundary for Windrose, ChannelCheevos, and Hermes.
- [Windrose ↔ ChannelCheevos Integration Test Design](roadmaps/windrose-runtime-control-surface/channel-cheevos-integration-tests.md) - the first end-to-end test matrix for chat, create, and Windrose event flows.
- [Public Exposure Readiness](roadmaps/public-exposure-readiness/README.md) - documentation scrub and release-hardening checklist for public sharing.
- [Windrose Core NuGet Publishing](roadmaps/windrose-core-nuget-publishing/README.md) - packaging and publish flow for the shared `Windrose.StateWeb.Core` contract layer.
- [Windrose Core Versioning](roadmaps/windrose-core-nuget-publishing/versioning-policy.md) - semantic versioning and release checklist for the shared contract package.
- [Windrose Core Consumer Note](roadmaps/windrose-core-nuget-publishing/downstream-consumer-note.md) - how downstream GitOps-managed runners should switch to the package.
- [Windrose Core 0.1.1 Release Checklist](roadmaps/windrose-core-nuget-publishing/release-tag-notes-0.1.1.md) - the ready-to-use checklist for the next Core package bump.
- [Windrose Core Checklist Template](roadmaps/windrose-core-nuget-publishing/release-tag-notes-template.md) - copy this for future Core package bumps.

## Roadmaps

- [Companion State Webserver](roadmaps/companion-state-webserver/README.md) - phased plan for a browser/API surface similar in spirit to the companion app.
- [Companion State Webserver Deployment Notes](roadmaps/companion-state-webserver/deployment.md) - current compose/env/deployment guidance for the sidecar.
- [Companion State Webserver Remaining Work](roadmaps/companion-state-webserver/remaining-work.md) - the current open-item sequence after the v3 observability pass.
- [Windrose Fork and Pipeline Path](roadmaps/windrose-fork-and-pipeline/README.md) - the fork/build/pipeline/cutover plan for operating Windrose from this repo.
- [Windrose Core NuGet Publishing](roadmaps/windrose-core-nuget-publishing/README.md) - GitHub Actions packaging and publish flow for the shared `Windrose.StateWeb.Core` library.

## Prompts

- [Windrose Goal Prompt Pack](plans/2026-05-28-windrose-goal-prompts.md) - ready-to-copy goal prompts for finishing Windrose work, validating deployment, and deciding whether deeper decoding is worth shipping.
