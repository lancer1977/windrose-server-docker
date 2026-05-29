# Windrose.StateWeb.Core 0.1.1 release checklist

## Purpose

Use this note when cutting the next `0.1.1` release tag and publishing the package from GitHub Actions.

## What will change in 0.1.1

- added shared time-series source/context helpers in `Windrose.StateWeb.Core`
- kept the package focused on shared payload shapes and pure transforms
- preserved the downstream consumer boundary for `channel-cheevos` and similar consumers

## Release steps

1. Confirm the repo builds and tests pass.
2. Create or update the release tag to `v0.1.1`.
3. Let the GitHub Actions publish workflow resolve the tag version and pack the core project.
4. Verify the package page/version index on nuget.org after publish.
5. Keep deployment/GitOps changes separate from this package release.

## Validation checkpoints

- `dotnet test WindroseServerDocker.slnx --configuration Release -p:RestoreIgnoreFailedSources=true`
- `dotnet pack src/Windrose.StateWeb.Core/Windrose.StateWeb.Core.csproj --configuration Release -p:RestoreIgnoreFailedSources=true -p:PackageVersion=0.1.1`
- nuget.org registration for `Windrose.StateWeb.Core/0.1.1`

## Consumer reminder

Downstream repos should update to the new package version only after they are ready to restore from nuget.org or an approved mirror.

For the exact migration shape and GitOps runner notes, see:

- `docs/roadmaps/windrose-core-nuget-publishing/downstream-consumer-note.md`
- `docs/roadmaps/windrose-core-nuget-publishing/versioning-policy.md`
