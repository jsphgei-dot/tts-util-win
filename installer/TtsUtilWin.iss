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

#define VoiceBaseUrl "https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/"

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

[Types]
Name: "recommended"; Description: "Program and the three recommended voices"
Name: "minimal";     Description: "Program only, no voices"
Name: "custom";      Description: "Choose voices"; Flags: iscustom

[Components]
Name: "app"; Description: "{#AppName}"; Types: recommended minimal custom; Flags: fixed

Name: "voices";          Description: "Voice models, downloaded during installation"; Types: recommended custom
Name: "voices\ljspeech"; Description: "English (US), LJ Speech, high quality (110 MB, public domain)"; Types: recommended custom; ExtraDiskSpaceRequired: 115343360
Name: "voices\libritts"; Description: "English (US), LibriTTS-R, 900+ speakers (78 MB, CC BY 4.0)"; Types: recommended custom; ExtraDiskSpaceRequired: 81788928
Name: "voices\kokoro";   Description: "English, Kokoro, 11 voices (305 MB, Apache 2.0)"; Types: recommended custom; ExtraDiskSpaceRequired: 319815680
Name: "voices\alan";     Description: "English (GB), Alan (64 MB)"; ExtraDiskSpaceRequired: 67108864
Name: "voices\amy";      Description: "English (US), Amy, small and fast (64 MB)"; ExtraDiskSpaceRequired: 67108864
Name: "voices\lessac";   Description: "English (US), Lessac (64 MB)"; ExtraDiskSpaceRequired: 67108864

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceDir}\{#AppExeName}"; DestDir: "{app}"; Components: app; Flags: ignoreversion
Source: "..\LICENSE";                 DestDir: "{app}"; Components: app; Flags: ignoreversion
Source: "..\NOTICE";                  DestDir: "{app}"; Components: app; Flags: ignoreversion
Source: "..\README.md";               DestDir: "{app}"; Components: app; Flags: ignoreversion
Source: "..\scripts\FetchVoices.ps1"; DestDir: "{app}\scripts"; Components: app; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Voices downloaded here live under {app} and go with it. Settings stay.
Type: filesandordirs; Name: "{app}\voices"
Type: dirifempty; Name: "{app}\scripts"
Type: dirifempty; Name: "{app}"

[Code]
var
  DownloadPage: TDownloadWizardPage;
  VoiceComponents: TArrayOfString;
  VoiceIds: TArrayOfString;

procedure BuildVoiceTable;
begin
  SetArrayLength(VoiceComponents, 6);
  SetArrayLength(VoiceIds, 6);

  VoiceComponents[0] := 'voices\ljspeech';
  VoiceIds[0]        := 'vits-piper-en_US-ljspeech-high';

  VoiceComponents[1] := 'voices\libritts';
  VoiceIds[1]        := 'vits-piper-en_US-libritts_r-medium';

  VoiceComponents[2] := 'voices\kokoro';
  VoiceIds[2]        := 'kokoro-en-v0_19';

  VoiceComponents[3] := 'voices\alan';
  VoiceIds[3]        := 'vits-piper-en_GB-alan-medium';

  VoiceComponents[4] := 'voices\amy';
  VoiceIds[4]        := 'vits-piper-en_US-amy-low';

  VoiceComponents[5] := 'voices\lessac';
  VoiceIds[5]        := 'vits-piper-en_US-lessac-medium';
end;

function VoicesDirectory: String;
begin
  Result := ExpandConstant('{app}\voices');
end;

function ArchivePath(const Id: String): String;
begin
  Result := ExpandConstant('{tmp}\') + Id + '.tar.bz2';
end;

function TarPath: String;
begin
  Result := ExpandConstant('{sys}\tar.exe');

  if not FileExists(Result) then
    Result := 'tar.exe';
end;

function OnDownloadProgress(const Url, FileName: String; const Progress, ProgressMax: Int64): Boolean;
begin
  if Progress = ProgressMax then
    Log('Downloaded ' + FileName);

  Result := True;
end;

procedure InitializeWizard;
begin
  BuildVoiceTable;

  DownloadPage := CreateDownloadPage(
    'Downloading voices',
    'Setup is downloading the voice models you selected.',
    @OnDownloadProgress);
end;

function QueueSelectedVoices: Integer;
var
  I: Integer;
begin
  Result := 0;
  DownloadPage.Clear;

  for I := 0 to GetArrayLength(VoiceIds) - 1 do
  begin
    if not WizardIsComponentSelected(VoiceComponents[I]) then
      Continue;

    // A voice already on disk is left alone.
    if DirExists(VoicesDirectory + '\' + VoiceIds[I]) then
      Continue;

    DownloadPage.Add('{#VoiceBaseUrl}' + VoiceIds[I] + '.tar.bz2', VoiceIds[I] + '.tar.bz2', '');
    Result := Result + 1;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  if CurPageID <> wpReady then
    Exit;

  if QueueSelectedVoices = 0 then
    Exit;

  DownloadPage.Show;
  try
    try
      DownloadPage.Download;
    except
      SuppressibleMsgBox(
        'A voice could not be downloaded, so setup will continue without it.' + #13#10 + #13#10 +
        AddPeriod(GetExceptionMessage) + #13#10 + #13#10 +
        'Run scripts\FetchVoices.ps1 from the install folder to try again.',
        mbError, MB_OK, IDOK);
    end;
  finally
    DownloadPage.Hide;
  end;
end;

procedure ExtractVoices;
var
  I, ResultCode: Integer;
  Archive, Directory: String;
begin
  Directory := VoicesDirectory;

  if not ForceDirectories(Directory) then
    Exit;

  for I := 0 to GetArrayLength(VoiceIds) - 1 do
  begin
    Archive := ArchivePath(VoiceIds[I]);

    if not FileExists(Archive) then
      Continue;

    WizardForm.StatusLabel.Caption := 'Extracting ' + VoiceIds[I] + '...';

    if (not Exec(TarPath, '-xf "' + Archive + '" -C "' + Directory + '"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode))
      or (ResultCode <> 0) then
      SuppressibleMsgBox(
        'Could not extract ' + VoiceIds[I] + '.' + #13#10 + #13#10 +
        'Run scripts\FetchVoices.ps1 from the install folder to install it later.',
        mbError, MB_OK, IDOK);

    DeleteFile(Archive);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    ExtractVoices;
end;
