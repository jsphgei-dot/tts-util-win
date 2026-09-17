# Voices

> Where the speech models live. One folder per model, none of them committed to git.

## Getting a model

Use the **Voices** tab in the program, or the script:

```powershell
..\scripts\FetchVoices.ps1 -List          # the whole catalog
..\scripts\FetchVoices.ps1 -Default       # the three defaults, about 490 MB
..\scripts\FetchVoices.ps1 -Name kokoro-en-v0_19
```

A model downloaded by hand works just as well: unpack it into a folder here and press
**Rescan**. The Voices section of [the usage guide](../docs/usage.md#voices) has the curated
list, the licenses and the other languages on offer.

## What makes a folder a voice

A folder is recognized when it holds `tokens.txt` and at least one `.onnx` file. The kind is
then detected from what else is present:

| Also present | Kind |
| --- | --- |
| `voices.bin` | kokoro |
| a second `.onnx` whose name contains `vocos`, `hifigan` or `vocoder` | matcha |
| neither | vits, which covers every piper voice |

Optional files picked up when present: `espeak-ng-data\`, `lexicon.txt`, `dict\`, `LICENSE`,
`MODEL_CARD`. A `speakers.txt` you write yourself names the speakers of a multi speaker model.

The `LICENSE` or `MODEL_CARD` inside a folder is the authoritative license for that model, and
the About tab shows it for the selected voice.
