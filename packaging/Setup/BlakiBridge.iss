#define AppVersion "1.0.4"
[Setup]
AppId=BlakiBridge
AppName=BlakiBridge
AppVersion={#AppVersion}
AppPublisher=Blakiukas
LicenseFile=payload\LICENSE.txt
AppSupportURL=mailto:blaki.support@gmail.com
DefaultDirName={localappdata}\Programs\BlakiBridge
DefaultGroupName=BlakiBridge
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=dist
OutputBaseFilename=BlakiBridge-1.0.4-Setup
SetupIconFile=..\..\src\BlakiBridge\BlakiBridge.ico
WizardSmallImageFile=ikoblaki.png
UninstallDisplayIcon={app}\BlakiBridge.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
DisableDirPage=no
DisableProgramGroupPage=no
DisableWelcomePage=no
AllowNoIcons=yes
Uninstallable=yes
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "lithuanian"; MessagesFile: "Lithuanian.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
[CustomMessages]
english.StartupTask=Start BlakiBridge with Windows
lithuanian.StartupTask=Paleisti BlakiBridge kartu su Windows
russian.StartupTask=Запускать BlakiBridge вместе с Windows
english.StoreSettings=Copy existing Microsoft Store settings (only if no desktop settings exist)
lithuanian.StoreSettings=Kopijuoti Microsoft Store nustatymus (tik jei nėra atskiros kopijos nustatymų)
russian.StoreSettings=Копировать настройки Microsoft Store (если нет настроек отдельной копии)
english.CloseBridge=Please close BlakiBridge using its tray menu, then retry.
lithuanian.CloseBridge=Uždarykite BlakiBridge per jos ikonėlės meniu sistemos dėkle ir bandykite dar kartą.
russian.CloseBridge=Закройте BlakiBridge через меню значка в трее и повторите попытку.
english.CopyFailed=Settings could not be copied. The original Store settings are unchanged.
lithuanian.CopyFailed=Nepavyko nukopijuoti nustatymų. Originalūs Store nustatymai nepakeisti.
russian.CopyFailed=Не удалось скопировать настройки. Исходные настройки Store не изменены.
english.DeleteSettings=Also delete your BlakiBridge settings? Choose No to keep them.
lithuanian.DeleteSettings=Ar ištrinti ir BlakiBridge nustatymus? Pasirinkite Ne, jei norite juos išsaugoti.
russian.DeleteSettings=Удалить также настройки BlakiBridge? Выберите Нет, чтобы сохранить их.
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; Flags: unchecked
Name: "startup"; Description: "{cm:StartupTask}"; Flags: unchecked
Name: "storeconfig"; Description: "{cm:StoreSettings}"; Check: CanCopyStoreSettings
[Files]
Source: "payload\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\BlakiBridge"; Filename: "{app}\BlakiBridge.exe"
Name: "{userdesktop}\BlakiBridge"; Filename: "{app}\BlakiBridge.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\BlakiBridge.exe"; Description: "{cm:LaunchProgram,BlakiBridge}"; Flags: nowait postinstall skipifsilent
[Code]
var DeleteSettings: Boolean;
function StoreConfig: String;
begin
  Result := ExpandConstant('{localappdata}\Packages\Blakiukas.BlakiBridge_sdefccrkgb0gm\LocalCache\Local\BlakiBridge\config.json');
end;
function CanCopyStoreSettings: Boolean;
begin
  Result := FileExists(StoreConfig) and not FileExists(ExpandConstant('{localappdata}\BlakiBridge\config.json'));
end;
function OpenEvent(Access: LongWord; Inherit: Boolean; Name: String): THandle;
  external 'OpenEventW@kernel32.dll stdcall';
function SetEvent(Event: THandle): Boolean;
  external 'SetEvent@kernel32.dll stdcall';
function CloseHandle(Handle: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';
procedure StopBridge;
var Event: THandle; I: Integer;
begin
  Event := OpenEvent(2, False, 'Local\BlakiBridge.SetupExit');
  if Event <> 0 then begin SetEvent(Event); CloseHandle(Event); end;
  for I := 1 to 40 do begin
    if not CheckForMutexes('Local\BlakiBridge.Desktop') then exit;
    Sleep(250);
  end;
end;
function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  StopBridge;
  Result := '';
  if CheckForMutexes('Local\BlakiBridge.Desktop') then
    Result := CustomMessage('CloseBridge');
end;
procedure CurStepChanged(CurStep: TSetupStep);
var Existing: String;
begin
  if CurStep = ssPostInstall then begin
    if WizardIsTaskSelected('storeconfig') and CanCopyStoreSettings then begin
      ForceDirectories(ExpandConstant('{localappdata}\BlakiBridge'));
      if not CopyFile(StoreConfig, ExpandConstant('{localappdata}\BlakiBridge\config.json'), True) then
        MsgBox(CustomMessage('CopyFailed'), mbError, MB_OK);
      if FileExists(StoreConfig + '.bak') then
        CopyFile(StoreConfig + '.bak', ExpandConstant('{localappdata}\BlakiBridge\config.json.bak'), True);
    end;
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'BlakiBridge', Existing) then
      RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'BlakiBridge', '"' + ExpandConstant('{app}\BlakiBridge.exe') + '" --startup');
    if WizardIsTaskSelected('startup') then
      RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'BlakiBridge', '"' + ExpandConstant('{app}\BlakiBridge.exe') + '" --startup');
  end;
end;
function InitializeUninstall: Boolean;
begin
  DeleteSettings := False;
  Result := True;
end;
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var Question, Existing: String;
begin
  if CurUninstallStep = usUninstall then begin
    StopBridge;
    if CheckForMutexes('Local\BlakiBridge.Desktop') then RaiseException(CustomMessage('CloseBridge'));
    if not UninstallSilent then begin
      Question := CustomMessage('DeleteSettings');
      DeleteSettings := MsgBox(Question, mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES;
    end;
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'BlakiBridge', Existing) then
      if Pos(Lowercase(ExpandConstant('{app}\BlakiBridge.exe')), Lowercase(Existing)) > 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'BlakiBridge');
  end;
  if (CurUninstallStep = usPostUninstall) and DeleteSettings then begin
    DeleteFile(ExpandConstant('{localappdata}\BlakiBridge\config.json'));
    DeleteFile(ExpandConstant('{localappdata}\BlakiBridge\config.json.bak'));
  end;
end;
