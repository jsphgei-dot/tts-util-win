# TTS Util Win

[![Version](https://img.shields.io/badge/version-0.12.0--beta-blue.svg)](https://github.com/jsphgei-dot/tts-util-win/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-green.svg)](#requirements)
[![License](https://img.shields.io/badge/license-Apache%202.0-lightgrey.svg)](LICENSE)

> Reads text aloud on Windows, offline.

## 📦 This repository is retired

It carried the downloads while the source repository was still private, which was always meant
to be temporary, and it lasted only as long as it took to settle what the source repository
should look like in public. Both now live in one place:

**[jsphgei-dot/tts-util-win](https://github.com/jsphgei-dot/tts-util-win)**, which has the
source, the releases, the changelog and the issue tracker.

Nothing new is published here. The releases already on this page stay exactly where they are,
so a copy built before 0.12.0-beta keeps finding the manifest and the downloads it was told to
look for, and updates itself one last time into a copy that asks the new address.

The program speaks locally with sherpa-onnx voice models: no account, no key, no text leaving
the machine.

## ⬇️ Download

Take the newest build from [Releases](https://github.com/jsphgei-dot/tts-util-win/releases/latest).

| Download | For |
| --- | --- |
| `TtsUtilWin-<version>-setup.zip` | Unpack and run the installer. Adds Start menu entries, upgrades in place, can fetch voices for you |
| `TtsUtilWin-<version>-portable.zip` | Unpack anywhere and run `TtsUtilWin.exe`. Settings and voices stay in the folder |

Every release lists the SHA256 of both files, and `latest.json` carries the same hashes. The
program refuses any update download whose hash does not match.

### Requirements

Windows 10 version 1607 or newer, x64. The executable is self contained, so no runtime needs
installing.

| | Minimum | Recommended |
| --- | --- | --- |
| Windows | 10 version 1607 | 11, or 10 22H2 |
| Processor | two cores | four cores or more |
| Memory | 4 GB | 8 GB |
| Free disk | 250 MB for the program | that, plus 60 to 300 MB for each neural voice |
| Audio | any output device, or none at all if you only write files | |

The minimum column is what runs: the Windows voices, the smaller neural voices, and reading
along with the text on screen. The recommended column is what keeps the heaviest neural voice
comfortably ahead of playback and makes writing an MP3 of a long script quick rather than
something to walk away from.

Speed, measured on one desktop machine (eight cores, sixteen threads), in seconds of speech
produced per second of work:

| Voice | 1 thread | 2 | 4 | 8 |
| --- | --- | --- | --- | --- |
| kokoro, the heaviest offered | 1.4x | 2.4x | 3.7x | 4.3x |
| piper ljspeech high | 1.7x | 3.1x | 5.1x | 6.8x |
| piper libritts_r medium | 13.6x | 22.7x | 32.4x | 35.5x |

Anything above 1.0x keeps ahead of playback, so on a slower machine the question is how long
writing a file takes rather than whether reading aloud keeps up. Four threads is most of what
eight gives, which is why the automatic count stops there.

**Synthesis threads** on the Settings tab is what to reach for on either end of that. Left
empty it works the count out from the processors this machine reports, and it can be set by
hand anywhere from 1 to 16. It belongs to the downloaded voices only. The Windows voices are
rendered by Windows itself, hundreds of times faster than playback on the same machine, and
nothing in this section applies to them.

No voice model has to be downloaded. The three Microsoft voices that come with Windows,
Microsoft David, Microsoft Zira and Microsoft Mark, work with the program as they are, and the
installer shows them as ticked, greyed out rows to say so. The neural models, roughly 60 to 300
MB each, are an optional step up in quality, taken from the Voices tab, from the installer or
with the bundled script.

### A note on the warning you will see

The installer is not code signed yet, so SmartScreen warns the first time you run it. Click
**More info**, then **Run anyway**.

SmartScreen is also known to block the installer outright, with no **Run anyway** offered at
all. If that happens, open **Windows Security**, go to **App & browser control**, then
**Reputation-based protection settings**, turn **Check apps and files** off, run the installer,
and turn it back on afterwards. Check the SHA256 against the release page first if you want to
be sure of what you have.

## 🔄 Updates

The program asks at each start whether a newer release exists, by reading `latest.json`. A
copy built before 0.12.0-beta asks this repository, where the answer is frozen at 0.12.0-beta,
and every copy from 0.12.0-beta onward asks the repository above. Nothing is sent: no identifier, no text, no telemetry. A newer version waits on
the **Updates** tab, which wears a red exclamation mark, names the version and lists what
changed in it. Nothing interrupts what you were doing unless you ask it to, with the box
**Update prompt popup box on startup**, and while that box is off no update dialog opens
anywhere in the program. **Install update**, beside **Check now**, takes the
version the last check found at any time: an installed copy downloads the installer and runs
it, after checking the download against its published hash, and a portable copy is given the
link and left to unpack it where it likes. The whole check can be switched off on the same tab,
where **Check now** also looks on demand.

## 📜 History

[CHANGELOG.md](CHANGELOG.md) covers every release, including the private alphas that came
before this repository existed.

## 🐛 Issues and suggestions

Bug reports and voice suggestions belong in
[Issues](https://github.com/jsphgei-dot/tts-util-win/issues), on the repository above. The
tracker here is closed along with the rest of it.

## 🤖 How it was made

This application was coded using AI assistance.

## ⚖️ License

Apache 2.0. Every download carries `LICENSE`, `NOTICE` and `THIRD-PARTY-NOTICES.txt`, which
name every component that ships inside the program and the work it is derived from: TTS Util
for Android by Dane Finlay, sherpa-onnx, ONNX Runtime, PdfPig, NAudio and the .NET runtime.

Voice models are not covered by that license. Each carries its own, stated in the `LICENSE` or
`MODEL_CARD` file inside the model folder and shown in the About tab.
