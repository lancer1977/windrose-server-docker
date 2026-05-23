# Windrose Core NuGet Publishing

## Purpose

Publish the shared `Windrose.StateWeb.Core` contract layer to nuget.org from GitHub Actions so downstream repos can consume the payloads and interfaces without taking a sibling checkout dependency.

This release track is intentionally separate from deployment:

- GitHub Actions owns packaging and publish
- Azure DevOps or GitOps can still handle deployment independently
- the in-repo app can keep its local `ProjectReference` to the core project

## Current Status

- [x] The core project is packable and has package metadata
- [x] A GitHub Actions workflow exists for release-time publish
- [x] The package README documents the public contract surface
- [ ] nuget.org API key secret is configured in GitHub for future automated releases
- [x] First package publish has been completed successfully
- [ ] At least one downstream consumer has switched to the package

## What Is In Scope

- [x] Package metadata
- [x] GitHub Actions release builder
- [x] NuGet.org publish command
- [x] README/package docs
- [ ] Consumer migration guidance
- [ ] Release notes / versioning policy
- [ ] GitHub secret wiring for fully automated future releases

## What Is Not In Scope

- [ ] Replacing local project references inside this repo
- [ ] Changing the server deployment strategy
- [ ] Moving the Docker image release process
- [ ] Adding GitOps requirements for package publication
