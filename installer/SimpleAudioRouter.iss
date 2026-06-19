#define MyAppName "Simple Audio Router"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "SimpleAudioRouter"
#define MyAppExeName "SimpleAudioRouter.exe"
#define MyAppMutex "SimpleAudioRouter.SingleInstance"
#ifndef PublishDir
#define PublishDir "..\dist\publish\win-x64"
#endif
#ifndef AppIcon
#define AppIcon "..\src\SimpleAudioRouter\obj\Release\net10.0-windows\app.ico"
#endif

[Setup]
AppId={{8F4E2A91-6C3D-4B1E-9F7A-2D5E8C1B4A03}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SimpleAudioRouter
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=SimpleAudioRouter-Setup-{#MyAppVersion}
SetupIconFile={#AppIcon}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
AppMutex={#MyAppMutex}
CloseApplications=force
UninstallDisplayIcon={app}\{#MyAppExeName}
LicenseFile=
InfoBeforeFile=
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; shellexec avoids runasoriginaluser so Windows can UAC-prompt for requireAdministrator
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent shellexec

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\SimpleAudioRouter"
