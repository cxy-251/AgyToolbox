; Inno Setup Script for AgyToolbox
; Builds user-friendly Setup .exe with Start Menu & Desktop shortcuts and clean uninstaller

#ifndef MyAppVersion
#define MyAppVersion "0.2.0"
#endif

#ifndef MySourceDir
#define MySourceDir "..\bin\Release\net8.0-windows\win-x64\publish"
#endif

#ifndef MyOutputDir
#define MyOutputDir "..\dist"
#endif

[Setup]
AppId={{E7E7A1C2-88B1-4A59-86D3-1D0A3D634509}
AppName=AgyToolbox
AppVersion={#MyAppVersion}
AppPublisher=cxy-251
AppPublisherURL=https://github.com/cxy-251/AgyToolbox
AppSupportURL=https://github.com/cxy-251/AgyToolbox/issues
AppUpdatesURL=https://github.com/cxy-251/AgyToolbox/releases
DefaultDirName={autopf}\AgyToolbox
DefaultGroupName=AgyToolbox
DisableProgramGroupPage=yes
OutputDir={#MyOutputDir}
OutputBaseFilename=AgyToolbox-Setup-v{#MyAppVersion}-x64
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"
Name: "english"; MessagesFile: "compiler:Languages\English.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AgyToolbox"; Filename: "{app}\AgyToolbox.exe"
Name: "{group}\{cm:UninstallProgram,AgyToolbox}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\AgyToolbox"; Filename: "{app}\AgyToolbox.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\AgyToolbox.exe"; Description: "{cm:LaunchProgram,AgyToolbox}"; Flags: nowait postinstall skipifsilent
