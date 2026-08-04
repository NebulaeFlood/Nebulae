# CI/CD Configuration

[简体中文](../zh-CN/ci-cd.md)

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

## Adding a project

1. Add a production or test project to `Nebulae.slnx`.
2. Omit `CICD` to use the `default` group, or declare a custom group when the project requires an independent pipeline.
3. Use `<CICD>none</CICD>` only when the project should remain in the solution but must never participate in CI/CD.
4. Keep auxiliary `.csproj` files that are invoked by tests or build tooling out of the solution.
5. Run the discovery commands below before committing.

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
