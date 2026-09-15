// Semantic version comparison, shared by the installer and its test script.
// Included from a [Code] section; holds no state.

function VersionRank(const Version: String): Int64;
var
  Core, Part: String;
  Position, Index: Integer;
  Numbers: array[0..2] of Integer;
begin
  Core := Version;
  Position := Pos('-', Core);

  if Position > 0 then
    Core := Copy(Core, 1, Position - 1);

  Numbers[0] := 0;
  Numbers[1] := 0;
  Numbers[2] := 0;
  Index := 0;

  while (Core <> '') and (Index <= 2) do
  begin
    Position := Pos('.', Core);

    if Position > 0 then
    begin
      Part := Copy(Core, 1, Position - 1);
      Core := Copy(Core, Position + 1, Length(Core));
    end
    else
    begin
      Part := Core;
      Core := '';
    end;

    Numbers[Index] := StrToIntDef(Part, 0);
    Index := Index + 1;
  end;

  Result := Int64(Numbers[0]) * 1000000 + Int64(Numbers[1]) * 1000 + Int64(Numbers[2]);
end;

function SuffixRank(const Version: String): Integer;
var
  Position: Integer;
  Suffix: String;
begin
  Position := Pos('-', Version);

  // No suffix means a final release, which outranks every prerelease.
  if Position = 0 then
  begin
    Result := 4;
    Exit;
  end;

  Suffix := Lowercase(Copy(Version, Position + 1, Length(Version)));

  if Pos('alpha', Suffix) = 1 then
    Result := 1
  else if Pos('beta', Suffix) = 1 then
    Result := 2
  else if Pos('rc', Suffix) = 1 then
    Result := 3
  else
    Result := 0;
end;

function CompareVersions(const Left, Right: String): Integer;
var
  LeftRank, RightRank: Int64;
begin
  LeftRank := VersionRank(Left);
  RightRank := VersionRank(Right);

  if LeftRank < RightRank then
    Result := -1
  else if LeftRank > RightRank then
    Result := 1
  else if SuffixRank(Left) < SuffixRank(Right) then
    Result := -1
  else if SuffixRank(Left) > SuffixRank(Right) then
    Result := 1
  else
    Result := 0;
end;
