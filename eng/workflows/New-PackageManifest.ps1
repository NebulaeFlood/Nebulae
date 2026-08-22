[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ProjectsJson,
    [Parameter(Mandatory)][string] $PackageOutputDir,
    [Parameter(Mandatory)][string] $PackageVersion,
    [Parameter(Mandatory)][string] $OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$group = (Get-Content -LiteralPath $ProjectsJson -Raw | ConvertFrom-Json).groups[0]
$resolvedPackageOutput = [IO.Path]::GetFullPath($PackageOutputDir)
if (-not (Test-Path -LiteralPath $resolvedPackageOutput -PathType Container)) {
    throw "Package output directory '$resolvedPackageOutput' does not exist."
}

$packageFiles = @(
    Get-ChildItem -LiteralPath $resolvedPackageOutput -File |
        Where-Object {
            $_.Name.EndsWith('.nupkg', [StringComparison]::OrdinalIgnoreCase) -or
            $_.Name.EndsWith('.snupkg', [StringComparison]::OrdinalIgnoreCase)
        } |
        Sort-Object Name
)
$mainPackages = @(
    $packageFiles |
        Where-Object { -not $_.Name.EndsWith('.snupkg', [StringComparison]::OrdinalIgnoreCase) }
)
$expectedCount = @($group.packableProjects).Count
if ($mainPackages.Count -ne $expectedCount) {
    throw "Expected $expectedCount NuGet packages, found $($mainPackages.Count)."
}
if ($mainPackages.Count -eq 0) {
    throw "Group '$($group.cicd)' produced no NuGet packages."
}

$entries = @(
    foreach ($packageFile in $packageFiles) {
        $kind = if ($packageFile.Name.EndsWith('.snupkg', [StringComparison]::OrdinalIgnoreCase)) {
            'symbol'
        }
        else {
            'main'
        }

        [pscustomobject][ordered]@{
            name   = $packageFile.Name
            kind   = $kind
            length = $packageFile.Length
            sha256 = (Get-FileHash -LiteralPath $packageFile.FullName -Algorithm SHA256).Hash
        }
    }
)

$manifest = [pscustomobject][ordered]@{
    schemaVersion  = 1
    cicd           = $group.cicd
    packageVersion = $PackageVersion
    files          = @($entries)
}
$resolvedOutputPath = [IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
if ($outputDirectory) {
    $null = New-Item -ItemType Directory -Path $outputDirectory -Force
}
[IO.File]::WriteAllText(
    $resolvedOutputPath,
    ($manifest | ConvertTo-Json -Depth 5) + [Environment]::NewLine)

$entries | ForEach-Object { "$($_.kind): $($_.name) [$($_.sha256)]" }
