# EmuVora updates

Settings → App-Updates shows the installed version and checks the latest stable
GitHub release. Startup checks can be disabled independently of emulator updates.
Download the update, then select “Aktualisieren und neu starten”. Games and ES-DE
must be closed first. A failed check never prevents playing offline.

The updater accepts only the official repository's Windows setup asset with a
GitHub-provided SHA-256 digest. It limits metadata and download sizes, verifies
the completed executable, and verifies it again before starting the installer.
Interrupted or invalid downloads cannot be installed. Downloads remain under
the selected data directory's `updates` folder.

For an installed copy, setup waits for the old process to exit, updates the same
App directory, keeps the explicit data path and existing shortcuts, then starts
EmuVora again. The preceding App directory remains as `App-backup-*`; games,
settings, cover files, emulator packages and saves stay in the data directory.
Portable copies open the setup folder picker and pass their existing data path.
Installation errors are written beside the downloaded setup as
`EmuVora-setup-error.log`. The retained App backup can be launched if needed.

Every push to main, including each merged PR, runs the full Windows build,
functional/network tests and installer tests. Only a successful run publishes
`EmuVora-Setup.exe` and `EmuVora-win-x64.zip` as a GitHub release. Version numbers
use the project's major/minor version plus the increasing workflow run number;
the same version is embedded in the application. PR builds provide artifacts
without publishing a release. Re-running a build uses the same release tag.
