# Windrose Core NuGet Publishing

## Purpose

Publish the shared `Windrose.StateWeb.Core` contract layer to nuget.org from GitHub Actions so downstream repos can consume the payloads and interfaces without taking a sibling checkout dependency.

This release track is intentionally separate from deployment:

- GitHub Actions owns packaging and publish
- Azure DevOps or GitOps can still handle deployment independently
- the in-repo app stays on a local project reference so the repo can build against the shared source while the package tracks release readiness

## Current Status

- [x] The core project is packable and has package metadata
- [x] A GitHub Actions workflow exists for release-time publish
- [x] The package README documents the public contract surface
- [x] The nuget.org API key secret is configured in GitHub for future automated releases
- [x] First package publish has been completed successfully
- [x] The package page and version indexing are confirmed on nuget.org
- [x] A downstream consumer note exists for package migration
- [x] At least one downstream consumer has switched to the package
- [x] A release notes / versioning policy doc exists
- [x] Release/tag notes for 0.1.1 exist
- [x] Release/tag notes template exists for future Core package bumps

## What Is In Scope

- [x] Package metadata
- [x] GitHub Actions release builder
- [x] NuGet.org publish command
- [x] README/package docs
- [x] Consumer migration guidance
- [x] Release notes / versioning policy
- [x] Release/tag notes for 0.1.1
- [x] Release/tag notes template for future Core package bumps
- [x] GitHub secret wiring for fully automated future releases

## What Is Not In Scope

- changing the server deployment strategy
- moving the Docker image release process
- adding GitOps requirements for package publication
