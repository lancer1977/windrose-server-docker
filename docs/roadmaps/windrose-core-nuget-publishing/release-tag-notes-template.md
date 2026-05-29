# Windrose.StateWeb.Core release checklist template

## Purpose

Use this template when cutting a new `Windrose.StateWeb.Core` release tag and publishing the package from GitHub Actions.

Copy this file, rename it to the target version, and replace the bracketed placeholders.

## Release metadata

- Package id: `Windrose.StateWeb.Core`
- Release target: `[VERSION]`
- Release tag: `v[VERSION]`
- Publish path: GitHub Actions release workflow in this repo

## What changed in [VERSION]

- [Describe the package/API/doc changes in one or two bullets.]
- [Call out any new shared contracts, helpers, or consumer-facing behavior.]
- [Call out anything that should make a downstream consumer re-check its mappings.]

## Release steps

1. Confirm the repo builds and tests pass.
2. Create or update the release tag to `v[VERSION]`.
3. Let the GitHub Actions publish workflow resolve the tag version and pack the core project.
4. Verify the package page/version index on nuget.org after publish.
5. Keep deployment/GitOps changes separate from this package release.

## Validation checkpoints

- `dotnet test WindroseServerDocker.slnx --configuration Release -p:RestoreIgnoreFailedSources=true`
- `dotnet pack src/Windrose.StateWeb.Core/Windrose.StateWeb.Core.csproj --configuration Release -p:RestoreIgnoreFailedSources=true -p:PackageVersion=[VERSION]`
- nuget.org registration for `Windrose.StateWeb.Core/[VERSION]`

## Consumer reminder

Downstream repos should update to the new package version only after they are ready to restore from nuget.org or an approved mirror.

For the exact migration shape and GitOps runner notes, see:

- `docs/roadmaps/windrose-core-nuget-publishing/downstream-consumer-note.md`
- `docs/roadmaps/windrose-core-nuget-publishing/versioning-policy.md`
