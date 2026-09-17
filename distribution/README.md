# TTS Util Win

[![Version](https://img.shields.io/badge/version-0.11.0--beta-blue.svg)](https://github.com/jsphgei-dot/tts-util-win-releases/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-green.svg)](#requirements)
[![License](https://img.shields.io/badge/license-Apache%202.0-lightgrey.svg)](LICENSE)

> Reads text aloud on Windows, offline. Downloads for the program live here.

This repository carries the **downloads** for TTS Util Win, the version manifest the program
reads when it looks for updates, and the change history. The program speaks locally with
sherpa-onnx voice models: no account, no key, no text leaving the machine.

## ⬇️ Download

Take the newest build from [Releases](https://github.com/jsphgei-dot/tts-util-win-releases/releases/latest).

| Download | For |
| --- | --- |
| `TtsUtilWin-<version>-setup.zip` | Unpack and run the installer. Adds Start menu entries, upgrades in place, can fetch voices for you |
| `TtsUtilWin-<version>-portable.zip` | Unpack anywhere and run `TtsUtilWin.exe`. Settings and voices stay in the folder |

Every release lists the SHA256 of both files, and `latest.json` carries the same hashes. The
program refuses any update download whose hash does not match.

### Requirements

Windows 10 version 1809 or newer, x64. The executable is self contained, so no runtime needs
installing.

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

The program asks this repository at each start whether a newer release exists, by reading
`latest.json`. Nothing is sent: no identifier, no text, no telemetry. A newer version waits on
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
[Issues](https://github.com/jsphgei-dot/tts-util-win-releases/issues). A report that names the
voice, the setting and the text that misbehaved is worth a great deal.

## 🤖 How it was made

This application was coded using AI assistance.

## ⚖️ License

Apache 2.0. Every download carries `LICENSE`, `NOTICE` and `THIRD-PARTY-NOTICES.txt`, which
name every component that ships inside the program and the work it is derived from: TTS Util
for Android by Dane Finlay, sherpa-onnx, ONNX Runtime, PdfPig, NAudio and the .NET runtime.

Voice models are not covered by that license. Each carries its own, stated in the `LICENSE` or
`MODEL_CARD` file inside the model folder and shown in the About tab.
