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
if ($group.cicd -ne 'inline-il-package') {
    throw "The Inline IL CI hook received group '$($group.cicd)'."
}

$null = New-Item -ItemType Directory -Path $PackageOutputDir -Force
foreach ($project in $group.packableProjects) {
    & dotnet pack $project `
        --configuration $Configuration `
        --no-build `
        --output $PackageOutputDir `
        -p:PackageVersion=$PackageVersion `
        -p:GeneratePackageOnBuild=false
    if ($LASTEXITCODE -ne 0) {
        throw "Packing '$project' failed."
    }
}

& (Join-Path $PSScriptRoot 'inline-il-package-cd.ps1') `
    -Configuration $Configuration `
    -ProjectsJson $ProjectsJson `
    -PackageOutputDir $PackageOutputDir `
    -PackageVersion $PackageVersion
