# CI/CD Configuration

## Project discovery

`Nebulae.slnx` is the source of truth for projects that belong to the repository. The discovery script reads only `.csproj` entries from the solution. Auxiliary projects, such as build scenarios and test fixtures, are not CI/CD projects unless they are added to the solution.

Projects in the solution participate in CI/CD by default. A solution project that is intended only for local use can opt out with the reserved `none` value:

```xml
<PropertyGroup>
  <CICD>none</CICD>
</PropertyGroup>
```

The discovery script reads this value directly from the project XML and skips the project before MSBuild evaluation. `none` is not a runnable CI/CD group. A change confined to a `none` project's directory does not schedule a group. If a participating project explicitly consumes a file from that directory, the consuming project's input rule still schedules its group.

## CI/CD groups

Projects without a `CICD` property belong to the implicit `default` group:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

Use a static `CICD` property to assign a project to another group:

```xml
<PropertyGroup>
  <CICD>inline-il-package</CICD>
</PropertyGroup>
```

Group names may contain lowercase ASCII letters, digits, and hyphens. `default` and `none` are reserved values. Do not add a `Condition` to the `CICD` property, and declare it no more than once in a project file.

After static validation, the discovery script evaluates `CICD`, `IsPackable`, and `IsTestProject` with MSBuild. The evaluated `CICD` value must match the value declared directly in the project file. This prevents imports, environment variables, or repository-wide build configuration from changing a project's group implicitly.

## Test execution metadata

Test projects can statically declare their operating systems, configurations, and pipeline stage:

```xml
<PropertyGroup>
  <CICDOperatingSystems>linux;windows</CICDOperatingSystems>
  <CICDConfigurations>Debug;Release</CICDConfigurations>
  <CICDStage>source</CICDStage>
</PropertyGroup>
```

The supported operating systems are `linux` and `windows`; the workflow maps them to its runner labels. The supported configurations are `Debug` and `Release`. `CICDStage` must be either `source` or `package`:

- `source` tests build and test the current source tree on every declared operating system and configuration.
- `package` tests run after packing and consume the exact candidate package downloaded from the workflow artifact.

The defaults are `linux`, `Release`, and `source`. These properties are valid only on test projects. Like `CICD`, an explicitly declared value must be unconditional, appear no more than once, and evaluate to the exact static value in the project file.

A group with package-stage tests must provide `eng/workflows/hooks/<group>-package.ps1`. The hook receives the discovered test plan and candidate package directory, and adapts that package to the group's consumer tests. CI and CD both build one candidate package on Linux, record every package file's name, length, and SHA-256 in a manifest, and validate that same artifact on every declared package-stage operating system. CD publishes only the manifest-listed packages after every package-stage job succeeds.

## Execution flow

### CI

CI runs for pushes and pull requests targeting `master`, and it can also be started manually. A newer run for the same pull request or branch cancels an unfinished older run.

1. `CI Orchestrator` compares the relevant commits and calculates the affected CI/CD groups; manual runs, initial pushes without a usable previous commit, and global changes run every group.
2. Each group first runs `discover` to validate project metadata and produce the operating-system matrices for the source and package stages.
3. If the group has source-stage tests, `source-test` invokes `Invoke-SourceTests.ps1` on every declared operating system to restore, build, and test the current source in the declared Debug and Release configurations.
4. If the group has package-stage tests and the source stage succeeded or was skipped, `pack` builds one temporary-version Release candidate and package manifest on Linux, then uploads them as a workflow artifact.
5. `package-test` downloads that same candidate artifact on every declared operating system, verifies its group, version, file names, lengths, and SHA-256 hashes, and then invokes `eng/workflows/hooks/<group>-package.ps1` to run package-consumer tests.

CI never publishes packages. A stage and its stage-specific downstream jobs are skipped when the group declares no tests for that stage.

### CD

CD runs from a release tag and can also be started manually for an existing release tag. `v1.2.3` selects the `default` group, while `<group>-v1.2.3` selects a named group; the version in the tag is also the NuGet package version.

1. `CD Orchestrator` resolves and validates the tag, group, version, and release source ref; release runs for the same group and version do not cancel each other.
2. `discover` creates the test plan from the source referenced by the release tag, after which the available source-stage tests run with the same matrix behavior as CI.
3. After the source stage succeeds or is skipped, `pack` builds every project in Release on Linux, packs every packable project in the group, creates the package manifest, and uploads the single release-candidate artifact.
4. If the group has package-stage tests, `package-test` downloads, verifies, and consumes that candidate artifact through the group hook on every declared operating system.
5. After the source and package stages succeed or are skipped, `publish` downloads the same candidate artifact, verifies its manifest again, and uses NuGet Trusted Publishing to publish only the `.nupkg` files marked as main packages in the manifest.

The publish command does not ignore duplicate versions, so CD fails explicitly when the version already exists on NuGet.org.

## Adding a project

1. Add a production or test project to `Nebulae.slnx`.
2. Omit `CICD` to use the `default` group, or declare a custom group when the project requires an independent pipeline.
3. Use `<CICD>none</CICD>` only when the project should remain in the solution but must never participate in CI/CD.
4. On a test project, declare `CICDOperatingSystems`, `CICDConfigurations`, or `CICDStage` only when its execution contract differs from the defaults.
5. Keep auxiliary `.csproj` files that are invoked by tests or build tooling out of the solution.
6. Run the discovery commands below before committing.

Changes to `Nebulae.slnx`, the workflow definitions, or the discovery scripts are treated as global changes and cause all discovered groups to run.

## Local validation

Display all discovered projects and groups:

```powershell
./eng/workflows/Get-Projects.ps1
```

Produce machine-readable output:

```powershell
./eng/workflows/Get-Projects.ps1 -Json
```

Inspect one custom group:

```powershell
./eng/workflows/Get-Projects.ps1 -CICD inline-il-package
```

List every group that the orchestrator can run:

```powershell
./eng/workflows/Get-DifferentGroups.ps1 -All -Json
```
