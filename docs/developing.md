[Documentation index](index.md) | [Usage](usage.md) | [Performance](performance.md) | [Build](building.md) | [Release](releasing.md) | [Develop](developing.md)

---

# 🧪 Develop and test

```powershell
dotnet build TtsUtilWin.sln
dotnet test tests\TtsUtil.Core.Tests
dotnet test tests\TtsUtil.App.Tests -p:SkipTests=true
```

`TtsUtil.Core` targets plain `net10.0` and holds the text and audio logic, so it is testable
without a UI. `TtsUtil.App` is the WPF front end. There are two test projects to match:
`TtsUtil.Core.Tests` for the platform neutral half, and `TtsUtil.App.Tests`, which builds the
real `MainWindow` on an STA dispatcher thread and raises Click events on its actual buttons.
The UI tests need no display and no voice model, and the whole suite runs in under a second.

**The tests gate every build of the app.** A `BeforeTargets="BeforeBuild"` target in
`TtsUtil.App.csproj` runs the core tests first and fails the build if any test fails, so a
broken pipeline can never reach a published executable. `BuildPortable.ps1` runs both suites up
front and refuses to publish on a failure. The UI tests are deliberately not in the csproj
target, since building the app under test from inside its own build would collide. To bypass
the gate during a fast edit loop:

```powershell
dotnet build src\TtsUtil.App -p:SkipTests=true
.\scripts\BuildPortable.ps1 -SkipTests
```

## Git hooks

Run once per clone:

```powershell
.\scripts\InstallHooks.ps1
```

That sets `core.hooksPath` to `.githooks`, so the hooks are versioned with the repository
rather than stranded in `.git\hooks`. Each is a small shell shim over a PowerShell script.

| Hook | Checks | Cost |
| --- | --- | --- |
| `pre-commit` | trailing whitespace and conflict markers in the staged diff, staged files over 5 MB, staged build output, `dotnet format --verify-no-changes`, a `-warnaserror` build, the core tests | about 8 s, and it skips the last four when no code is staged |
| `commit-msg` | Conventional Commits subject in lowercase with no trailing period, 72 character subject, blank line before the body, body of 200 characters or fewer with trailers excluded, no tool attribution line | instant |
| `pre-push` | the whole suite, user interface tests included | about 15 s |

`pre-commit` also runs `scripts\TestInstaller.ps1` when an `.iss` file is staged. That compiles
`installer\VersionTests.iss`, which includes the same `installer\Version.iss` the real installer
uses, runs it silently and checks 14 version comparison cases, so the upgrade, repair and
downgrade decisions are tested rather than assumed.

The heavy UI tests sit on push rather than commit so that committing stays quick. Bypass a
single run with `git commit --no-verify` or `git push --no-verify`, and a whole session with
`$env:SKIP_HOOKS = 1`. `.editorconfig` holds the formatting rules that `dotnet format` enforces,
so an editor and the hook agree.

## Versioning

Two numbers, the same split the Android original uses: a semantic **version name**
(`0.3.0-beta`) and a monotonic **version code** (`6`) that increases on every release and is
never reused. Both live in `Directory.Build.props`, both are compiled into the executable, and
the About tab shows them. Git tags match the version name, prefixed with `v`.
