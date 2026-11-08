## v1.2.1

### Changes

- Select card ranges with Shift-click and Shift-arrow keys, with Ctrl+Shift for additive selection.
- Clear large result lists in one layout batch and delete old temporary outputs without blocking the UI thread.
- Remove update ZIPs and extracted files after installation, cancellation, or failure; clean leftovers from the old updater on first launch.
- Isolate concurrent update downloads and retry cleanup when a file is briefly locked.
- Release card previews and owned fonts when cards are removed.
- Expand regression coverage to 35 groups, including 1000-card lists and real updater installation/restart tests.

## v1.2.0

### Changes

- Resize images in independent worker processes, with concurrency limited by CPU count and available memory.
- Avoid copying full-size input bitmaps and keep small previews instead of full output images in result cards.
- Stream completed results to the UI and coalesce progress updates.
- Prevent parallel output filename collisions and wait for workers when canceling a batch.
- Simplify image processing, worker transport, preview ownership, and embedded executable caching.
- Add image, protocol, cancellation, packaging, and architecture regression checks before publishing releases.
- Publish the exact tested commit, verify the executable version, and package only the application and license.

## v1.1.0.18

### Changes

- Added Auto Update feature
- Added arrows navigation on cards
- Added deleting of cards by del.
- Updated update dialog
- New versioning scheme
- Updated Workflow
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
