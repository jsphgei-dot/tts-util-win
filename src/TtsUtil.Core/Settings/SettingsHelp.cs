/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Settings;

/// <summary>One explanation, shown when the reader presses the question mark beside a setting.</summary>
public sealed record SettingsHelpTopic(string Key, string Title, string Body);

/// <summary>The text behind the question marks on the Settings tab, kept out of the XAML.</summary>
public static class SettingsHelp
{
    public const string Silence = "silence";
    public const string ScaleSilence = "scale-silence";
    public const string Filters = "filters";
    public const string SpokenCharacters = "spoken-characters";
    public const string AllowedExtra = "allowed-extra";
    public const string VoicesDirectory = "voices-directory";
    public const string OutputDirectory = "output-directory";
    public const string Threads = "threads";
    public const string ChunkLength = "chunk-length";
    public const string OutputFormat = "output-format";
    public const string Mp3BitRate = "mp3-bit-rate";
    public const string CheckForUpdates = "check-for-updates";

    public const string UseWindowsVoices = "use-windows-voices";

    public const string SaveScriptWithAudio = "save-script-with-audio";

    public const string KeepLastMessages = "keep-last-messages";

    public const string ShowLastMessages = "show-last-messages";

    /// <summary>The blank line the other topics write inline as an escape.</summary>
    private const string NewParagraph = "\n\n";

