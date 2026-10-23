param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release', [switch]$SkipBuild)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$runner = [Diagnostics.Process]::GetCurrentProcess()
$runner.PriorityClass = 'Idle'
# Children inherit the affinity mask. Use one available logical CPU throughout the build and tests.
$allowedMask = $runner.ProcessorAffinity.ToInt64()
for ($cpu = [Math]::Min(62, [Environment]::ProcessorCount - 1); $cpu -ge 0; $cpu--) {
    $mask = [long]1 -shl $cpu
    if (($allowedMask -band $mask) -ne 0) {
        $runner.ProcessorAffinity = [IntPtr]$mask
        break
    }
}

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class ResizeTestCpuUsage {
    [DllImport("kernel32.dll")]
    public static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
}
'@
[long]$idleBefore = 0; [long]$kernelBefore = 0; [long]$userBefore = 0
[long]$idleAfter = 0; [long]$kernelAfter = 0; [long]$userAfter = 0
$sampleBefore = [ResizeTestCpuUsage]::GetSystemTimes([ref]$idleBefore, [ref]$kernelBefore, [ref]$userBefore)
Start-Sleep -Milliseconds 1000
$sampleAfter = [ResizeTestCpuUsage]::GetSystemTimes([ref]$idleAfter, [ref]$kernelAfter, [ref]$userAfter)
$totalDelta = ($kernelAfter - $kernelBefore) + ($userAfter - $userBefore)
if ($sampleBefore -and $sampleAfter -and $totalDelta -gt 0) {
    $usage = 100 * (1 - ($idleAfter - $idleBefore) / $totalDelta)
    'CPU before tests: {0:F1}%. Runner: Idle priority, one logical CPU.' -f $usage
} else {
    'CPU sample unavailable. Runner remains limited to one logical CPU at Idle priority.'
}

if (-not $SkipBuild) {
    & msbuild (Join-Path $PSScriptRoot 'ResizeTests.csproj') /t:Build ('/p:Configuration=' + $Configuration) /p:UseSharedCompilation=false /m:1 /nodeReuse:false /nologo /v:minimal
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
}
$testExe = Join-Path $projectRoot ('bin\Tests\' + $Configuration + '\DropResize.Tests.exe')
$workerExe = Join-Path $projectRoot ('obj\Worker\' + $Configuration + '\Drop&Resize.Worker.exe')
& $testExe $projectRoot $workerExe
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
& (Join-Path $PSScriptRoot 'TestReleasePackage.ps1') -Configuration $Configuration
