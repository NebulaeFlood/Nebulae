[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $PackageOutputDir,
    [Parameter(Mandatory)][string] $ManifestPath,
    [Parameter(Mandatory)][string] $ExpectedCICD,
    [Parameter(Mandatory)][string] $ExpectedPackageVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedPackageOutput = [IO.Path]::GetFullPath($PackageOutputDir)
$resolvedManifestPath = [IO.Path]::GetFullPath($ManifestPath)
if (-not (Test-Path -LiteralPath $resolvedManifestPath -PathType Leaf)) {
    throw "Package manifest '$resolvedManifestPath' does not exist."
}

$manifest = Get-Content -LiteralPath $resolvedManifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1) {
    throw "Unsupported package manifest schema '$($manifest.schemaVersion)'."
}
if ($manifest.cicd -cne $ExpectedCICD) {
    throw "Package manifest group '$($manifest.cicd)' does not match '$ExpectedCICD'."
}
if ($manifest.packageVersion -cne $ExpectedPackageVersion) {
    throw "Package manifest version '$($manifest.packageVersion)' does not match '$ExpectedPackageVersion'."
}

$entries = @($manifest.files)
if ($entries.Count -eq 0) {
    throw 'Package manifest contains no files.'
}

$entryByName = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
foreach ($entry in $entries) {
    $name = [string] $entry.name
    if ([string]::IsNullOrWhiteSpace($name) -or [IO.Path]::GetFileName($name) -cne $name) {
        throw "Package manifest contains invalid file name '$name'."
    }
    if ([string] $entry.kind -cnotin @('main', 'symbol')) {
        throw "Package manifest entry '$name' has invalid kind '$($entry.kind)'."
    }
    if (-not $entryByName.TryAdd($name, $entry)) {
        throw "Package manifest contains duplicate file '$name'."
    }
}
if (@($entries | Where-Object kind -CEQ 'main').Count -eq 0) {
    throw 'Package manifest contains no main packages.'
}

$packageFiles = @(
    Get-ChildItem -LiteralPath $resolvedPackageOutput -File |
        Where-Object {
            $_.Name.EndsWith('.nupkg', [StringComparison]::OrdinalIgnoreCase) -or
            $_.Name.EndsWith('.snupkg', [StringComparison]::OrdinalIgnoreCase)
        }
)
if ($packageFiles.Count -ne $entries.Count) {
    throw "Package artifact contains $($packageFiles.Count) package files, but the manifest contains $($entries.Count)."
}

foreach ($packageFile in $packageFiles) {
    $entry = $null
    if (-not $entryByName.TryGetValue($packageFile.Name, [ref] $entry)) {
        throw "Package artifact contains unlisted file '$($packageFile.Name)'."
    }
    if ([long] $entry.length -ne $packageFile.Length) {
        throw "Package '$($packageFile.Name)' length does not match the manifest."
    }

    $hash = (Get-FileHash -LiteralPath $packageFile.FullName -Algorithm SHA256).Hash
    if ([string] $entry.sha256 -cne $hash) {
        throw "Package '$($packageFile.Name)' SHA-256 does not match the manifest."
    }
}

$entries | Sort-Object name | ForEach-Object { "$($_.kind): $($_.name) [$($_.sha256)]" }
