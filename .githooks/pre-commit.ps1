# Runs before every commit: hygiene, formatting, a warning free build, and the core tests.

. (Join-Path $PSScriptRoot 'common.ps1')

if (Test-HooksSkipped 'pre-commit') { exit 0 }

Write-Host 'pre-commit' -ForegroundColor White

$staged = Get-StagedFiles

if ($staged.Count -eq 0)
{
    Write-Host 'Nothing staged.' -ForegroundColor Yellow
    exit 0
}

$codeStaged = @($staged | Where-Object { $_ -match '\.(cs|csproj|sln|props|targets|xaml)$' })

Write-Step 'whitespace and conflict markers'
$whitespace = @(git diff --cached --check)

if ($whitespace.Count -gt 0)
{
    $whitespace | Select-Object -First 20 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkRed }
    Add-Failure 'Trailing whitespace, a space before a tab, or a conflict marker is staged.'
}
else
{
    Write-Pass 'whitespace clean'
}

Write-Step 'file sizes'
$large = @()
foreach ($file in $staged)
{
    $full = Join-Path $RepoRoot $file
    if (-not (Test-Path $full)) { continue }

    $size = (Get-Item -LiteralPath $full).Length
    if ($size -gt 5MB) { $large += "$file ($([math]::Round($size / 1MB, 1)) MB)" }
}

if ($large.Count -gt 0)
{
    Add-Failure "Files over 5 MB are staged: $($large -join ', '). Release artifacts belong in a release, not in git."
}
else
{
    Write-Pass 'no oversized files'
}

Write-Step 'build artifacts'
$artifacts = @($staged | Where-Object { $_ -match '^(dist|artifacts|voices)/' -and $_ -notmatch 'README' })

if ($artifacts.Count -gt 0)
{
    Add-Failure "Build output is staged: $($artifacts -join ', ')."
}
else
{
    Write-Pass 'no build output staged'
}

$installerStaged = @($staged | Where-Object { $_ -match '\.iss$' })

if ($installerStaged.Count -gt 0)
{
    Write-Step 'installer version tests'
    $installerTests = & (Join-Path $RepoRoot 'scripts\TestInstaller.ps1') 2>&1

    if ($LASTEXITCODE -ne 0)
    {
        $installerTests | Select-Object -Last 20 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkRed }
        Add-Failure 'Installer version tests failed.'
    }
    else
    {
        Write-Pass 'installer version tests pass'
    }
}

if ($codeStaged.Count -eq 0)
{
    Write-Host '  (no code staged, skipping format, build and tests)' -ForegroundColor DarkGray
    Exit-WithFailures 'pre-commit' 'git commit --no-verify'
}

Write-Step 'dotnet format'
$format = dotnet format $Solution --verify-no-changes --no-restore 2>&1
if ($LASTEXITCODE -ne 0)
{
    $format | Select-Object -First 15 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkRed }
    Add-Failure 'Formatting differs. Run: dotnet format TtsUtilWin.sln'
}
else
{
    Write-Pass 'formatting clean'
}

Write-Step 'build, warnings as errors'
$build = dotnet build $Solution -c Debug --nologo -v q -warnaserror -p:SkipTests=true 2>&1
if ($LASTEXITCODE -ne 0)
{
    $build | Select-Object -Last 15 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkRed }
    Add-Failure 'The build failed or produced a warning.'
}
else
{
    Write-Pass 'build clean'
}

Write-Step 'core tests'
$coreTests = Join-Path $RepoRoot 'tests\TtsUtil.Core.Tests\TtsUtil.Core.Tests.csproj'
$tests = dotnet test $coreTests -c Debug --nologo -v q --no-build 2>&1
if ($LASTEXITCODE -ne 0)
{
    $tests | Select-Object -Last 15 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkRed }
    Add-Failure 'Core tests failed. The user interface tests run on push.'
}
else
{
    Write-Pass 'core tests pass'
}

Exit-WithFailures 'pre-commit' 'git commit --no-verify'
