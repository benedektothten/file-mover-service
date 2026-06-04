#define AppName "File Mover Service"
#define AppPublisher "benedektothten"
#define AppExeName "FileMoverService.exe"
#define UIExeName "FileMoverService.UI.exe"
#define ServiceName "FileMoverService"

; ── Variant selection ──────────────────────────────────────────────────────────
; Pass /DBundled to ISCC to produce the self-contained installer.
; Without it you get the small framework-dependent installer.
#ifdef Bundled
  #define ServiceSourceDir "publish-service-bundled"
  #define UISourceDir      "publish-ui-bundled"
  #define OutputFile       "FileMoverServiceInstaller-Full"
  #define VariantSuffix    " (Includes .NET Runtime)"
#else
  #define ServiceSourceDir "publish-service"
  #define UISourceDir      "publish-ui"
  #define OutputFile       "FileMoverServiceInstaller"
  #define VariantSuffix    ""
#endif

[Setup]
AppId={{B3F2A1D0-4E7C-4F8A-9B2E-1C3D5E6F7A8B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputBaseFilename={#OutputFile}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

SetupIconFile=
WizardSmallImageFile=
DisableWelcomePage=no
DisableReadyPage=no
DisableProgramGroupPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to the {#AppName} Setup Wizard
WelcomeLabel2=This will install {#AppName} {#AppVersion}{#VariantSuffix} on your computer.%n%nThe service monitors folders and automatically moves files based on rules you configure.%n%nClick Next to continue.
FinishedHeadingLabel=Setup complete
FinishedLabel={#AppName} {#AppVersion} has been installed.%n%nThe configuration UI has been added to your system tray startup. You can find it in the Start Menu or by right-clicking the tray icon.%n%nClick Finish to exit Setup.
ReadyLabel1=Setup is ready to install {#AppName} {#AppVersion}{#VariantSuffix}.
ReadyLabel2a=Click Install to proceed, or click Back to review your settings.

[Tasks]
Name: "desktopicon";  Description: "Create a &desktop shortcut for the configuration UI"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "startservice"; Description: "&Start the service automatically after installation";  GroupDescription: "Service:"

[Dirs]
Name: "{app}"
; Grant standard users modify rights so the UI can write appsettings.json without elevation
Name: "{commonappdata}\FileMoverService"; Permissions: users-modify

[Files]
; Windows service
Source: "{#ServiceSourceDir}\*"; DestDir: "{app}\service"; Flags: ignoreversion recursesubdirs

; Configuration UI
Source: "{#UISourceDir}\*"; DestDir: "{app}\ui"; Flags: ignoreversion recursesubdirs

; Shared config in ProgramData — written once, never overwritten on upgrade or uninstall
Source: "FileMoverService\appsettings.json"; DestDir: "{commonappdata}\FileMoverService"; Flags: onlyifdoesntexist uninsneveruninstall

[Icons]
Name: "{group}\{#AppName} Configuration"; Filename: "{app}\ui\{#UIExeName}"; Comment: "Configure File Mover Service"
Name: "{group}\Uninstall {#AppName}";     Filename: "{uninstallexe}"
Name: "{userdesktop}\{#AppName} Configuration"; Filename: "{app}\ui\{#UIExeName}"; Tasks: desktopicon

[Registry]
; Auto-start UI in system tray on login
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; \
  ValueData: """{app}\ui\{#UIExeName}"""; \
  Flags: uninsdeletevalue

[Run]
Filename: "{app}\service\{#AppExeName}"; Parameters: "install"; Flags: runhidden waituntilterminated; StatusMsg: "Installing Windows service..."
Filename: "{app}\service\{#AppExeName}"; Parameters: "start";   Flags: runhidden waituntilterminated; StatusMsg: "Starting service..."; Tasks: startservice
Filename: "{app}\ui\{#UIExeName}"; Description: "Launch {#AppName} configuration UI"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\service\{#AppExeName}"; Parameters: "stop";      Flags: runhidden waituntilterminated; RunOnceId: "StopService"
Filename: "{app}\service\{#AppExeName}"; Parameters: "uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "UninstallService"

[Code]
// ── .NET check — skipped in the bundled variant ───────────────────────────────
#ifndef Bundled
function IsDotNet10Installed(): Boolean;
var
  runtimes: TArrayOfString;
  i: Integer;
  key: String;
begin
  Result := False;
  key := 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App';
  if RegGetValueNames(HKLM, key, runtimes) then
    for i := 0 to GetArrayLength(runtimes) - 1 do
      if Pos('10.', runtimes[i]) = 1 then
      begin
        Result := True;
        Exit;
      end;
end;

function InitializeSetup(): Boolean;
var
  errCode: Integer;
begin
  Result := True;
  if not IsDotNet10Installed() then
  begin
    if MsgBox(
      '.NET Desktop Runtime 10 was not detected on this machine.' + #13#10 + #13#10 +
      'Please download and install it from:' + #13#10 +
      'https://aka.ms/dotnet/10/dotnet-runtime-win-x64.exe' + #13#10 + #13#10 +
      'Click OK to open the download page, or Cancel to quit setup.',
      mbError, MB_OKCANCEL) = IDOK then
      ShellExec('open', 'https://aka.ms/dotnet/10/dotnet-runtime-win-x64.exe', '', '', SW_SHOW, ewNoWait, errCode);
    Result := False;
  end;
end;
#endif
