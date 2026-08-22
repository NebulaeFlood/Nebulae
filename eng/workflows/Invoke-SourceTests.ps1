[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('linux', 'windows')][string] $OperatingSystem,
    [Parameter(Mandatory)][string] $ProjectsJson,
    [string] $VersionPrefix,
    [string] $VersionSuffix
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$group = (Get-Content -LiteralPath $ProjectsJson -Raw | ConvertFrom-Json).groups[0]
$testRuns = @(
    $group.testRuns |
        Where-Object {
            $_.stage -eq 'source' -and
            $OperatingSystem -cin @($_.operatingSystems)
        }
)

if ($testRuns.Count -eq 0) {
    throw "Group '$($group.cicd)' has no source-stage tests for '$OperatingSystem'."
}

foreach ($project in $group.projects) {
    & dotnet restore $project
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed for '$project'."
    }
}

$configurations = @(
    'Debug', 'Release' |
        Where-Object {
            $configuration = $_
            $null -ne ($testRuns | Where-Object { $configuration -cin @($_.configurations) } | Select-Object -First 1)
        }
)

foreach ($configuration in $configurations) {
    foreach ($project in $group.projects) {
        $arguments = @(
            'build'
            $project
            '--configuration'
            $configuration
            '--no-restore'
            '-p:GeneratePackageOnBuild=false'
        )
        if (-not [string]::IsNullOrWhiteSpace($VersionPrefix)) {
            $arguments += "-p:VersionPrefix=$VersionPrefix"
            $arguments += "-p:VersionSuffix=$VersionSuffix"
        }

        & dotnet @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for '$project' in '$configuration'."
        }
    }

    foreach ($testRun in $testRuns | Where-Object { $configuration -cin @($_.configurations) }) {
        & dotnet test $testRun.path `
            --configuration $configuration `
            --no-build `
            --no-restore `
            --verbosity normal
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed for '$($testRun.path)' in '$configuration' on '$OperatingSystem'."
        }
    }
}
