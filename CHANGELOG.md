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