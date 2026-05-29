# Windrose.StateWeb.Core

`Windrose.StateWeb.Core` is the reusable contract package for Windrose observability and overlay consumers.

It is the canonical home for the shared data shapes and pure transforms that both the server app and downstream consumers should agree on.

It carries the shared payloads, response models, interfaces, and extension helpers used by:

- the Windrose state-web sidecar
- browser overlays and observer tooling
- `channel-cheevos` and other downstream consumers that should not depend on the server repo as a sibling checkout

Contents include:

- timeline/history records
- overlay snapshots
- time-series exports
- time-series window/context helpers
- source abstractions for history and overlay snapshots
- extension helpers for turning live state into readable surfaces

Install it from NuGet in downstream repos:

```shell
dotnet add package Windrose.StateWeb.Core
```

- the current published package is `0.1.0`
- `0.1.1` is the next release target and carries the shared time-series source/context helpers.

The package is published from GitHub Actions to nuget.org when a release is created.

See `docs/roadmaps/windrose-core-nuget-publishing/downstream-consumer-note.md` for the recommended migration shape and GitOps runner notes.