    public static IReadOnlyList<SettingsHelpTopic> Topics { get; } = new[]
    {
        new SettingsHelpTopic(
            Silence,
            "Silence after breaks",
            "A pause in milliseconds, inserted where the text is split. 1000 is one second, and 0 " +
            "means no pause at all.\n\n" +
            "Line endings, full stops, question marks and exclamation marks are counted separately, " +
            "so a paragraph can rest longer than a sentence inside it.\n\n" +
            "The mark itself is never spoken. It becomes the pause.\n\n" +
            "Defaults: 200 after a line ending, 0 for the rest, since most voices already pause at a " +
            "full stop on their own."),

        new SettingsHelpTopic(
            ScaleSilence,
            "Scale silence to the speech rate",
            "Off: a 300 ms pause stays 300 ms whatever the speed slider says.\n\n" +
            "On: pauses shrink as the voice speeds up and stretch as it slows down, so the rhythm " +
            "stays in proportion. At 2.0x a 300 ms pause becomes 150 ms."),

        new SettingsHelpTopic(
            Filters,
            "Omitting hashes and links",
            "These run before the character rules below, and take out whole words rather than " +
            "spelling them.\n\n" +
            "Omit hash characters removes every #, so a heading written as ## Chapter two reads as " +
            "Chapter two.\n\n" +
            "Omit web links removes any word starting http:// or https://. Omit mailto links does the " +
            "same for mailto: addresses.\n\n" +
            "A bare address such as example.com is left alone, since it reads as ordinary words."),

        new SettingsHelpTopic(
            SpokenCharacters,
            "Characters read aloud",
            "What survives to be spoken. Punctuation always survives as a pause, whatever this is " +
            "set to.\n\n" +
            "Letters and digits only: keeps a to z, A to Z and 0 to 9. Accents fold to the plain " +
            "letter, so café is read as cafe. Everything else becomes a space, which removes non " +
            "Latin scripts completely.\n\n" +
            "Any letter or digit: keeps letters and digits of every script, so Chinese, Japanese, " +
            "Korean, Cyrillic and Greek are read. Symbols are still removed.\n\n" +
            "Everything: nothing is removed, and the voice may say dollar, percent and hash out " +
            "loud. This is how earlier versions behaved."),

        new SettingsHelpTopic(
            AllowedExtra,
            "Also read these characters",
            "Type the characters themselves, one after another, with nothing between them. There is " +
            "no separator, so no commas and no spaces are needed.\n\n" +
            "Example: typing  %$&+  lets the voice read 50% off, $20, Smith & Co and 2+2.\n\n" +
            "Every character is taken literally, so a comma typed here means the comma character " +
            "rather than a separator. Spaces typed here do nothing.\n\n" +
            "Letters count too. Under Letters and digits only, typing a letter with an accent keeps " +
            "that letter instead of folding it to the plain one.\n\n" +
            "Leave it empty to allow nothing beyond the setting above."),

        new SettingsHelpTopic(
            VoicesDirectory,
            "Voices directory",
            "Where voice models live, one folder for each voice. The Voices tab installs into this " +
            "folder.\n\n" +
            "Leave it empty for the usual place: a voices folder beside the program on a portable " +
            "copy, or one under your local application data when the program was installed.\n\n" +
            "Press Rescan at the top of the window after changing this."),

        new SettingsHelpTopic(
            OutputDirectory,
            "Default output directory",
            "The folder the Save dialog opens in.\n\n" +
            "Leave it empty for a TTS Util folder under Music, which is created the first time " +
            "something is saved. The status bar links to whichever folder was written to."),

        new SettingsHelpTopic(
            Threads,
            "Synthesis threads",
            "How many processor threads the voice model uses. 1 to 16, and 2 by default.\n\n" +
            "More threads mean less waiting before playback starts, with little gained past about " +
            "four. This does not make the voice speak faster. That is the speed slider."),

        new SettingsHelpTopic(
            ChunkLength,
            "Maximum characters per chunk",
            "Text is split at sentence ends and line breaks, and this caps how much is handed to the " +
            "voice at once. 64 to 20000, and 2000 by default.\n\n" +
            "Smaller starts speaking sooner and makes Stop feel quicker. Larger keeps long passages " +
            "flowing together. A passage with no break in it is split at a space instead."),

        new SettingsHelpTopic(
            OutputFormat,
            "Saved audio format",
            "Which file type the Save dialog offers first. MP3 is far smaller and plays anywhere. WAV " +
            "is uncompressed, which is the one to pick for editing.\n\n" +
            "The extension chosen in the dialog decides the actual format, so this is only the " +
            "starting point."),

        new SettingsHelpTopic(
            Mp3BitRate,
            "MP3 bit rate",
            "In kilobits per second. 32 to 320, and 128 by default. Higher means a larger file and " +
            "slightly cleaner sound.\n\n" +
            "One speaking voice needs little: 96 or 128 is plenty. Ignored when saving a WAV."),

        new SettingsHelpTopic(
            CheckForUpdates,
            "Look for a new version",
            "Once a day, at most, the program asks a single published file whether a newer release "
            + "exists. Nothing about you is sent: no identifier, no text, no list of voices, and no "
            + "request at all while this is off." + NewParagraph
            + "An installed copy can offer to download the installer and run it, after checking the "
            + "download against its published checksum. A portable copy is only given the link, since "
            + "replacing the folder it is running from is yours to do." + NewParagraph
            + "Check now looks straight away, whether or not the box is ticked and whether or not "
            + "you have already turned down the version it finds."),

        new(
            UseWindowsVoices,
            "Also offer the voices Windows already has",
            "Every Windows machine ships with at least one speech voice, and more can be added "
            + "through Settings, Time and language, Speech. Turning this on lists them beside the "
            + "downloaded ones, so the program can read something before anything is installed."
            + NewParagraph
            + "They are more robotic than the downloaded voices, which is what the downloads are "
            + "for. Both run on this machine and neither sends anything anywhere. A Windows voice "
            + "has one speaker, so the speaker picker does nothing for it."),

        new(
            KeepLastMessages,
            "Keep last messages",
            "How many past messages the history holds on to. Older ones are dropped as new ones "
            + "arrive, so a long session cannot fill memory with status text." + NewParagraph
            + "This is what Copy all writes out. Lowering it throws away anything past the new "
            + "number as soon as Apply is pressed."),

        new(
            ShowLastMessages,
            "Show last messages",
            "How many messages one page of the history shows. The rest are still kept and are a "
            + "page away." + NewParagraph
            + "Page 1 is always the newest, so the numbers count backwards in time and the double "
            + "arrow is the way back to now."),

        new SettingsHelpTopic(
            SaveScriptWithAudio,
            "Keep a script with the audio",
            "On: writing audio from the Text tab also saves that text as a script, under the name "
            + "you gave the audio file, so the words behind a recording can be found and read "
            + "again." + NewParagraph
            + "A script of that name already there is replaced." + NewParagraph
            + "Off: the audio file is written and nothing is saved beside it."),
    };

    /// <summary>The topic for a key, or null when nothing matches it.</summary>
    public static SettingsHelpTopic? Find(string? key) =>
        key is null ? null : Topics.FirstOrDefault(topic => topic.Key == key);
}
