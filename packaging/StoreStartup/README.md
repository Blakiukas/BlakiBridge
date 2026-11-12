# Microsoft Store build and startup launcher

`Program.cs` and `BlakiBridge.Startup.csproj` contain the complete source of the small startup launcher. It starts the adjacent `BlakiBridge.exe` with `--startup`, so the main program opens in its startup mode. If launching fails, it writes a local `startup-error.txt` diagnostic file.

The application switches to the Store build with `StoreBuild=true`. `PackageMode.cs` then uses the packaged Windows StartupTask API. The manifest references `BlakiBridge.Startup.exe` and task ID `BlakiBridgeStartup`.

Run these commands from the repository root:

```powershell
dotnet publish .\src\BlakiBridge\BlakiBridge.csproj -c Release -r win-x64 --self-contained true --source https://api.nuget.org/v3/index.json -p:StoreBuild=true -p:DebugType=None -p:DebugSymbols=false -o .\artifacts\store\payload
dotnet publish .\packaging\StoreStartup\BlakiBridge.Startup.csproj -c Release -r win-x64 --self-contained true --source https://api.nuget.org/v3/index.json -p:Version=1.0.4 -p:AssemblyVersion=1.0.4.0 -p:FileVersion=1.0.4.0 -p:DebugType=None -p:DebugSymbols=false -o .\artifacts\store\startup
Get-ChildItem .\artifacts\store\startup\BlakiBridge.Startup.* | Copy-Item -Destination .\artifacts\store\payload
```

Both publish commands must use compatible runtime versions. Include the nine root license/privacy/notice documents in the final payload, as described in [`BUILDING.md`](../../BUILDING.md).

## MSIX inputs

`AppxManifest.template.xml` contains identity placeholders. Replace the identity name, publisher and publisher display name with the values for your own package, and set its four-part version. Preserve the application/launcher filenames and StartupTask ID unless you also update their code references. Store identity values must match the Partner Center product used for submission.

Generate image assets with the Store executable's export mode:

```powershell
$assetFolder = Join-Path (Resolve-Path .\artifacts\store).Path 'Assets'
$assetProcess = Start-Process -FilePath .\artifacts\store\payload\BlakiBridge.exe -ArgumentList '--export-store-assets', ('"' + $assetFolder + '"') -WindowStyle Hidden -PassThru -Wait
if ($assetProcess.ExitCode) { throw 'Store asset export failed.' }
Copy-Item -LiteralPath $assetFolder -Destination .\artifacts\store\payload -Recurse
foreach ($assetName in @('StoreLogo','Square44x44Logo','Square150x150Logo')) {
    Copy-Item -LiteralPath ".\artifacts\store\Assets\$assetName.scale-100.png" -Destination ".\artifacts\store\payload\Assets\$assetName.png"
}
```

Save the completed manifest as `artifacts/store/payload/AppxManifest.xml`. Use the Windows SDK's `makepri.exe` with the included `priconfig.xml` and the package identity name to create `payload/resources.pri`. Then use `makeappx.exe pack` to validate and package that payload:

```powershell
# Run from a shell where the Windows SDK x64 tools are on PATH.
# Replace Your.Package.Name with the identity used in your completed manifest.
makepri new /pr .\artifacts\store\payload /cf .\packaging\StoreStartup\priconfig.xml /in Your.Package.Name /of .\artifacts\store\payload\resources.pri /o
makeappx pack /d .\artifacts\store\payload /p .\artifacts\store\BlakiBridge-1.0.4-x64.msix /o
```

This creates an unsigned MSIX. Local installation requires an appropriate signing/trust setup. Store submission and certification are separate from compilation and packaging. No signing credentials, certificate files or private Partner Center data are included in this repository.
