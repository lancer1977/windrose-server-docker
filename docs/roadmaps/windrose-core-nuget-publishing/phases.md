# Windrose Core NuGet Publishing Phases

## Phase 1 - Package readiness

- [x] Make the core library packable
- [x] Add package metadata
- [x] Add a package README
- [x] Keep the local app on a project reference

## Phase 2 - GitHub builder

- [x] Add a GitHub Actions workflow that restores, tests, packs, and publishes the package
- [x] Trigger publish from release publication or manual dispatch
- [x] Pull the package version from the release tag or workflow input
- [x] Fail fast if the NuGet secret is missing

## Phase 3 - Release execution

- [ ] Configure the `NUGET_API_KEY` repository secret for future automated releases
- [x] Publish the first package to nuget.org
- [ ] Confirm the package page and version indexing on nuget.org
- [ ] Write a short downstream consumer note for the next repo that adopts it
