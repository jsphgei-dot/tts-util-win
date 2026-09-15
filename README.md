# TTS Util Win

A Windows port of [TTS Util](https://github.com/jdanefinlay/tts-util-app) that synthesises
speech locally with [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx). No system speech
engine, no network access at run time, and your choice of an installer or a portable folder.

## What it does

* Read typed text, clipboard text, or a plain text file aloud.
* Write the same input to an MP3 or a wave file, into a TTS Util folder under Music.
* Insert custom silence for line endings, sentences, questions, and exclamations.
* Omit hash characters, web links, and mailto links from the audio.
* Read only letters and digits aloud by default, so symbols are never voiced as their names.
* Read each word back as you finish typing it.
* Import a PDF, including a scanned one, which is read with the OCR built into Windows.
* Pause and resume playback, and start reading from any line.
* Keep named scripts and reopen them, stored as ordinary text files.
* Pick a voice, a speaker within a multi speaker voice, and a speech rate.
* Search a long speaker list by name or number, and star the speakers you keep coming back to.

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

**From the program.** The **Voices** tab lists the curated models with their size, licence
and whether they are installed. Select one and press **Install**: it is downloaded, checked
and unpacked into the voices directory, with a progress bar and a Cancel button. **Remove**
deletes one again. A cancelled or failed install leaves nothing behind. When the configured
voices directory cannot be written, which happens for an all users installation under
Program Files, the download goes to `%LOCALAPPDATA%\TtsUtilWin\voices` instead, and the tab
says where it is writing.

**From a script.** `scripts\FetchVoices.ps1` does the same job for a portable copy or an
unattended setup. The three defaults were chosen for clear, free licences:

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

### What gets read aloud

By default only letters, digits and the punctuation that phrases speech reach the engine.
Everything else is dropped, because a text to speech engine happily says "dollar",
"percent" and "hash" out loud where a reader would say nothing.

* **Kept and voiced:** `a-z`, `A-Z`, `0-9`. Accented Latin letters are folded to their base
  letter, so `café` is read as `cafe` rather than broken into `caf`.
* **Kept as phrasing:** `. , ; : ? !` and their fullwidth forms. The engine pauses on these
  rather than naming them, which is exactly the behaviour asked for.
* **Dropped:** everything else. A symbol between two words leaves a gap so the words do not
  run together, while an apostrophe inside a word simply vanishes, so `don't` stays one
  word. Sentence ending punctuation already became silence in the chunker before this runs.

Settings has two controls for it. **Characters read aloud** chooses between the strict set
above, any Unicode letter or digit, or no filtering at all. **Also read these characters**
takes a list of extra characters to let through, for example `%$` if you want prices and
percentages spoken.

One consequence worth knowing: the strict default removes non Latin scripts entirely, so a
Chinese or Japanese voice reads nothing under it. Switch to "Any letter or digit" for
those. If a run ends up with nothing to say, the status bar says so and points at the
setting rather than finishing silently.

### Speakers

A multi speaker voice shows a speaker row under the voice picker. Names come from the
model's own metadata where it has any: piper models carry a `speaker_id_map`, so
`vits-piper-en_US-libritts_r-medium` lists its 904 speakers by LibriTTS reader id rather
than by position. Where a model says nothing, the speakers are numbered.

To name them yourself, drop a `speakers.txt` into the voice folder. A line is either
`id = name` or a bare name, bare lines numbering themselves from zero, and blank lines and
lines starting with `#` are ignored. Your names win over the model's.

```
# voices/vits-piper-en_US-libritts_r-medium/speakers.txt
12  = Narrator
307 = Warm, low
```

The search box filters by name or by leading digits of the number, and Star keeps a speaker
at the top of the list. Both the chosen speaker and its starred speakers are remembered per
voice, so switching voices and back returns you to where you were. Favourites shows only
the starred speakers, and shows everything again when pressed a second time.

## Saved audio

Save audio writes MP3 by default, which is roughly a tenth the size of the equivalent wave
file and plays in anything. Switch to WAV in Settings if you want uncompressed audio, and set
the MP3 bit rate there too. The file type actually written follows the extension you choose
in the save dialog, so you can override the default for one file.

Files go to a `TTS Util` folder inside Music unless you set another output directory. The
folder is created when it is first needed. After a file is written the status bar names it
and the folder is a link: click it to open that folder.

MP3 is encoded with the Media Foundation encoder built into Windows, so nothing extra is
installed. Encoding happens after synthesis, on the same background thread, so the window
stays responsive; the status bar says "Encoding MP3..." while it runs.

## Saved scripts

The Scripts tab keeps named texts you can come back to. Type a title, press **Save from Text
tab**, and the text is written as a `.txt` file; **Open in Text tab** loads one back, ready to
read. Rename and Delete do what they say, and Delete asks first.

Scripts are plain UTF-8 text files, one per script, named after the title. They live in
`%APPDATA%\TtsUtilWin\scripts`, or in a `scripts` folder beside the executable on a portable
copy. **Open folder** opens that location. Nothing is trapped in an application format: edit
the files in any editor, or drop one in from elsewhere and it appears in the list.

Characters Windows will not accept in a file name become spaces in the file name, so
`Chapter 1: the beginning` is stored as `Chapter 1 the beginning.txt`.

## Pause, resume and moving about

Pause holds the audio device and Resume carries on from the same place. Synthesis keeps a few
seconds of lookahead and then waits, so pausing does not quietly race ahead generating the
rest of the book. Stop gives up on the run entirely; pressing it while paused releases the
pause first, so nothing is left waiting.

There is no scrub bar, because the audio does not exist yet: it is synthesised as it plays.
Moving about is done by line instead. Stop, pick a line, and Read from here, which is why the
line list follows the run and leaves the selection where playback reached.

## Reading from a line

The Text tab shows a numbered line list beside the editor. Double click a line, or select it
and press **Read from here**, to start there instead of at the top. With nothing selected,
**Read from here** uses the line the cursor is on, so you can click into the text and carry
on from there.

The list follows the run: the line being spoken is selected and scrolled into view. That
makes Stop and **Read from here** a resume, since the selection is left on the line playback
reached. Untick **Lines** to hide the list and give the editor the full width.

## PDFs

The File tab takes a PDF as well as a text file. **Import to Text tab** puts the extracted
text in the Text tab so you can read it over and edit it before listening; **Read file** and
**Convert to WAV** go straight through.

Text comes from the PDF's own text layer, read with PdfPig, which covers anything produced
by a word processor or a typesetter. A page with no text layer is a scan, and its images are
passed to the OCR engine built into Windows, so nothing is downloaded and nothing leaves the
machine. A file is judged by its header rather than its extension, so a text file named
`.pdf` is still read as text.

The status line reports how many pages had no text layer and which pages yielded nothing, so
a partial read is visible rather than silent. If Windows has no OCR language pack installed,
scanned pages are reported as unreadable instead of failing.

## Requirements

* Windows 10 or 11, x64. OCR of scanned PDFs uses the Windows OCR engine, which needs a
  language pack Windows installs with its display languages.
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
