# TTS Util Win

[![Version](https://img.shields.io/badge/version-0.3.0--beta-blue.svg)](https://github.com/jsphgei-dot/tts-util-win-releases/releases/latest)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-green.svg)](#requirements)
[![Licence](https://img.shields.io/badge/licence-Apache%202.0-lightgrey.svg)](LICENSE)

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
installing. Voice models are downloaded separately, from the Voices tab or with the bundled
script, and are roughly 60 to 300 MB each.

### A note on the warning you will see

The installer is not code signed yet, so SmartScreen warns the first time you run it. Check the
SHA256 against the release page if you want to be sure of what you have.

## 🔄 Updates

The program asks this repository once a day whether a newer release exists, by reading
`latest.json`. Nothing is sent: no identifier, no text, no telemetry. An installed copy can
offer to download the installer and run it, after checking the download against its published
hash. A portable copy is given the link and left to unpack it where it likes. The whole thing
can be switched off in **Settings**.

## 📜 History

[CHANGELOG.md](CHANGELOG.md) covers every release, including the private alphas that came
before this repository existed.

## 🐛 Issues and suggestions

Bug reports and voice suggestions belong in
[Issues](https://github.com/jsphgei-dot/tts-util-win-releases/issues). A report that names the
voice, the setting and the text that misbehaved is worth a great deal.

## ⚖️ Licence

Apache 2.0. Every download carries `LICENSE`, `NOTICE` and `THIRD-PARTY-NOTICES.txt`, which
name every component that ships inside the program and the work it is derived from: TTS Util
for Android by Dane Finlay, sherpa-onnx, ONNX Runtime, PdfPig, NAudio and the .NET runtime.

Voice models are not covered by that licence. Each carries its own, stated in the `LICENSE` or
`MODEL_CARD` file inside the model folder and shown in the About tab.
