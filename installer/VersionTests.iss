; Test harness for Version.iss. Compile it, run the result with /VERYSILENT,
; and read the report it writes. Run by scripts\TestInstaller.ps1.

[Setup]
AppName=Version.iss tests
AppVersion=1.0
DefaultDirName={tmp}\VersionTests
Uninstallable=no
CreateAppDir=no
DisableProgramGroupPage=yes
OutputDir=.
OutputBaseFilename=VersionTests
PrivilegesRequired=lowest

[Code]
#include "Version.iss"

var
  Report: TStringList;
  Failures: Integer;

procedure Check(const Left, Right: String; const Expected: Integer);
var
  Actual: Integer;
  Verdict: String;
begin
  Actual := CompareVersions(Left, Right);

  if Actual = Expected then
    Verdict := 'pass'
  else
  begin
    Verdict := 'FAIL';
    Failures := Failures + 1;
  end;

  Report.Add(Format('%s  compare(%s, %s) = %d, expected %d', [Verdict, Left, Right, Actual, Expected]));
end;

function InitializeSetup: Boolean;
begin
  Report := TStringList.Create;
  Failures := 0;

  Check('0.1.0-alpha', '0.1.1-alpha', -1);
  Check('0.1.1-alpha', '0.1.0-alpha', 1);
  Check('0.1.1-alpha', '0.1.1-alpha', 0);
  Check('0.9.0', '1.0.0', -1);
  Check('1.0.0', '0.9.9', 1);
  Check('0.2.0-alpha', '0.10.0-alpha', -1);
  Check('1.0.0-alpha', '1.0.0-beta', -1);
  Check('1.0.0-beta', '1.0.0-rc.1', -1);
  Check('1.0.0-rc.1', '1.0.0', -1);
  Check('1.0.0', '1.0.0-rc.1', 1);
  Check('0.1.1-alpha', '0.1.1-alpha.2', 0);
  Check('2.3.4', '2.3.4', 0);
  Check('0.1', '0.1.0', 0);
  Check('nonsense', '0.0.1', -1);

  Report.Add(Format('%d failure(s)', [Failures]));
  Report.SaveToFile(ExpandConstant('{param:report|report.txt}'));

  Result := False;
end;
