# Changelog

Newest first. Version codes are monotonic and never reused.

## 0.8.0-beta (version code 12)

* **The lists that ship are real rules now.** Ticking chemistry, math symbols, units or everyday
  shorthand puts those rules straight into the grid, each list under a tab of its own, so they
  can be read, reordered and edited like any other rule. Unticking takes them out again.
* **Rules that can never fire are named.** A rule an earlier one already covers is called out
  under the grid rather than quietly doing nothing.
* **A new version waits on the Updates tab.** Updates have a tab of their own, marked with a red
  exclamation while a newer release is out, naming the version and listing what changed in it.
  The old dialog at startup is now a box you can turn on.
* **The change history is in the program.** The Updates tab lists it newest first, built into
  the build, so it needs no network.
* **Voices install as a batch.** Tick as many voices as you want and Install works through them
  one at a time. A press anywhere on a row moves its tick, on the Voices and Scripts lists
  alike, and holding Shift carries that tick across a run of rows.
* Empty boxes throughout the program show in grey what they are for until they are typed in.
* The Text tab strip stays on one scrolling row instead of wrapping.

## 0.7.0-beta (version code 11)

* **Lists that ship with the program.** The Aliases tab now offers chemistry, math symbols,
  units and everyday shorthand, each off until it is ticked. The chemistry list reads K as
  potassium and H2O as water. Any of them can be copied into your own rules and edited there.
* **A finished file says so.** Settings decides whether writing audio ends with no notification,
  a sound, or a Windows notification naming the file alongside the sound. Converting several
  scripts announces itself once at the end.
* **Ctrl+S asks for the name.** Saving a script from the Text tab prompts for the title first,
  and the save icon shows a tick, green for a new script and blue when one was replaced.
* **Voices are listed in the order they arrived.** The downloaded voices sit oldest first, so a
  voice installed a moment ago is at the bottom rather than somewhere alphabetical.
* Settings from elsewhere in the program are repeated on the Settings tab, with a reset button,
  and the reading can hold itself when another window takes over.
* A second press on a button no longer runs the same command twice.

## 0.6.0-beta (version code 10)

* **Several texts at once.** The Text tab holds as many documents as you like, each on its own
  tab with a plus at the end of the strip and a cross to close one. Each keeps its own words and
  its own script name.
* **Audio is written in the background.** Writing one tab to a file leaves the others alone, so
  a second text can be typed, or read aloud, while the first is still rendering. The tab shows
  how far along it is.
* **Each script remembers its voice.** Saving a script keeps the voice, the speaker and the
  speaking speed with it, and opening or converting that script sets them back.
* Everything typed comes back when the program is opened again, every tab of it.

## 0.5.0-beta (version code 9)

* **The voices Windows already has.** Every Microsoft voice installed in Windows appears in the
  voice list beside the downloaded ones, so the program speaks before anything is downloaded.
  A setting turns them off again.
* **Aliases.** A list of words the voice should say differently, applied as the text is read.
  Whole word and case matching per rule, a preview box, and lists that import and export as
  JSON so they can be shared.
* **The message history is paged.** Each message has its own box, the strip below reads
  `< page 1/4 > >>`, and the page number can be typed. Settings hold how many messages are kept
  and how many a page shows.
* **Batch conversion.** Every saved script has a tick box. Tick what you want, press Convert
  ticked to audio, choose a folder, and each is written as its own file.
* **Saving no longer loses your place.** Writing audio to a file stops the reading, writes the
  file, then sets the reading up again at the line you were hearing, paused.

## 0.4.1-beta (version code 8)

* **A Check now button** beside the update setting in Settings. It looks straight away whether
  or not the daily check is on, reports that you are up to date when you are, and offers a
  version you turned down before.
* The Apply button no longer sits on top of the update checkbox in Settings.

## 0.4.0-beta (version code 7)

* **A script title above the text**, with a save icon beside it and **Ctrl+S**. The Scripts tab
  shows the same title, so saving from either place uses one name.
* **An editing toolbar.** Undo and redo, cut, copy and paste, bulleted and numbered lists,
  indent and outdent, upper, lower, sentence and title case, and find and replace on Ctrl+H.
  A tool with text selected works on whole lines; with nothing selected it works on everything.
* **Clean up.** Join wrapped lines, collapse blank lines and tidy spacing, for text pasted out
  of a PDF or an email.
* **Editor size and wrapping**, remembered between sessions. Neither changes a saved file.
* **The media keys work.** Play, pause and stop on a keyboard drive the reading, and the
  Windows media overlay shows the script title with the same three buttons.

Scripts are plain text files, so the toolbar carries no bold, italic or color: everything in
it changes the words themselves and survives being saved.

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

* Speaker names for multi speaker voices, with search, favorites and per voice memory.
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

* Setup recognizes an existing installation: upgrade in place, repair, and a downgrade warning.
* A running copy is closed through the Restart Manager rather than failing on a locked file.

## 0.1.1-alpha (version code 2)

* Setup can download voices during installation, with a size and a license shown for each.

## 0.1.0-alpha (version code 1)

* First tagged build: read typed text, a file or the clipboard, save to a wave file, read as
  you type, adjustable speed, and a voice picker.
