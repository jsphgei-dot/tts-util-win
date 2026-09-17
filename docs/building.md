[Documentation index](index.md) | [Usage](usage.md) | [Build](building.md) | [Release](releasing.md) | [Develop](developing.md)

---

# 🛠️ Build from source

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
properties all come from `Directory.Build.props`. Nothing needs editing in two places, and
nothing in a pull request needs to touch that file: see [releasing.md](releasing.md) for how a
build reaches other people.
