param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectDir,

    [Parameter(Mandatory = $true)]
    [string]$BaseVersion,

    [Parameter(Mandatory = $true)]
    [string]$Revision,

    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = "Stop"

$generatedFolder = Join-Path $ProjectDir "obj\$Configuration"
$generatedPath = Join-Path $generatedFolder 'VersionInfo.Generated.cs'

if (-not ($Revision -match '^\d+$')) {
    throw "Revision must be a non-negative integer."
}

$version = "$BaseVersion.$Revision"
$content = @"
using System.Reflection;

[assembly: AssemblyVersion("$version")]
[assembly: AssemblyFileVersion("$version")]
[assembly: AssemblyInformationalVersion("$version")]
"@

# Keep generated versions out of source control and preserve timestamps on unchanged builds.
if (-not (Test-Path -LiteralPath $generatedPath) -or
    (Get-Content -LiteralPath $generatedPath -Raw).TrimEnd() -ne $content.TrimEnd()) {
    [IO.Directory]::CreateDirectory($generatedFolder) | Out-Null
    Set-Content -LiteralPath $generatedPath -Value $content -Encoding ASCII
}
Write-Host "Generated Drop&Resize version $version"
