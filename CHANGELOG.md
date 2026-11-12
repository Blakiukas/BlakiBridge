# Changelog

## 1.0.4 — 2026-11-12

- Fixed an incorrect cursor jump after mouse input was interrupted and resumed on another monitor, including a case that could move a window unexpectedly.
- The pointer engine now detects an outdated monitor position and resets its movement baseline before applying scaling or screen-boundary mapping.
- BlakiBridge is now open source under the MIT License. Use, modification, redistribution and sale are permitted under that license.
- Setup, Portable and Microsoft Store packages include the license, privacy information and third-party notices.

The fix does not remove Windows input restrictions when an application with higher privileges is active.

### Downloads

- **Setup.exe** installs for your Windows account. It is unsigned, so Windows may display an unknown-publisher or SmartScreen warning.
- **Portable ZIP** runs after extracting the entire archive into a writable folder. Keep the existing `Data` folder when updating. Windows startup is unavailable in portable mode.
- **Source ZIP** contains the source code and build instructions.
- **SHA256SUMS.txt** provides checksums for the downloadable files.

Close the running copy through its tray menu before updating. Existing settings and diagnostics should be preserved.

Microsoft Store updates are published after Microsoft's certification process.
