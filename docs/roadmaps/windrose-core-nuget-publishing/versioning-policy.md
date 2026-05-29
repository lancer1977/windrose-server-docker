# Windrose.StateWeb.Core versioning

## Purpose

Keep package releases predictable for downstream consumers that restore `Windrose.StateWeb.Core` from nuget.org.

## Source of truth

- Package id: `Windrose.StateWeb.Core`
- Published from: `docs/roadmaps/windrose-core-nuget-publishing/` GitHub Actions workflow
- Release trigger: GitHub release publication or manual workflow dispatch

## Version rules

- Use semantic versioning for published package versions: `MAJOR.MINOR.PATCH`.
- Release tags may use a leading `v` for convenience, but the workflow strips it before packing.
- Increment `PATCH` for documentation-only, packaging-only, or compatibility-safe fixes.
- The current published version is `0.1.0`.
- The next release target is `0.1.1`, which carries the shared time-series source/context helpers.
- Increment `MINOR` for additive contract changes.
- Increment `MAJOR` for breaking contract changes.

## Release checklist

1. Confirm the core project still builds and tests cleanly.
2. Create a release tag that matches the intended version.
3. Ensure the `NUGET_API_KEY` repository secret is present.
4. Let the GitHub Actions workflow pack and publish the package.
5. Verify the package page and version index on nuget.org.
6. Update any downstream consumer notes if the contract surface changed.

## Consumer guidance

- Downstream repos should pin to a stable package version.
- Consumers that use Central Package Management should place the version in `Directory.Packages.props`.
- Switch consumers only after they can restore from nuget.org or an approved mirror.

## Notes

- The package release flow is intentionally separate from Docker image deployment.
- GitOps-managed runners should treat the package as a restore-time dependency, not a deployment artifact.
