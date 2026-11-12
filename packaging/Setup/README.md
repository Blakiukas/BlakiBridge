# Setup installer

This folder contains the Inno Setup script, its Lithuanian translation and the small wizard image used by the official Setup. The image is installer artwork; the application icon is `src/BlakiBridge/BlakiBridge.ico`.

1. Publish the standalone desktop x64 build described in [`BUILDING.md`](../../BUILDING.md).
2. Copy the complete output to `packaging/Setup/payload/` and include the nine root license/privacy/notice documents. Do not add a `portable.flag` or user settings to this payload.
3. Install Inno Setup 6.7.3 or a compatible version and run its compiler from this folder:

```powershell
ISCC.exe .\BlakiBridge.iss
```

The resulting installer is written to `dist/BlakiBridge-1.0.4-Setup.exe` under this folder. The script uses relative project paths and includes EN, LT and RU installer languages. Inno Setup's own license is included at the repository root; review the compiler's terms for your version.

The installer supports per-user installation, optional shortcuts/startup and optional copying of existing Store settings. Its Store-settings path identifies the official BlakiBridge package. Adapt product identity, names and settings migration before distributing a renamed derivative. Review and test installation/uninstallation on an appropriate test system before distributing your build.
