# Build BlakiBridge

BlakiBridge is a Windows Forms application written in C# for .NET 10. The application source is in [`src/BlakiBridge`](src/BlakiBridge). The MIT License is in [`LICENSE.txt`](LICENSE.txt).

## Requirements

- Windows and the .NET 10 SDK, including Windows desktop development support.
- PowerShell for the commands below.
- Internet access for any .NET runtime or Windows SDK reference packs not already installed. The project has no direct NuGet PackageReference dependencies.

Run commands from the repository root. The included `NuGet.Config` uses the official NuGet v3 feed to obtain any missing Microsoft packs. Already installed packs can be reused from the local SDK/cache.

## Desktop build

```powershell
dotnet build .\src\BlakiBridge\BlakiBridge.csproj -c Release
```

The executable is written to `src/BlakiBridge/bin/Release/net10.0-windows/`. This framework-dependent build requires the .NET 10 Desktop Runtime to run.

To create a standalone x64 folder that includes the runtime:

```powershell
dotnet publish .\src\BlakiBridge\BlakiBridge.csproj -c Release -r win-x64 --self-contained true --source https://api.nuget.org/v3/index.json -p:DebugType=None -p:DebugSymbols=false -o .\artifacts\desktop
```

Official 1.0.4 packages were prepared with the .NET 10.0.10 runtime. SDK and runtime patch differences can change the resulting files; a local rebuild is not expected to have the same checksums as the official downloads.

For redistribution, include `LICENSE.txt`, `PRIVACY.md`, `THIRD-PARTY-NOTICES.txt`, and the six component license/notice files from the repository root. If you build with a different runtime version, review and include that version's original license and notice files.

## Portable folder

Copy the complete standalone desktop folder to a writable location, include the documents above and [`packaging/PORTABLE-README.txt`](packaging/PORTABLE-README.txt), and create an empty `portable.flag` beside `BlakiBridge.exe`. Settings and diagnostics will then use the adjacent `Data` folder. Keep an existing `Data` folder when updating; do not include personal `Data` files in a distribution. Portable mode does not support Windows startup.

## Regression checks

The executable accepts these diagnostic switches, followed by an absolute report filename. Create the report directory first.

```powershell
New-Item -ItemType Directory -Path .\artifacts\qa -Force | Out-Null
$reportRoot = (Resolve-Path .\artifacts\qa).Path
& .\artifacts\desktop\BlakiBridge.exe --input-continuity-test "$reportRoot\input-continuity.json"
& .\artifacts\desktop\BlakiBridge.exe --monitor-identity-test "$reportRoot\monitor-identity.json"
& .\artifacts\desktop\BlakiBridge.exe --self-test "$reportRoot\self-test.json"
```

Read the JSON reports after the processes finish. These commands use test fixtures rather than saving the user's active configuration. Hardware detection and some UI checks depend on the current Windows session. Automated checks do not replace physical testing of monitor crossings, elevated windows, startup, sleep or display reconnection.

## Setup and Microsoft Store packaging

- [`packaging/Setup/README.md`](packaging/Setup/README.md) describes building Setup with Inno Setup.
- [`packaging/StoreStartup/README.md`](packaging/StoreStartup/README.md) describes the Store build, its startup launcher and MSIX inputs.

Local builds are unsigned unless you arrange signing separately. The Microsoft Store distributes its accepted packages through Microsoft's signing process.
