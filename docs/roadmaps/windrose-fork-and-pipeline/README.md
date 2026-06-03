# Windrose Fork, Build, and Pipeline Path

## Purpose

This roadmap captures the path for treating this repository as the maintained Windrose fork, building it from source, publishing images from the fork, and then switching the live deployment to consume that forked build.

The goal is not to turn Windrose State Web into a write-capable surface. The goal is to keep the observer/capture stack healthy, keep SignalR live updates working, and leave room for a separate WindrosePlus fork only if we later prove we need lower-level hook access for live event or spawn work.

## Completion Goal

This roadmap is complete when:

- the repo is clearly operated as a fork of upstream Windrose
- local builds pass from the forked source tree
- the pipeline produces a tagged build artifact or container image from this fork
- the deployment points at that forked build instead of an upstream image
- the SignalR/state-capture path still works end to end after the switch
- any future WindrosePlus work remains a separate, explicitly scoped fork decision

## Verified Baseline

These steps have already been proven in the forked tree:

- `git remote add upstream git@github.com:indifferentbroccoli/windrose-server-docker.git`
- `dotnet test WindroseServerDocker.slnx --configuration Release -p:RestoreIgnoreFailedSources=true`
- `dotnet pack src/Windrose.StateWeb.Core/Windrose.StateWeb.Core.csproj --configuration Release -p:RestoreIgnoreFailedSources=true -p:PackageVersion=0.1.1`
- `docker build -f src/Windrose.StateWeb/Dockerfile -t windrose-state-web:fork-check .`

Observed result:

- the solution test run passed: 96 tests, 0 failures
- `Windrose.StateWeb.Core.0.1.1.nupkg` was created under `src/Windrose.StateWeb.Core/bin/Release/`
- the state-web container image built successfully as `windrose-state-web:fork-check`
- the repo now carries an explicit `upstream` remote for rebases and comparisons

## Roadmap to Completion

1. **Establish fork posture**
   - Confirm the canonical upstream repo.
   - Keep this repository as the working fork for Windrose server-side changes.
   - Add or document the upstream remote so changes can be merged or rebased cleanly.
   - Decide the branch policy for the fork so `main` stays deployable.

2. **Prove local build parity**
   - Run the repo’s normal .NET path from the fork: `dotnet test WindroseServerDocker.slnx --configuration Release -p:RestoreIgnoreFailedSources=true`.
   - Verify the shared core package path with `dotnet pack src/Windrose.StateWeb.Core/Windrose.StateWeb.Core.csproj --configuration Release -p:RestoreIgnoreFailedSources=true -p:PackageVersion=0.1.1`.
   - Build the state-web container from `src/Windrose.StateWeb/Dockerfile` so the container path is exercised from the forked tree as well.
   - Smoke the read-only endpoints and the SignalR hub contract.
   - Fix any fork-specific build drift before touching the pipeline.

3. **Add or harden the fork pipeline**
   - Extend the existing package workflow in `.github/workflows/publish-core-nuget.yml` and add a server-image workflow that builds from the forked root `Dockerfile`/sidecar `src/Windrose.StateWeb/Dockerfile` as appropriate.
   - Produce a container image or release artifact from forked source.
   - Keep publish credentials and deployment secrets in the target system, not in the repo.
   - Tag the image/artifact in a way that makes the fork origin obvious.

4. **Cut deployment over to the fork build**
   - Point the live environment or Portainer stack at the fork-produced image tag.
   - Keep a rollback path to the previous known-good tag.
   - Validate the live container against the expected API and SignalR routes.
   - Confirm the forked build is the one actually running in the container.

5. **Separate WindrosePlus hook work from the observer fork**
   - Keep capture/state work in this repo.
   - Evaluate a WindrosePlus fork only if we prove we need raw hook access for event injection, enemy spawning, or similar native behaviors.
   - If that need becomes real, document the WindrosePlus fork path as its own roadmap slice.

## Phase Breakdown

### Phase 1 - Fork setup

- [ ] Confirm upstream repo and branch strategy
- [ ] Add or document the upstream remote
- [ ] Define the fork’s default branch and update policy
- [ ] Record how fork merges/rebases will be handled

### Phase 2 - Local build parity

- [ ] Run the fork’s local build path
- [ ] Verify the shared core project still compiles
- [ ] Verify the state web project still compiles
- [ ] Smoke the read-only API and SignalR routes locally

### Phase 3 - Pipeline

- [ ] Add or harden GitHub Actions for build/test/package
- [ ] Publish a forked image or artifact from CI
- [ ] Make the fork origin obvious in artifact tags and release notes
- [ ] Keep secrets and deployment credentials out of source control

### Phase 4 - Deployment cutover

- [ ] Point Portainer or GitOps at the fork-produced build
- [ ] Verify the live container is running the fork artifact
- [ ] Smoke the key observer endpoints in the deployed environment
- [ ] Capture rollback instructions before promoting the switch

### Phase 5 - Future WindrosePlus fork decision

- [ ] Prove the exact hook or native capability that is still missing
- [ ] Decide whether upstream WindrosePlus can cover it
- [ ] Fork WindrosePlus only if raw code access is justified
- [ ] Keep the WindrosePlus fork scope narrow and documented

## Completion Gate

This roadmap is done when all of the following are true:

- the fork relationship is documented and stable
- CI or release automation builds the forked source
- deployment consumes the forked artifact/image
- SignalR/live state capture still works after the cutover
- the next write-capable or hook-heavy idea has an explicit fork decision instead of being implied

## Related Docs

- `README.md` in the repo root
- `docs/README.md`
- `docs/features/README.md`
- `docs/roadmaps/README.md`
- `docs/features/server-state-observability/README.md`
- `docs/features/server-state-observability/runtime-control-surface.md`
- `docs/roadmaps/windrose-runtime-control-surface/README.md`
