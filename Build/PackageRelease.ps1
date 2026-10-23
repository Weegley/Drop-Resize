param(
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release',
    [Nullable[int]]$ExpectedRevision,
    [string]$OutputFolder
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$applicationPath = Join-Path $projectRoot ("bin\$Configuration\Drop&Resize.exe")
$version = [Version][Diagnostics.FileVersionInfo]::GetVersionInfo($applicationPath).FileVersion
[xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot 'Drop&Resize.csproj') -Raw
$baseVersion = $project.SelectSingleNode('//*[local-name()="BaseVersion"]').InnerText
if ($null -ne $ExpectedRevision -and $version.ToString() -ne "$baseVersion.$ExpectedRevision") {
    throw "Binary version $version does not match expected release $baseVersion.$ExpectedRevision."
}

if (-not $OutputFolder) { $OutputFolder = Join-Path $projectRoot 'output' }
$outputRoot = [IO.Path]::GetFullPath($OutputFolder)
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$stagingFolder = Join-Path $outputRoot ('package-' + [Guid]::NewGuid().ToString('N'))
$packageFolder = Join-Path $stagingFolder 'DropResize'
[IO.Directory]::CreateDirectory($packageFolder) | Out-Null
$archivePath = Join-Path $outputRoot 'DropResize-Windows.zip'
try {
    # Package an explicit allowlist: profiles, caches, test tools and local settings must stay local.
    Copy-Item -LiteralPath $applicationPath -Destination $packageFolder
    Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE.txt') -Destination $packageFolder
    Compress-Archive -LiteralPath $packageFolder -DestinationPath $archivePath -Force
}
finally {
    $resolvedStaging = [IO.Path]::GetFullPath($stagingFolder)
    if (-not $resolvedStaging.StartsWith($outputRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Package cleanup target is outside the output directory.'
    }
    Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
}

# The repository keeps a curated changelog; releases also get GitHub's generated commit notes.
$changelog = Get-Content -LiteralPath (Join-Path $projectRoot 'CHANGELOG.md') -Raw -Encoding UTF8
$sectionPattern = '(?ms)^## v' + [Regex]::Escape($baseVersion) + '\s*\r?\n.*?(?=^## |\z)'
$notes = [Regex]::Match($changelog, $sectionPattern).Value.Trim()
if (-not $notes) { throw "CHANGELOG.md has no section for v$baseVersion." }
$notesPath = Join-Path $outputRoot 'release-notes.md'
[IO.File]::WriteAllText($notesPath, $notes, (New-Object Text.UTF8Encoding($false)))
[pscustomobject]@{ Version = $version.ToString(); ArchivePath = $archivePath; NotesPath = $notesPath }
