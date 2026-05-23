# Windrose.StateWeb.Core

`Windrose.StateWeb.Core` is the reusable contract package for Windrose observability and overlay consumers.

It carries the shared payloads, response models, interfaces, and extension helpers used by:

- the Windrose state-web sidecar
- browser overlays and observer tooling
- `channel-cheevos` and other downstream consumers that should not depend on the server repo as a sibling checkout

Contents include:

- timeline/history records
- overlay snapshots
- time-series exports
- source abstractions for history and overlay snapshots
- extension helpers for turning live state into readable surfaces

Install it from NuGet in downstream repos:

```shell
dotnet add package Windrose.StateWeb.Core
```

The package is published from GitHub Actions to nuget.org when a release is created.
