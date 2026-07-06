[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Configuration,
    [Parameter(Mandatory)][string] $ProjectsJson,
    [Parameter(Mandatory)][string] $PackageOutputDir,
    [Parameter(Mandatory)][string] $PackageVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$group = (Get-Content -LiteralPath $ProjectsJson -Raw | ConvertFrom-Json).groups[0]
$testProject = @($group.testProjects | Where-Object { $_ -like '*/Tests.Rumtime.Emit.Inline.csproj' })
if ($testProject.Count -ne 1) {
    throw "Expected one Inline IL package test project, found $($testProject.Count)."
}
$testProjectPath = $testProject[0]

$resolvedPackageOutput = [IO.Path]::GetFullPath($PackageOutputDir)
$packagePath = Join-Path $resolvedPackageOutput "Nebulae.Runtime.Emit.Inline.$PackageVersion.nupkg"
if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "Inline IL package '$packagePath' was not found."
}

$validationRoot = Join-Path $resolvedPackageOutput "inline-il-validation-$([guid]::NewGuid().ToString('N'))"
$packageCache = Join-Path $validationRoot 'packages'
$null = New-Item -ItemType Directory -Path $packageCache -Force
$testConfiguration = "$Configuration-PackageValidation-$([guid]::NewGuid().ToString('N').Substring(0, 8))"

# Prime an isolated package cache with test dependencies, then restore only the generated
# Inline IL package from the local source. This prevents an existing NuGet.org version from
# being selected when a release is re-run.
& dotnet restore $testProjectPath `
    --force `
    --no-cache `
    --packages $packageCache `
    --source 'https://api.nuget.org/v3/index.json' `
    -p:UsePackagedInlineIL=false
if ($LASTEXITCODE -ne 0) {
    throw 'Priming the isolated Inline IL test package cache failed.'
}

& dotnet restore $testProjectPath `
    --force `
    --no-cache `
    --packages $packageCache `
    --source $resolvedPackageOutput `
    -p:UsePackagedInlineIL=true `
    -p:InlineILPackageVersion=$PackageVersion `
    -p:Configuration=$testConfiguration
if ($LASTEXITCODE -ne 0) {
    throw 'Restoring the Inline IL test project from the generated package failed.'
}

& dotnet test $testProjectPath `
    --configuration $testConfiguration `
    --no-restore `
    --verbosity normal `
    -p:RestorePackagesPath=$packageCache `
    -p:UsePackagedInlineIL=true `
    -p:InlineILPackageVersion=$PackageVersion `
    -p:Optimize=true `
    -p:GeneratePackageOnBuild=false
if ($LASTEXITCODE -ne 0) {
    throw 'Testing the installed Inline IL package failed.'
}
