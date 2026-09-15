# TTS Util Win

A Windows port of [TTS Util](https://github.com/jdanefinlay/tts-util-app) that synthesises
speech locally with [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx). No system speech
engine, no network access at run time, and your choice of an installer or a portable folder.

## What it does

* Read typed text, clipboard text, or a plain text file aloud.
* Write the same input to a wave file.
* Insert custom silence for line endings, sentences, questions, and exclamations.
* Omit hash characters, web links, and mailto links from the audio.
* Read each word back as you finish typing it.
* Pick a voice, a speaker within a multi speaker voice, and a speech rate.

## Where to get a build

Published builds, when there are any, are attached to the
[Releases page](https://github.com/jsphgei-dot/tts-util-win/releases). Nothing is committed
to the repository itself: `dist/` is ignored, so the executable and the installer are always
either downloaded from a release or built locally.

Building takes about a minute and needs only the .NET 6 SDK.

## Quick start

```powershell
# 1. Download voices (about 490 MB for the three permissively licensed defaults).
.\scripts\FetchVoices.ps1 -List
.\scripts\FetchVoices.ps1 -Default

# 2. Build the portable executable into dist\TtsUtilWin.
.\scripts\BuildPortable.ps1

# 3. Run it.
.\dist\TtsUtilWin\TtsUtilWin.exe
```

## Building a release

**Prerequisites**

* .NET 6 SDK, for everything.
* [Inno Setup 6](https://jrsoftware.org/isinfo.php), only if you want the installer:
  `winget install JRSoftware.InnoSetup`. The build script finds it automatically, whether
  winget installed it per user or per machine.

**Commands**

```powershell
.\scripts\BuildPortable.ps1                 # portable folder only
.\scripts\BuildPortable.ps1 -Installer      # portable folder and the setup program
.\scripts\BuildPortable.ps1 -Installer -IncludeVoices   # also copy the voice models in
```

**Outputs**

| What | Path | Size |
| --- | --- | --- |
| Portable folder | `dist\TtsUtilWin\` | 70 MB, plus voices |
| Portable executable | `dist\TtsUtilWin\TtsUtilWin.exe` | 70 MB |
| Installer | `dist\TtsUtilWin-<version>-setup.exe` | 65 MB |

**Switches**

| Switch | Effect |
| --- | --- |
| `-Installer` | Compiles `installer\TtsUtilWin.iss` after publishing |
| `-IncludeVoices` | Copies `voices\` into the portable folder, so it can be zipped and handed over whole |
| `-SkipTests` | Skips both test suites. The build normally refuses to publish if any test fails |
| `-OutputDirectory <path>` | Publishes somewhere other than `dist\TtsUtilWin` |
| `-Configuration`, `-Runtime` | Default to `Release` and `win-x64` |

The version in the installer filename, in its Apps entry and in the executable's file
properties all come from `Directory.Build.props`. Nothing needs editing in two places.

To publish a build for other people, attach the two artifacts to a GitHub release:

```powershell
gh release create v0.1.0-alpha `
  dist\TtsUtilWin-0.1.0-alpha-setup.exe `
  --title "0.1.0-alpha" --notes "First tagged build."
```

## Installing, or not

Two ways to run it, from one build:

* **Installer.** `scripts\BuildPortable.ps1 -Installer` produces
  `dist\TtsUtilWin-<version>-setup.exe`. Its first page asks whether to install for all
  users (needs administrator) or just for you (recommended, no prompt), then adds a Start
  menu entry, an optional desktop shortcut, and an entry in Settings, Apps. Settings go to
  `%APPDATA%\TtsUtilWin`.
* **Voices during setup.** A components page lists the curated models with a checkbox and
  a size each. "Program and the three recommended voices" ticks the permissively licensed
  defaults, "Program only" ticks none, and "Choose voices" leaves every box to you. Setup
  downloads what is ticked from the sherpa-onnx release page and extracts it into `voices`
  beside the program, skipping any model already present. A failed download warns and lets
  the installation finish; run `FetchVoices.ps1` afterwards for whatever is missing.
  Uninstalling removes the voices it installed.
* **Reinstalling and upgrading.** Setup recognises an existing installation by its
  application id and acts on the version it finds. A newer setup upgrades in place, keeping
  the folder, shortcuts, settings and the voices already downloaded, and the ready page says
  so. The same version offers a repair, which replaces the program files and fetches any
  ticked voice that is missing. An older setup warns that it would downgrade and asks for
  confirmation. A running copy is closed through the Restart Manager rather than failing on
  a locked file, and the folder page is skipped when a previous install is found. There is
  no need to uninstall first.
* **Portable.** Copy `dist\TtsUtilWin` anywhere. The `portable.txt` file beside the
  executable keeps settings and voices inside that folder, so nothing touches the profile.

The voices directory is resolved in this order, first hit wins:

1. `--voices <path>` on the command line
2. the `TTSUTIL_VOICES` environment variable
3. the Settings tab, stored in `settings.json`
4. `%LOCALAPPDATA%\TtsUtilWin\voices`
5. a `voices` folder beside the executable

## Layout of a portable install

```
TtsUtilWin\
  TtsUtilWin.exe        the app, the .NET runtime, and the sherpa native libraries
  settings.json         written on first run, beside the exe when that is writable
  voices\
    vits-piper-en_US-ljspeech-high\
    kokoro-en-v0_19\
```

The app looks for voices in `voices` beside the executable, then in the nearest `voices`
folder above it, and finally wherever the Settings tab points. Press **Rescan** after
adding a model.

## Voices

`scripts\FetchVoices.ps1` pulls models published by the sherpa-onnx project. The three
defaults were chosen for clear, free licences:

| Model | Voice | Licence |
| --- | --- | --- |
| `vits-piper-en_US-ljspeech-high` | English (US), single speaker | LJ Speech data set, public domain |
| `vits-piper-en_US-libritts_r-medium` | English (US), 900+ speakers | LibriTTS-R, CC BY 4.0 |
| `kokoro-en-v0_19` | English, 11 voices | Apache 2.0 |

The installer offers these three plus `vits-piper-en_GB-alan-medium`,
`vits-piper-en_US-amy-low` and `vits-piper-en_US-lessac-medium` as checkboxes.

Run the script with `-List` for the rest of the catalogue, and `-Name <id>` to fetch one.
On an all users installation the program folder is under Program Files, so running the
script there needs an elevated PowerShell, or a `-Destination` you can write to.
The authoritative licence for a model is the `LICENSE` or `MODEL_CARD` file inside its
folder; the About tab displays it for the selected voice.

## Requirements

* Windows 10 or 11, x64.
* .NET 6 SDK to build, plus Inno Setup 6 for the installer. The published executable is
  self contained and needs no runtime on the machine that runs it.

## Developing and testing

```powershell
dotnet build TtsUtilWin.sln
dotnet test tests\TtsUtil.Core.Tests
dotnet test tests\TtsUtil.App.Tests -p:SkipTests=true
```

`TtsUtil.Core` targets plain `net6.0` and holds the text and audio logic, so it is testable
without a UI. `TtsUtil.App` is the WPF front end.

There are two test projects: `TtsUtil.Core.Tests` for the platform neutral half, and
`TtsUtil.App.Tests`, which builds the real `MainWindow` on an STA dispatcher thread and
raises Click events on its actual buttons. The UI tests need no display and no voice model,
and the whole suite runs in under a second.

**The tests gate every build of the app.** A `BeforeTargets="BeforeBuild"` target in
`TtsUtil.App.csproj` runs the core tests first and fails the build if any test fails, so a
broken pipeline can never reach a published executable. `BuildPortable.ps1` runs both
suites up front and refuses to publish on a failure. The UI tests are deliberately not in
the csproj target, since building the app under test from inside its own build would
collide. To bypass the gate during a fast edit loop:

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
| `commit-msg` | Conventional Commits subject in lowercase with no trailing period, 72 character subject, blank line before the body, body of 200 words or fewer, no tool attribution line | instant |
| `pre-push` | the whole suite, user interface tests included | about 15 s |

`pre-commit` also runs `scripts\TestInstaller.ps1` when an `.iss` file is staged. That
compiles `installer\VersionTests.iss`, which includes the same `installer\Version.iss` the
real installer uses, runs it silently and checks 14 version comparison cases, so the
upgrade, repair and downgrade decisions are tested rather than assumed.

The heavy UI tests sit on push rather than commit so that committing stays quick. Bypass a
single run with `git commit --no-verify` or `git push --no-verify`, and a whole session with
`$env:SKIP_HOOKS = 1`. `.editorconfig` holds the formatting rules that `dotnet format`
enforces, so an editor and the hook agree.

## Versioning

Two numbers, the same split the Android original uses: a semantic **version name**
(`0.1.0-alpha`) and a monotonic **version code** (`1`) that increases on every release and is
never reused. Both live in `Directory.Build.props`, both are compiled into the executable,
and the About tab shows them. Git tags match the version name, prefixed with `v`.

See `DECISIONS.md` for the rules and for the open questions about cross platform support.

## Licence

Apache 2.0, the same licence as the original TTS Util by Dane Finlay and as sherpa-onnx.
See `LICENSE` and `NOTICE`. Voice models carry their own licences, listed above.
