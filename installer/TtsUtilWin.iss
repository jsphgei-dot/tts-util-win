; TTS Util Win installer. Build it with: scripts\BuildPortable.ps1 -Installer
; Direct use: iscc installer\TtsUtilWin.iss

#ifndef AppVersion

  #define AppVersion "0.1.0-alpha"
  #define FileVersion "0.1.0.1"

  #define SourceDir "..\dist\TtsUtilWin"

#endif

#define AppName "TTS Util Win"
#define AppExeName "TtsUtilWin.exe"

#define AppPublisher "TTS Util Win contributors"
#define AppUrl "https://github.com/jsphgei-dot/tts-util-win"

[Setup]
AppId={{8F3C21D6-5A74-4E9B-B0C8-2D1E7A6F4B39}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#FileVersion}

; Show the install mode page and preselect the per user option.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes

LicenseFile=..\LICENSE
OutputDir=..\dist
OutputBaseFilename=TtsUtilWin-{#AppVersion}-setup
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName} {#AppVersion}

Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE";                 DestDir: "{app}"; Flags: ignoreversion
Source: "..\NOTICE";                  DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md";               DestDir: "{app}"; Flags: ignoreversion
Source: "..\scripts\FetchVoices.ps1"; DestDir: "{app}\scripts"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Voices and settings live under the user profile and are left in place.
Type: dirifempty; Name: "{app}\scripts"
Type: dirifempty; Name: "{app}"
