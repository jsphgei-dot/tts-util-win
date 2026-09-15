# Runs before every push: the whole suite, user interface tests included.

. (Join-Path $PSScriptRoot 'common.ps1')

if (Test-HooksSkipped 'pre-push') { exit 0 }

Write-Host 'pre-push' -ForegroundColor White

Write-Step 'full test suite'
$tests = dotnet test $Solution -c Debug --nologo -v q -p:SkipTests=true 2>&1

if ($LASTEXITCODE -ne 0)
{
    $tests | Select-Object -Last 25 | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkRed }
    Add-Failure 'Tests failed.'
}
else
{
    $tests | Where-Object { $_ -match 'Passed!' } | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkGray }
    Write-Pass 'all tests pass'
}

Exit-WithFailures 'pre-push' 'git push --no-verify'
