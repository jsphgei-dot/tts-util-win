# Voices

Voice models live here, one folder per model. They are not committed to git; see
`.gitignore`.

Download them with:

```powershell
..\scripts\FetchVoices.ps1 -List
..\scripts\FetchVoices.ps1 -Default
```

A folder is recognised as a voice when it holds `tokens.txt` and at least one `.onnx`
file. The kind is detected from what else is present:

* `voices.bin` present: kokoro.
* A second `.onnx` whose name contains `vocos`, `hifigan`, or `vocoder`: matcha.
* Otherwise: vits (this covers every piper voice).

Optional files that are picked up when present: `espeak-ng-data\`, `lexicon.txt`, `dict\`,
`LICENSE`, `MODEL_CARD`.
