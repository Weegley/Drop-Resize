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
- Multi-threaded processing with selectable worker count
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
- `ImageResizeService.cs` — image loading, resizing, and saving logic
- `BatchProcessor.cs` — multi-threaded batch processing
- `ResizeProfile.cs` and `ProfileStore.cs` — profile model and storage
- `ImageCardControl.cs` — result card UI
- `TempBatchManager.cs` — temporary output batch folders

## Build

Open `Drop&Resize.sln` in Visual Studio and build the solution.

The project targets .NET Framework 4.8 and uses standard .NET / Windows Forms components. No external NuGet packages are required for the current version.

## Notes

Drop&Resize focuses on speed and simplicity. It does not include image editing, history, project management, cloud upload, RAW/WebP/HEIC/AVIF support, or GPU acceleration.

## Screenshots:
<img  src="https://github.com/user-attachments/assets/12c21104-5e31-40fb-8398-6c1b2b257d51" /> <img src="https://github.com/user-attachments/assets/f6d2823a-c54f-4a1d-8bb7-dccd0e0c820f" /> <img  src="https://github.com/user-attachments/assets/6ec8dd74-f395-4d6d-89f2-682a5ae29e9c" />















## Changelog

## v1.1.0.16

### Changes

- Delete changelog for version 1.1.0.15
- Removed changelog entry for version 1.1.0.15.
- Clean up CHANGELOG by removing v1.1.0.15 details
- Removed details of version 1.1.0.15 from CHANGELOG.
- Refactor changelog generation in workflow

## v1.0.0.5

### Changes

- Add navigation within cards by arrow keys and deleting them wit del button
- Update README with more screenshots
- Update copyright year and name in LICENSE.txt

## v1.0.0.4

### Changes

- update version
- UI fix for clear exit

## v1.0.0.3

### Changes

- Added removal of working folder in %TEMP%
- Update README.md
- Update CHANGELOG with new updates checking
- docs: update changelog [bot]
- Remove v1.0.0.2 changelog entries

## v1.0.0.2

### Changes

- Test release to check workflow
- docs: update changelog [bot]
- Update workflow to auto generate/update Changelog and update README
- Added updates checking

## v1.0.0.1

### Settings and profiles location

- Updated settings and profiles storage location to `%PROGRAMDATA%`.

## v1.0.0.0

### Initial release

- Added the first public Drop&Resize release.
