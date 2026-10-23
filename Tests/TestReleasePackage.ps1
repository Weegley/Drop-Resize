param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$projectRoot = Split-Path $PSScriptRoot -Parent
$applicationPath = Join-Path $projectRoot ("bin\$Configuration\Drop&Resize.exe")
$version = [Version][Diagnostics.FileVersionInfo]::GetVersionInfo($applicationPath).FileVersion
$testFolder = Join-Path $projectRoot ('tmp\package-test-' + [Guid]::NewGuid().ToString('N'))
$package = & (Join-Path $projectRoot 'Build\PackageRelease.ps1') -Configuration $Configuration -ExpectedRevision $version.Revision -OutputFolder $testFolder
$archive = [IO.Compression.ZipFile]::OpenRead($package.ArchivePath)
try {
    $fileNames = @($archive.Entries | Where-Object { $_.Name } | ForEach-Object { $_.FullName.Replace('\', '/') } | Sort-Object)
    $expectedFiles = @('DropResize/Drop&Resize.exe', 'DropResize/LICENSE.txt')
    if (@(Compare-Object $expectedFiles $fileNames).Count -ne 0) { throw "Package contains unexpected files: $fileNames" }
    $entryStream = ($archive.Entries | Where-Object Name -eq 'Drop&Resize.exe').Open()
    $binaryStream = [IO.File]::OpenRead($applicationPath)
    $hash = [Security.Cryptography.SHA256]::Create()
    try {
        $archivedHash = [BitConverter]::ToString($hash.ComputeHash($entryStream))
        $builtHash = [BitConverter]::ToString($hash.ComputeHash($binaryStream))
        if ($archivedHash -ne $builtHash) { throw 'Packaged executable differs from the tested binary.' }
    }
    finally { $hash.Dispose(); $binaryStream.Dispose(); $entryStream.Dispose() }
}
finally { $archive.Dispose() }

$notes = Get-Content -LiteralPath $package.NotesPath -Raw
$expectedHeading = "## v$($version.Major).$($version.Minor).$($version.Build)"
if (-not $notes.StartsWith($expectedHeading) -or [Regex]::Matches($notes, '(?m)^## ').Count -ne 1) {
    throw 'Release notes include the wrong version section.'
}
$mismatchRejected = $false
try {
    & (Join-Path $projectRoot 'Build\PackageRelease.ps1') -Configuration $Configuration -ExpectedRevision ($version.Revision + 1) -OutputFolder $testFolder | Out-Null
}
catch { $mismatchRejected = $_.Exception.Message -like '*does not match expected release*' }
if (-not $mismatchRejected) { throw 'A mismatched release version was accepted.' }

foreach ($script in Get-ChildItem -Path (Join-Path $projectRoot 'Build'),$PSScriptRoot -Filter '*.ps1') {
    $parseErrors = $null
    $tokens = $null
    [Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$parseErrors) | Out-Null
    if ($parseErrors.Count) { throw "Invalid PowerShell syntax in $($script.Name): $parseErrors" }
}
Write-Host 'PASS release ZIP contents and executable identity, version guard, release notes, PowerShell syntax'
