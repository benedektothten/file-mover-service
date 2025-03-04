   [Setup]
   AppName=FileMoverService
   AppVersion=1.0.0
   DefaultDirName={pf}\FileMoverService
   DefaultGroupName=FileMoverService
   OutputBaseFilename=FileMoverServiceInstaller
   Compression=lzma
   SolidCompression=yes

   [Files]
   Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

   [Run]
   Filename: "{app}\FileMoverService.exe"; Parameters: "install"; Flags: runhidden

   [UninstallRun]
   Filename: "{app}\FileMoverService.exe"; Parameters: "uninstall"; Flags: runhidden

   [Icons]
   Name: "{group}\FileMoverService"; Filename: "{app}\FileMoverService.exe"
   Name: "{group}\Uninstall FileMoverService"; Filename: "{uninstallexe}"
