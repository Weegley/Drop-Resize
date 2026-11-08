# Regression tests

Windows and .NET Framework 4.8 are required. No test packages or downloads are needed.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RunTests.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RunTests.ps1 -Configuration Debug
```

The script builds the test project and its application/worker references, samples
system CPU utilization, then runs 35 test groups on small images, result cards, and updates. The script uses
Idle priority and one available logical CPU; child workers inherit the affinity.
Generated inputs and outputs are retained in an isolated `tmp/tests-*` directory
for inspecting failures. No visible application window is opened.

After building the complete solution, use `-SkipBuild` to test the existing binaries.
GitHub Actions uses that path, preserving the release revision supplied to MSBuild.

Tests cover image dimensions and encoding, alpha and DPI, resampling compatibility,
file ownership, worker transport, memory scheduling, architecture and embedded
worker identity, parallel filename collisions, progress, cancellation, malformed
responses, stderr draining, worker/callback failures, cleanup, and preview disposal.
It also verifies the release ZIP's allowlist and executable hash, rejects version
mismatches, and checks release notes and PowerShell syntax.

UI checks cover Shift-click and Shift-arrow ranges (including reversing direction,
Ctrl+Shift, wrapped rows, and scrolling), focus after deletion, and replacing a batch.
Two tests create 1000 cards with real window handles in a transparent off-screen
window, then clear/delete them. They verify disposal, list reuse, and bounded
layout passes; elapsed times are reported without machine-dependent time limits.

Update tests use an in-memory HTTP handler and real updater processes with a small,
silent test application. They cover installation and restart, ZIP/extraction cleanup,
download/copy/launch failures, cancellation, concurrent downloads, temporary file
locks, cleanup boundaries, and the first upgrade using the old helper. Update
tests use isolated temporary folders and do not download GitHub releases.

This suite does not run large-image stress tests or verify interactive drag-and-drop.
