#define AppName "File Mover Service"
#define AppPublisher "benedektothten"
#define AppExeName "FileMoverService.exe"
#define UIExeName "FileMoverService.UI.exe"
#define ServiceName "TorrentMoverService"

[Setup]
AppId={{B3F2A1D0-4E7C-4F8A-9B2E-1C3D5E6F7A8B}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputBaseFilename=FileMoverServiceInstaller
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin

; Wizard pages
SetupIconFile=
WizardSmallImageFile=
DisableWelcomePage=no
DisableReadyPage=no
DisableProgramGroupPage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to the {#AppName} Setup Wizard
WelcomeLabel2=This will install {#AppName} {#AppVersion} on your computer.%n%nThe service monitors folders and automatically moves files based on rules you configure.%n%nClick Next to continue.
FinishedHeadingLabel=Setup complete
FinishedLabel={#AppName} {#AppVersion} has been installed.%n%nThe configuration UI has been added to your system tray startup. You can find it in the Start Menu or by right-clicking the tray icon.%n%nClick Finish to exit Setup.
ReadyLabel1=Setup is ready to install {#AppName} {#AppVersion}.
ReadyLabel2a=Click Install to proceed, or click Back to review your settings.

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut for the configuration UI"; GroupDescription: "Additional icons:"; Flags: unchecked
Name: "startservice"; Description: "&Start the service automatically after installation"; GroupDescription: "Service:"

[Dirs]
Name: "{app}"

[Files]
; Windows service
Source: "publish-service\*"; DestDir: "{app}\service"; Flags: ignoreversion recursesubdirs

; Configuration UI
Source: "publish-ui\*"; DestDir: "{app}\ui"; Flags: ignoreversion recursesubdirs

; Shared appsettings (written once, not overwritten on upgrade)
Source: "FileMoverService\appsettings.json"; DestDir: "{app}\service"; Flags: onlyifdoesntexist

[Icons]
; Start Menu
Name: "{group}\{#AppName} Configuration"; Filename: "{app}\ui\{#UIExeName}"; Comment: "Configure File Mover Service"
Name: "{group}\Uninstall {#AppName}";     Filename: "{uninstallexe}"

; Desktop (optional task)
Name: "{userdesktop}\{#AppName} Configuration"; Filename: "{app}\ui\{#UIExeName}"; Tasks: desktopicon

[Registry]
; Auto-start UI in system tray on login
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
  ValueType: string; ValueName: "{#AppName}"; \
  ValueData: """{app}\ui\{#UIExeName}"""; \
  Flags: uninsdeletevalue

[Run]
; Install and optionally start the Windows service
Filename: "{app}\service\{#AppExeName}"; Parameters: "install"; Flags: runhidden waituntilterminated; StatusMsg: "Installing Windows service..."
Filename: "{app}\service\{#AppExeName}"; Parameters: "start";   Flags: runhidden waituntilterminated; StatusMsg: "Starting service..."; Tasks: startservice

; Launch the UI after setup (not elevated, so it runs as the user)
Filename: "{app}\ui\{#UIExeName}"; Description: "Launch {#AppName} configuration UI"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "{app}\service\{#AppExeName}"; Parameters: "stop";      Flags: runhidden waituntilterminated; RunOnceId: "StopService"
Filename: "{app}\service\{#AppExeName}"; Parameters: "uninstall"; Flags: runhidden waituntilterminated; RunOnceId: "UninstallService"
