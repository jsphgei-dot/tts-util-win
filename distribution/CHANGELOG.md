# Changelog

Newest first. Version codes are monotonic and never reused.

## 0.3.0-beta (version code 6)

First public release. Earlier versions were private alphas, so their downloads are not
published here.

* **A queue, and repeat.** Scripts can be lined up and played in order, with repeat off,
  repeat one, or repeat the whole queue.
* **Restarts sound the same.** The voice is wound back to the state it loaded in before every
  run, so a repeat is not a different performance.
* **Icons for playback.** Play, pause, stop, save and read from here, named on hover.
* **Read from here** moved beside the line list it works with.
* **A copyable status history.** The History panel is selectable, scrollable text.
* **Update checks.** Once a day, switchable off, with checksummed downloads.
* **Licensing.** `NOTICE` and `THIRD-PARTY-NOTICES.txt` ship with every build.
* **Fixed.** The bundled `FetchVoices.ps1` downloaded models one folder above where the program
  looks for them.

## 0.2.0-alpha (version code 5)

* Speaker names for multi speaker voices, with search, favourites and per voice memory.
* PDF import, reading the text layer and falling back to Windows OCR for scanned pages.
* A numbered line list, and reading from any line.
* A script library kept as plain text files.
* Pause and resume.
* MP3 output, saved under Music.
* A spoken character policy, and a status history.
* Fixed: the file progress bar understated progress on non ASCII files.

## 0.1.3-alpha (version code 4)

* Install voices from inside the program, with progress, Cancel and Remove.
* Downloads fall back to `%LOCALAPPDATA%` when the voices folder is read only.

## 0.1.2-alpha (version code 3)

* Setup recognises an existing installation: upgrade in place, repair, and a downgrade warning.
* A running copy is closed through the Restart Manager rather than failing on a locked file.

## 0.1.1-alpha (version code 2)

* Setup can download voices during installation, with a size and a licence shown for each.

## 0.1.0-alpha (version code 1)

* First tagged build: read typed text, a file or the clipboard, save to a wave file, read as
  you type, adjustable speed, and a voice picker.
