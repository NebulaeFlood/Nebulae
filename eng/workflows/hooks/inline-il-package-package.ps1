[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('linux', 'windows')][string] $OperatingSystem,
    [Parameter(Mandatory)][string] $ProjectsJson,
    [Parameter(Mandatory)][string] $PackageOutputDir,
    [Parameter(Mandatory)][string] $PackageVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$group = (Get-Content -LiteralPath $ProjectsJson -Raw | ConvertFrom-Json).groups[0]
if ($group.cicd -ne 'inline-il-package') {
    throw "The Inline IL package hook received group '$($group.cicd)'."
}

$testRuns = @(
    $group.testRuns |
        Where-Object {
            $_.stage -eq 'package' -and
            $OperatingSystem -cin @($_.operatingSystems)
        }
)
if ($testRuns.Count -eq 0) {
    throw "The Inline IL package hook found no package-stage tests for '$OperatingSystem'."
}

$resolvedPackageOutput = [IO.Path]::GetFullPath($PackageOutputDir)
$packagePath = Join-Path $resolvedPackageOutput "Nebulae.Runtime.Emit.Inline.$PackageVersion.nupkg"
if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "Inline IL package '$packagePath' was not found."
}

$previousPackagePath = $env:NEBULAE_INLINE_IL_PACKAGE_PATH
$previousPackageVersion = $env:NEBULAE_INLINE_IL_PACKAGE_VERSION

try {
    $env:NEBULAE_INLINE_IL_PACKAGE_PATH = $packagePath
    $env:NEBULAE_INLINE_IL_PACKAGE_VERSION = $PackageVersion

    foreach ($testRun in $testRuns) {
        & dotnet restore $testRun.path
        if ($LASTEXITCODE -ne 0) {
            throw "Restore failed for package-stage test '$($testRun.path)'."
        }

        foreach ($configuration in @($testRun.configurations)) {
            & dotnet test $testRun.path `
                --configuration $configuration `
                --no-restore `
                --verbosity normal `
                -p:GeneratePackageOnBuild=false
            if ($LASTEXITCODE -ne 0) {
                throw "Package-stage test '$($testRun.path)' failed in '$configuration' on '$OperatingSystem'."
            }
        }
    }
}
finally {
    $env:NEBULAE_INLINE_IL_PACKAGE_PATH = $previousPackagePath
    $env:NEBULAE_INLINE_IL_PACKAGE_VERSION = $previousPackageVersion
}
