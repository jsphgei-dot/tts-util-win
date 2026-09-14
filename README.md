# TTS Util Win

A Windows port of [TTS Util](https://github.com/jdanefinlay/tts-util-app) that synthesises
speech locally with [sherpa-onnx](https://github.com/k2-fsa/sherpa-onnx). It ships as a
portable executable: no installer, no system speech engine, no network access at run time.

## What it does

* Read typed text, clipboard text, or a plain text file aloud.
* Write the same input to a wave file.
* Insert custom silence for line endings, sentences, questions, and exclamations.
* Omit hash characters, web links, and mailto links from the audio.
* Read letters and words back as you type them.
* Pick a voice, a speaker within a multi speaker voice, and a speech rate.

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

`BuildPortable.ps1 -IncludeVoices` copies the models into the output folder so the whole
directory can be zipped and handed to someone else.

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

Run the script with `-List` for the rest of the catalogue, and `-Name <id>` to fetch one.
The authoritative licence for a model is the `LICENSE` or `MODEL_CARD` file inside its
folder; the About tab displays it for the selected voice.

## Requirements

* Windows 10 or 11, x64.
* .NET 6 SDK to build. The published executable is self contained and needs no runtime.

## Building and testing

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

## Licence

Apache 2.0, the same licence as the original TTS Util by Dane Finlay and as sherpa-onnx.
See `LICENSE` and `NOTICE`. Voice models carry their own licences, listed above.
