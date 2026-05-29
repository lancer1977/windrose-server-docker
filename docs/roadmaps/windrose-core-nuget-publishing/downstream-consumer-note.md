# Windrose.StateWeb.Core consumer note

Use this when a downstream repo wants to consume the Windrose contract layer from NuGet instead of a sibling checkout.

## Source of truth

- Package id: `Windrose.StateWeb.Core`
- Current published version: `0.1.0`
- Next release target: `0.1.1`
- Published from: GitHub Actions release workflow in this repo
- Intended use: shared contract surface for Windrose observability, overlays, and GitOps-managed runners that need the payload models without depending on the server repo tree

## Migration shape

Replace the local project reference with a package reference in the consuming repo:

```xml
<ItemGroup>
  <PackageReference Include="Windrose.StateWeb.Core" Version="0.1.0" />
</ItemGroup>
```

If the consumer uses Central Package Management, move the version to `Directory.Packages.props` instead of pinning it in the project file.

## GitOps runner notes

- The runner only needs outbound access to `https://api.nuget.org/v3/index.json` or to an internal mirror that republishes the package.
- Keep deployment GitOps and package publishing separate: the NuGet package ships from release automation, while the runner only restores and builds against the published contract.
- If your runner is locked down, add the NuGet source and any proxy or mirror credentials before switching the reference.

## Confirmed downstream consumer

ChannelCheevos is the first confirmed downstream consumer of the shared package boundary.

If the consumer is ChannelCheevos, the package boundary should look like this:

```xml
<ItemGroup>
  <PackageReference Include="Windrose.StateWeb.Core" Version="0.1.0" />
</ItemGroup>
```

ChannelCheevos is currently pinned to the released `0.1.0` package in-repo; update that pin to `0.1.1` after the next package publish.
Use the following types from the package instead of rebuilding the contract locally:

- `WindroseOverlaySnapshot`
- `WindroseOverlaySnapshotContext`
- `WindroseHistoryExport`
- `WindroseTimeSeriesExport`
- `WindroseTimeSeriesWindow`
- `WindroseTimelineEntry`
- `IWindroseOverlaySnapshotSource`
- `IWindroseHistorySource`
- `IWindroseTimeSeriesSource`
- `WindroseSurfaceExtensions`

What to delete from the consumer once the package is in place:

- local DTO copies for the Windrose overlay/history/time-series/timeline shapes
- local mapping helpers that mirror `WindroseSurfaceExtensions`
- ad hoc payload reconstruction logic for the same shared contract surface

## Recommended verification

1. Restore the consumer repo.
2. Build the consumer repo against the package reference.
3. Run the consumer repo tests or smoke checks that exercise the contract types.
4. Confirm the consuming app no longer needs the Windrose server repository as a sibling checkout.

## When not to switch yet

- The consumer still needs to edit the shared contracts directly.
- The downstream repo has not been updated to a stable package version policy.
- The runner cannot reach the package source and there is no mirror available.
