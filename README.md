[![Build & Release Drop&Resize ](https://github.com/Weegley/Drop-Resize/actions/workflows/BuildAndReleaseDropResize.yml/badge.svg)](https://github.com/Weegley/Drop-Resize/actions/workflows/BuildAndReleaseDropResize.yml)
# Drop&Resize

Drop&Resize is a small Windows utility for quickly resizing image files.

It is designed for a simple workflow: choose a resize profile, drag original images into the window, and drag the resized results back out to Explorer, Outlook, or another application.

## Features

- Drag and drop images from Windows Explorer
- Immediate batch resizing after drop
- One current batch only — new dropped files replace the previous results
- Result cards with thumbnail preview, dimensions, file size, and status
- Multi-select result cards and drag them out as normal files
- Resize profiles with custom width, height, JPEG quality, output format, suffix, and “do not enlarge” option
- Built-in profiles can be edited or reset to defaults
- Custom user profiles can be added, edited, and deleted
- Parallel image processing in separate worker processes with selectable worker count and memory-aware concurrency
- Basic protection against dropping generated files back into the app

## Supported formats

Input:

- JPG / JPEG
- PNG
- BMP
- GIF
- TIFF

Output:

- JPEG
- PNG
- BMP

Animated GIF and multi-page TIFF handling is basic; the app is intended for quick photo resizing, not advanced image editing.

## How it works

1. Select a resize profile.
2. Drop image files into the main window.
3. Drop&Resize creates resized files in a temporary batch folder.
4. Completed images appear as cards.
5. Select one or more cards and drag them out to Explorer, Outlook, or another target application.

Generated files are real temporary files, so standard Windows file drag-and-drop is used.

## Requirements

- Windows 10 or newer recommended
- .NET Framework 4.8
- Visual Studio 2022 for building from source

## Project structure

- `Drop&Resize` — main Windows Forms application
- `Drop&Resize.Worker` — background worker executable used for image processing
- `WorkerProcessPool.cs` — process scheduling, memory limits, progress, and cancellation
- `ImageResizeService.cs` — image loading, resizing, and saving logic
- `EmbeddedExecutableCache.cs` — atomic extraction and reuse of embedded tools
- `ResizeProfile.cs` and `ProfileStore.cs` — profile model and storage
- `ImageCardControl.cs` — result card UI
- `TempBatchManager.cs` — temporary output batch folders

## Build

Open `Drop&Resize.sln` in Visual Studio and build the solution.

The project targets .NET Framework 4.8 and uses standard .NET / Windows Forms components. No external NuGet packages are required for the current version.

The worker selector controls the maximum number of simultaneous image processing processes. Auto leaves a logical CPU available on multi-core machines, with a ceiling of 32 processes. The actual count is also limited by batch size and an estimate of image memory requirements using half of currently available physical memory. Workers run at BelowNormal priority.

Small previews are created during processing and sent to the UI with each result. Full output bitmaps are not retained by result cards.

Run functional regression tests with `powershell -NoProfile -ExecutionPolicy Bypass -File .\Tests\RunTests.ps1`. The runner reports CPU load and restricts itself and its child processes to one available logical CPU. It does not run large-image performance tests.

## Notes

Drop&Resize focuses on speed and simplicity. It does not include image editing, history, project management, cloud upload, RAW/WebP/HEIC/AVIF support, or GPU acceleration.

## Screenshots:
<img  src="https://github.com/user-attachments/assets/12c21104-5e31-40fb-8398-6c1b2b257d51" /> <img src="https://github.com/user-attachments/assets/f6d2823a-c54f-4a1d-8bb7-dccd0e0c820f" /> <img  src="https://github.com/user-attachments/assets/6ec8dd74-f395-4d6d-89f2-682a5ae29e9c" />

## Changelog

See [CHANGELOG.md](CHANGELOG.md) and [GitHub Releases](https://github.com/Weegley/Drop-Resize/releases).

## Release workflow

Pushes to `master` build and test the exact pushed commit, verify the generated executable version, and publish `DropResize-Windows.zip`. Version numbers combine `BaseVersion` in the project file with the workflow run number. Manual runs on other branches build and test without publishing.

The package includes only the application and license. Release notes combine the current curated changelog section with GitHub's generated notes. The workflow no longer creates documentation commits or appends duplicate history to this README. Existing tags must resolve to the tested source commit before a release can be updated.