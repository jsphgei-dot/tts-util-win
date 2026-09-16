# TTS Util Win

![version](https://img.shields.io/badge/version-0.6.0--beta-blue)
![platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011%20x64-0078D6)
![built with](https://img.shields.io/badge/.NET-6.0-512BD4)
![tests](https://img.shields.io/badge/tests-390%20passing-brightgreen)
![licence](https://img.shields.io/badge/licence-Apache%202.0-lightgrey)

> Read anything you can type, paste or open out loud, entirely on your own machine.

## ✨ Highlights

* **Offline, always.** The voice runs locally. Nothing is uploaded, nothing is fetched while
  you read, and no system speech engine is involved. It reaches the network twice only: to
  download a voice you asked for, and for the once a day update check, which sends nothing and
  can be switched off.
* **Real neural voices.** Piper, Kokoro and Matcha models through
  [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx), including one with over 900 speakers.
* **Reads what a reader would read.** Symbols, links and stray punctuation are filtered out
  instead of being spoken as "dollar", "hash" and "https colon slash slash".
* **Text, files, PDFs and scans.** Scanned pages go through the OCR already in Windows.
* **Listen or keep it.** Play it back, or write the same input to an MP3 or a wave file.
* **Queue and repeat.** Line up a run of scripts, loop one of them, or loop the lot.
* **Batch conversion.** Tick several saved scripts and write them all to audio files at once.
* **It keeps your place.** Every text tab is still there when you open the program again, audio
  written from a script is offered that script's title as its file name, and the words behind a
  recording are saved as a script under the name you gave the file.
* **Each script remembers its voice.** Saving a script keeps the voice, the speaker and the
  talking speed with it, and opening or converting it reads it back the way it sounded.
* **Several texts at once.** New tab gives you another text window, and each one writes its audio
  in the background on a voice of its own, so you can set several recordings going and carry on
  reading in another tab.
* **Installer or portable.** One build gives both, and the portable copy touches nothing
  outside its own folder.

## 📖 Overview

TTS Util Win turns text into speech on a Windows machine with no network access and no Windows
voices. You give it typed text, the clipboard, a text file or a PDF; it picks the text apart
into sentences, filters out what should not be spoken, and streams the audio from a neural
model as it is generated, so a long document starts playing in about a second rather than
after the whole thing has been synthesised.

The speech comes from [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx) running ONNX models
on the CPU. Voices are ordinary folders you can add, remove and back up. The program ships
with none and offers a curated list to download, so you choose what lands on the disk.

### How it relates to TTS Util

This is a Windows port of [TTS Util](https://github.com/drmfinlay/tts-util-app), the Android
application by Dane Finlay, and it keeps that program's ideas: read aloud or save to a file,
custom silence around sentences and line endings, filters for the things a reader would skip.
What is new here is the engine (Android hands text to the system TTS service, this carries its
own), PDF and OCR import, the script library and the queue.

### Who made it

Written by [jsphgei-dot](https://github.com/jsphgei-dot). The original TTS Util is by Dane
Finlay. See `NOTICE` for the full attribution.

This application was coded using AI assistance.

## ⬇️ Install

**Download a build.** Builds are published to the public distribution repository,
[jsphgei-dot/tts-util-win-releases](https://github.com/jsphgei-dot/tts-util-win-releases/releases/latest),
as `TtsUtilWin-<version>-setup.zip` and `TtsUtilWin-<version>-portable.zip`, each with its
SHA256 on the release page. Nothing is committed to this repository: `dist/` is ignored, so the
executable and the installer are always either downloaded from a release or built locally.

**Installer.** `TtsUtilWin-<version>-setup.exe`, inside the setup zip, asks on its first page
whether to install for all users (needs administrator) or just for you (recommended, no
prompt), then adds a Start menu entry, an optional desktop shortcut, and an entry in Settings,
Apps. Settings go to `%APPDATA%\TtsUtilWin`.

A components page lists the curated voices with a checkbox and a size each. "Program and the
three recommended voices" ticks the permissively licensed defaults, "Program only" ticks none,
and "Choose voices" leaves every box to you. Setup downloads what is ticked and extracts it
into `voices` beside the program, skipping any model already present. A failed download warns
and lets the installation finish. Uninstalling removes the voices it installed.

**Portable.** Copy the `TtsUtilWin` folder anywhere. The `portable.txt` file beside the
executable keeps settings and voices inside that folder, so nothing touches your profile.

```
TtsUtilWin\
  TtsUtilWin.exe        the app, the .NET runtime, and the sherpa native libraries
  settings.json         written on first run, beside the exe when that is writable
  voices\
    vits-piper-en_US-ljspeech-high\
    kokoro-en-v0_19\
  scripts\
    FetchVoices.ps1     the downloader, which fills the voices folder above
```

Saved scripts land in that same `scripts` folder, as plain `.txt` files.

**Upgrading.** Setup recognises an existing installation by its application id. A newer setup
upgrades in place, keeping the folder, shortcuts, settings and the voices already downloaded.
The same version offers a repair. An older setup warns that it would downgrade and asks for
confirmation. A running copy is closed through the Restart Manager rather than failing on a
locked file. There is no need to uninstall first.

**Requirements.** Windows 10 or 11, x64. The published executable is self contained and needs
no runtime installed. OCR of scanned PDFs uses the Windows OCR engine, which needs a language
pack, the kind Windows installs with its display languages.

## 🎧 Usage

### Voices

The **Voices** tab lists the curated models with their size, licence and whether they are
installed. Select one and press **Install**: it is downloaded, checked and unpacked, with a
progress bar and a Cancel button. **Remove** deletes one again, and a cancelled or failed
install leaves nothing behind. When the configured voices directory cannot be written, which
happens for an all users installation under Program Files, the download goes to
`%LOCALAPPDATA%\TtsUtilWin\voices` instead and the tab says so.

The three defaults were chosen for clear, free licences:

| Model | Voice | Licence |
| --- | --- | --- |
| `vits-piper-en_US-ljspeech-high` | English (US), single speaker | LJ Speech data set, public domain |
| `vits-piper-en_US-libritts_r-medium` | English (US), 900+ speakers | LibriTTS-R, CC BY 4.0 |
| `kokoro-en-v0_19` | English, 11 voices | Apache 2.0 |

The installer offers these three plus `vits-piper-en_GB-alan-medium`,
`vits-piper-en_US-amy-low` and `vits-piper-en_US-lessac-medium`.

For a portable copy or an unattended setup, `scripts\FetchVoices.ps1` does the same job:

```powershell
.\scripts\FetchVoices.ps1 -List          # the whole catalogue
.\scripts\FetchVoices.ps1 -Default       # the three defaults, about 490 MB
.\scripts\FetchVoices.ps1 -Name kokoro-en-v0_19
```

On an all users installation the program folder is under Program Files, so running the script
there needs an elevated PowerShell, or a `-Destination` you can write to. The authoritative
licence for a model is the `LICENSE` or `MODEL_CARD` file inside its folder, and the About tab
shows it for the selected voice.

The voices directory is resolved in this order, first hit wins:

1. `--voices <path>` on the command line
2. the `TTSUTIL_VOICES` environment variable
3. the Settings tab, stored in `settings.json`
4. `%LOCALAPPDATA%\TtsUtilWin\voices`
5. a `voices` folder beside the executable

Press **Rescan** after adding a model by hand.

#### Other languages

The Voices tab also lists Spanish (Spain and Mexico), French, German, Italian, Portuguese
(Brazil), Dutch, Polish, Swedish, Turkish, Vietnamese, Russian, Ukrainian, Greek, Arabic,
Hindi and Chinese. None is installed by default and none is offered as a setup checkbox: they
are there to install when you want one.

Anything outside the Latin alphabet, which is Russian, Ukrainian, Greek, Arabic, Hindi and
Chinese here, needs **Characters read aloud** set to any letter or digit in Settings,
otherwise the strict default removes the whole script before it reaches the voice. Installing
one of those says so. Vietnamese is Latin but heavily accented: under the strict setting its
accents fold away, so it wants the wider setting too.

#### Speakers

A multi speaker voice shows a speaker row under the voice picker. Names come from the model's
own metadata where it has any: piper models carry a `speaker_id_map`, so
`vits-piper-en_US-libritts_r-medium` lists its 904 speakers by LibriTTS reader id rather than
by position. Where a model says nothing, the speakers are numbered.

To name them yourself, drop a `speakers.txt` into the voice folder. A line is either
`id = name` or a bare name, bare lines numbering themselves from zero, and blank lines and
lines starting with `#` are ignored. Your names win over the model's.

```
# voices/vits-piper-en_US-libritts_r-medium/speakers.txt
12  = Narrator
307 = Warm, low
```

The search box filters by name or by leading digits of the number, and Star keeps a speaker at
the top of the list. Both the chosen speaker and its starred speakers are remembered per voice.
Favourites shows only the starred speakers, and shows everything again when pressed a second
time.

### Writing and editing

The Text tab opens with a title box above the text and a save icon beside it. Type a name,
press the icon or **Ctrl+S**, and the text is written to the script library under that name.
The same title shows on the Scripts tab, so saving from either place uses one name.

Under it is the editing toolbar. Scripts are plain text files, so there is no bold, no italic
and no colour: everything here changes the words themselves and survives a round trip through
a `.txt` file.

| Tool | What it does |
| --- | --- |
| Undo, redo, cut, copy, paste | The usual, on the text box |
| Bulleted list, numbered list | Marks the selected lines, or every line. Pressing again takes the marks off |
| Indent, outdent | Four spaces on or off the front of each line |
| AA, aa, Aa, Ab | Upper case, lower case, sentence case, title case. Words already in capitals are left alone |
| Clean up | Join wrapped lines, collapse blank lines, tidy spacing |
| Find and replace | A strip under the toolbar, also on **Ctrl+H** |
| Size, Wrap | How the editor looks. Neither changes a saved file |

A tool with text selected works on whole lines, never half of one. With nothing selected it
works on everything. **Join wrapped lines** is the one to reach for after pasting from a PDF:
it puts a sentence split over four lines back together, and leaves paragraphs and list items
alone.

### Reading text

Type or paste into the Text tab and press **Read**. **Read as I type** speaks each word as you
finish it, never single letters. The voice, the speaker and the speech rate sit along the top.

The playback buttons carry icons rather than words: play, pause, stop, save audio, and an arrow
for **Read from here** above the line list. Resting the pointer on one names it, and screen
readers get the same name, so nothing is lost by dropping the captions.

### The media keys

The play, pause and stop keys on a keyboard work while the program is running, and the Windows
media overlay shows the script title with the same three buttons. They drive the reading that
is already in flight, so pause from the keyboard is the same pause as the button. On a machine
where Windows will not hand out the transport controls, the program carries on without them and
says nothing.

### Moving about while it reads

Pause holds the audio device and Resume carries on from the same place. Synthesis keeps a few
seconds of lookahead and then waits, so pausing does not quietly race ahead generating the rest
of the book. Stop gives up on the run entirely; pressing it while paused releases the pause
first, so nothing is left waiting.

There is no scrub bar, because the audio does not exist yet: it is synthesised as it plays.
Moving about is done by line instead, and Stop is no part of it. While a reading is in flight,
**Read** turns into **Restart** and starts the same reading again from where it began. **Read
from here**, which sits above the line list, and a double click in the line list, move the
reading to that line without stopping first. Each of these stops the current run, waits for it to unwind, and starts the next one.

A restart also sounds the same as the reading it replaces. The voice draws its prosody from a
random generator that runs on from one sentence to the next, so reading a passage twice with
the same voice gives two different deliveries. Every run therefore begins by winding the voice
back to the state it loaded in, which takes about three quarters of a second on a Piper medium
model and is reported as *Preparing the voice*. The same applies to a repeat and to each entry
in the queue: a passage read twice is read identically.

The Text tab shows a numbered line list beside the editor. Double click a line, or select it
and press **Read from here**, to start there instead of at the top. With nothing selected,
**Read from here** uses the line the cursor is on, so you can click into the text and carry on
from there, whether or not something is already playing.

The list follows what you are hearing, not what has been synthesised: audio is generated
several seconds ahead of the speaker, so following synthesis left the highlight a line or two
early. The reading is marked as it is queued and the list follows the marks as they play. That
makes Stop and **Read from here** a resume, since the selection is left on the line playback
reached. Untick **Lines** to hide the list and give the editor the full width.

### Files and PDFs

The File tab takes a text file or a PDF. **Import to Text tab** puts the extracted text in the
Text tab so you can read it over and edit it before listening; **Read file** and **Convert to
WAV** go straight through.

Text comes from the PDF's own text layer, read with PdfPig, which covers anything produced by a
word processor or a typesetter. A page with no text layer is a scan, and its images are passed
to the OCR engine built into Windows, so nothing is downloaded and nothing leaves the machine.
A file is judged by its header rather than its extension, so a text file named `.pdf` is still
read as text.

The status line reports how many pages had no text layer and which pages yielded nothing, so a
partial read is visible rather than silent. If Windows has no OCR language pack installed,
scanned pages are reported as unreadable instead of failing.

### Saving audio

Save audio writes MP3 by default, which is roughly a tenth the size of the equivalent wave file
and plays in anything. Switch to WAV in Settings if you want uncompressed audio, and set the
MP3 bit rate there too. The file type actually written follows the extension you choose in the
save dialog, so you can override the default for one file.

Files go to a `TTS Util` folder inside Music unless you set another output directory. The
folder is created when it is first needed. After a file is written the status bar names it and
the folder is a link: click it to open that folder.

MP3 is encoded with the Media Foundation encoder built into Windows, so nothing extra is
installed. Encoding happens after synthesis, on the same background thread, so the window stays
responsive; the status bar says "Encoding MP3..." while it runs.

### Scripts

The Scripts tab keeps named texts you can come back to. Type a title, press **Save from Text
tab**, and the text is written as a `.txt` file; **Open in Text tab** loads one back, ready to
read. Rename and Delete do what they say, and Delete asks first.

Scripts are plain UTF-8 text files, one per script, named after the title. They live in
`%APPDATA%\TtsUtilWin\scripts`, or in a `scripts` folder beside the executable on a portable
copy. **Open folder** opens that location. Nothing is trapped in an application format: edit
the files in any editor, or drop one in from elsewhere and it appears in the list.

Characters Windows will not accept in a file name become spaces, so `Chapter 1: the beginning`
is stored as `Chapter 1 the beginning.txt`.

Each script in the list has a tick box. Tick the ones you want, press **Convert ticked to
audio**, choose a folder, and each is written as its own file in the format set in Settings.
They are written one at a time; **Stop** gives up on the rest. A name already in use gains a
number rather than writing over the file that is there. **Tick all** and **Clear** work the
list in one go.

### The queue

The panel on the right of the Scripts tab is a playlist. The **+** button adds whatever is
selected in the script list, or the Text tab itself when nothing is selected, so a chapter you
are drafting can sit in a run of saved ones. The arrows reorder, the bin removes the selected
entry and the broom empties the queue. The play button starts from the top, a double click on
an entry starts from there, and the highlight moves down the list as the reading moves on.

The repeat button cycles three ways and the label beside it says which is in force:

| Mode | What happens at the end of an entry |
| --- | --- |
| Off | the queue moves on, and stops after the last entry |
| Repeat one | the same entry is read again, for as long as you leave it |
| Repeat all | the queue moves on, and starts again from the top after the last entry |

Repeat one works on a single reading as much as on the queue, so the Text tab can be left
looping without pressing Read each time. Stop ends the repeat, whichever mode is set, and the
mode is remembered between sessions. An entry that cannot be read (an empty script, a file that
has gone) is reported and skipped rather than retried for ever.

### Settings

Every setting that needs explaining has a question mark beside it. Press one and a panel opens
with what the setting does, the range it accepts, the default, and an example where an example
helps. Press elsewhere to close it.

The panels cover the silence values, scaling silence to the speech rate, the hash and link
filters, both character settings, the two folders, threads, chunk length, the saved audio
format and the MP3 bit rate. The text lives in `SettingsHelp` in Core rather than in the XAML,
so it is readable and testable without opening the window.

Two groups are worth knowing about before you go looking. **Silence** sets how long a pause
follows a line ending, a sentence, a question and an exclamation, in milliseconds, and can be
scaled with the speech rate so a fast reading does not sit in long gaps. **Filters** drop hash
characters, web links and mailto links before they reach the voice, since a spoken URL is
noise rather than information.

#### What gets read aloud

By default only letters, digits and the punctuation that phrases speech reach the engine.
Everything else is dropped, because a text to speech engine happily says "dollar", "percent"
and "hash" out loud where a reader would say nothing.

* **Kept and voiced:** `a-z`, `A-Z`, `0-9`. Accented Latin letters are folded to their base
  letter, so `café` is read as `cafe` rather than broken into `caf`.
* **Kept as phrasing:** `. , ; : ? !` and their fullwidth forms. The engine pauses on these
  rather than naming them.
* **Dropped:** everything else. A symbol between two words leaves a gap so the words do not run
  together, while an apostrophe inside a word simply vanishes, so `don't` stays one word.
  Sentence ending punctuation already became silence in the chunker before this runs.

Settings has two controls for it. **Characters read aloud** chooses between the strict set
above, any Unicode letter or digit, or no filtering at all. **Also read these characters** lets
extra characters through: type the characters themselves, one after another, with nothing
between them. There is no separator, so `%$&+` allows all four. A comma typed there means the
comma character rather than a separator, and spaces do nothing.

One consequence worth knowing: the strict default removes non Latin scripts entirely, so a
Chinese or Japanese voice reads nothing under it. If a run ends up with nothing to say, the
status bar says so and points at the setting rather than finishing silently.

### The status bar

The bar along the bottom shows one message at a time, which used to mean a finished run, a
filtered count, or an error vanished as soon as the next message arrived. **History** opens the
last 50 messages, newest first, as text that can be selected and copied, and that scrolls once
there is more of it than the panel holds. Repeats are collapsed, so a progress figure that
updates many times a second does not crowd out everything else.

### Updates

Once a day at most, the program reads one published file,
`latest.json` in the public distribution repository, and compares its version code with the
running build. Nothing is sent with the request: no identifier, no text, no list of voices, and
no request at all while the setting is off.

What happens next depends on how the program was installed. An **installed** copy offers to
download the setup program, checks it against the SHA256 published in the manifest, and hands
over to it, since setup already knows how to upgrade in place and keep settings and voices. A
**portable** copy is told where the release is and left to unpack it, because replacing a folder
it may be running from is not the program's business. Saying no to a version means that version,
not every version after it.

Turn it off with **Look for a new version once a day** in Settings. **Check now**, beside that
box, looks straight away whatever the box says, and says so when there is nothing newer.

## 🛠️ Build from source

Building takes about a minute and needs only the .NET 6 SDK.

```powershell
# 1. Download voices (about 490 MB for the three permissively licensed defaults).
.\scripts\FetchVoices.ps1 -Default

# 2. Build the portable executable into dist\TtsUtilWin.
.\scripts\BuildPortable.ps1

# 3. Run it.
.\dist\TtsUtilWin\TtsUtilWin.exe
```

The installer needs [Inno Setup 6](https://jrsoftware.org/isinfo.php) as well:
`winget install JRSoftware.InnoSetup`. The build script finds it automatically, whether winget
installed it per user or per machine.

```powershell
.\scripts\BuildPortable.ps1                 # portable folder only
.\scripts\BuildPortable.ps1 -Installer      # portable folder and the setup program
.\scripts\BuildPortable.ps1 -Installer -IncludeVoices   # also copy the voice models in
```

| What | Path | Size |
| --- | --- | --- |
| Portable folder | `dist\TtsUtilWin\` | 70 MB, plus voices |
| Portable executable | `dist\TtsUtilWin\TtsUtilWin.exe` | 70 MB |
| Installer | `dist\TtsUtilWin-<version>-setup.exe` | 65 MB |

| Switch | Effect |
| --- | --- |
| `-Installer` | Compiles `installer\TtsUtilWin.iss` after publishing |
| `-IncludeVoices` | Copies `voices\` into the portable folder, so it can be zipped and handed over whole |
| `-SkipTests` | Skips both test suites. The build normally refuses to publish if any test fails |
| `-OutputDirectory <path>` | Publishes somewhere other than `dist\TtsUtilWin` |
| `-Configuration`, `-Runtime` | Default to `Release` and `win-x64` |

The version in the installer filename, in its Apps entry and in the executable's file
properties all come from `Directory.Build.props`. Nothing needs editing in two places. To
publish a build for other people, attach the artifacts to a GitHub release:

```powershell
# Build, zip, hash, and write dist\latest.json without publishing anything.
.\scripts\Publish.ps1

# The same, then create the release in the public distribution repository and
# push the manifest that makes the update notice appear.
.\scripts\Publish.ps1 -Publish -NotesFile dist\RELEASE_NOTES_v0.3.0-beta.md
```

The manifest is pushed **after** the assets, so nobody is pointed at a download that is not
there yet. The seed files for that public repository, its README and the changelog, live in
`distribution\` here.

### Code signing

The build is unsigned by default, which is why SmartScreen warns the first time a setup runs.
Both scripts take a certificate thumbprint and sign with it when one is given:

```powershell
.\scripts\Publish.ps1 -Publish -NotesFile dist\notes.md -CertThumbprint ABC123DEF456
```

The certificate is looked up in the current user's store by thumbprint, so nothing secret is
written down here. `TtsUtilWin.exe` is signed before the installer is compiled, so the
installed copy carries the signature too, and the setup is signed after Inno Setup builds it.
Both signatures are timestamped, which keeps them valid after the certificate expires, and
both are verified with `signtool verify /pa` before the build is allowed to finish. Leave
`-CertThumbprint` off and the build is unsigned exactly as before.

## 🧪 Develop and test

```powershell
dotnet build TtsUtilWin.sln
dotnet test tests\TtsUtil.Core.Tests
dotnet test tests\TtsUtil.App.Tests -p:SkipTests=true
```

`TtsUtil.Core` targets plain `net6.0` and holds the text and audio logic, so it is testable
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

### Git hooks

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

### Versioning

Two numbers, the same split the Android original uses: a semantic **version name**
(`0.3.0-beta`) and a monotonic **version code** (`6`) that increases on every release and is
never reused. Both live in `Directory.Build.props`, both are compiled into the executable, and
the About tab shows them. Git tags match the version name, prefixed with `v`.

## 🤝 Contributing and feedback

Bug reports and voice suggestions are welcome through
[issues on the distribution repository](https://github.com/jsphgei-dot/tts-util-win-releases/issues),
which is the one anybody can reach. A report that names the voice, the
setting and the text that misbehaved is worth a great deal, since most of the awkward cases live
in the text rather than in the code. Run `.\scripts\InstallHooks.ps1` before your first commit so
the formatting and test gates run locally rather than surprising you later.

## 📚 Further reading

* `SPEC.md`, what is built, what is tested and what is left.
* `DECISIONS.md`, why the awkward choices were made the way they were.
* [TTS Util](https://github.com/drmfinlay/tts-util-app), the Android original.
* [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx), the engine, and its
  [voice catalogue](https://github.com/k2-fsa/sherpa-onnx/releases/tag/tts-models).

## ⚖️ Licence

Apache 2.0, the same licence as the original TTS Util by Dane Finlay and as sherpa-onnx.

Three files travel with every build, in the installed folder and in the portable folder alike:
`LICENSE` is the Apache 2.0 text, `NOTICE` names the work this one is derived from, and
`THIRD-PARTY-NOTICES.txt` carries the licence of every component that ships inside the
executable (sherpa-onnx, ONNX Runtime, PdfPig, NAudio, and the .NET runtime itself).

Voice models are not covered by any of that. Each carries its own licence, listed above and
restated in the `LICENSE` or `MODEL_CARD` file inside the model folder.
