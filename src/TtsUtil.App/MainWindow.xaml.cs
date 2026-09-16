/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using TtsUtil.Core;
using TtsUtil.Core.Audio;
using TtsUtil.Core.Settings;
using TtsUtil.Core.Text;
using TtsUtil.Core.Tts;

namespace TtsUtil.App;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly bool _loadVoiceOnSelection;

    private IReadOnlyList<VoiceDescriptor> _voices = Array.Empty<VoiceDescriptor>();
    private ITtsEngine? _engine;
    private string? _loadedVoiceName;
    private CancellationTokenSource? _cancellation;
    private CancellationTokenSource? _typingCancellation;
    private IAudioPlayback? _player;
    private readonly SemaphoreSlim _typingTurnstile = new(1, 1);
    private int _queuedSnippets;
    private bool _initialising = true;
    private bool _busy;
    private readonly List<VoiceCatalogueRow> _catalogueRows = new();
    private CancellationTokenSource? _installCancellation;
    private bool _installing;
    private LineMap _lineMap = LineMap.Build(string.Empty);
    private DispatcherTimer? _lineRebuildTimer;
    private long _runStartOffset;
    private int _spokenLine = -1;
    private IReadOnlyList<SpeakerInfo> _speakers = Array.Empty<SpeakerInfo>();
    private string? _speakerVoiceName;
    private int _speakerId;
    private bool _favouritesOnly;

    public MainWindow() : this(LoadSettingsWithOverrides())
    {
    }

    private static AppSettings LoadSettingsWithOverrides()
    {
        var settings = AppSettings.Load();
        var overridden = App.VoicesDirectoryOverride;
        if (!string.IsNullOrWhiteSpace(overridden)) settings.VoicesDirectory = overridden;
        return settings;
    }

    internal MainWindow(AppSettings settings, bool loadVoiceOnSelection = true)
    {
        _settings = settings;
        _loadVoiceOnSelection = loadVoiceOnSelection;
        SpeakSnippet = snippet => _ = SpeakSnippetAsync(snippet);
        InitializeComponent();
        LoadSettingsIntoUi();
        _initialising = false;
        RefreshVoices();
        RefreshVoiceCatalogue();
        RebuildLineList();
        RefreshScripts();
    }

    /// <summary>Where saved scripts live. Tests point it at a temporary folder.</summary>
    internal ScriptLibrary Scripts { get; set; } = new(ScriptLibrary.ResolveDirectory(
        AppSettings.AppDirectory,
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        File.Exists));

    /// <summary>Opens a folder in Explorer. Tests replace it so nothing is launched.</summary>
    internal Action<string> FolderOpener { get; set; } = folder =>
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });

    /// <summary>Builds the PDF reader. Tests replace it so no OCR engine is needed.</summary>
    internal Func<PdfTextExtractor> PdfExtractorFactory { get; set; } =
        () => new PdfTextExtractor(WindowsPageOcr.IsAvailable ? new WindowsPageOcr() : null);

    /// <summary>Builds the installer used by the Voices tab. Tests replace it with a fake.</summary>
    internal Func<VoiceInstaller> VoiceInstallerFactory { get; set; } =
        () => new VoiceInstaller(new HttpVoiceDownloader(), new TarArchiveExtractor());

    /// <summary>Speaks one completed word during playback on input mode.</summary>
    internal Action<string> SpeakSnippet { get; set; }

    /// <summary>Reports an error as (message, title). Defaults to a modal dialog.</summary>
    internal Action<string, string> ErrorReporter { get; set; } = (message, title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

    /// <summary>Asks a yes or no question. Tests answer it without a dialog.</summary>
    internal Func<string, string, bool> Confirmer { get; set; } = (message, title) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private bool Confirm(string message, string title) => Confirmer(message, title);

    internal AppSettings Settings => _settings;

    internal IReadOnlyList<VoiceDescriptor> Voices => _voices;

    private VoiceDescriptor? SelectedVoice =>
        VoiceBox.SelectedIndex >= 0 && VoiceBox.SelectedIndex < _voices.Count
            ? _voices[VoiceBox.SelectedIndex]
            : null;

    private void LoadSettingsIntoUi()
    {
        SilenceLineEndingBox.Text = _settings.SilenceLineEndingMs.ToString();
        SilenceSentenceBox.Text = _settings.SilenceSentenceMs.ToString();
        SilenceQuestionBox.Text = _settings.SilenceQuestionMs.ToString();
        SilenceExclamationBox.Text = _settings.SilenceExclamationMs.ToString();
        ScaleSilenceBox.IsChecked = _settings.ScaleSilenceToRate;
        FilterHashBox.IsChecked = _settings.FilterHashes;
        FilterWebBox.IsChecked = _settings.FilterWebLinks;
        FilterMailBox.IsChecked = _settings.FilterMailToLinks;
        PopulateSpokenCharacterChoices();
        PopulateOutputFormatChoices();
        Mp3BitRateBox.Text = (_settings.Mp3BitRate / 1000).ToString();
        AllowedExtraBox.Text = _settings.AllowedExtraCharacters;
        VoicesDirBox.Text = _settings.ResolvedVoicesDirectory;
        OutputDirBox.Text = _settings.ResolvedOutputDirectory;
        ThreadsBox.Text = _settings.NumThreads.ToString();
        ChunkLengthBox.Text = _settings.MaxChunkLength.ToString();
        ReadAsYouTypeBox.IsChecked = _settings.ReadAsYouType;
        SpeedSlider.Value = Math.Clamp(_settings.Speed, 0.5, 2.0);
        SpeedText.Text = $"{_settings.Speed:0.00}x";
        SettingsPathText.Text = $"Settings file: {AppSettings.SettingsPath}";
        VersionText.Text = $"Version {AppVersion.Display}";
    }

    private void ApplySettingsFromUi()
    {
        _settings.SilenceLineEndingMs = ParseInt(SilenceLineEndingBox.Text, _settings.SilenceLineEndingMs);
        _settings.SilenceSentenceMs = ParseInt(SilenceSentenceBox.Text, _settings.SilenceSentenceMs);
        _settings.SilenceQuestionMs = ParseInt(SilenceQuestionBox.Text, _settings.SilenceQuestionMs);
        _settings.SilenceExclamationMs = ParseInt(SilenceExclamationBox.Text, _settings.SilenceExclamationMs);
        _settings.ScaleSilenceToRate = ScaleSilenceBox.IsChecked == true;
        _settings.FilterHashes = FilterHashBox.IsChecked == true;
        _settings.FilterWebLinks = FilterWebBox.IsChecked == true;
        _settings.FilterMailToLinks = FilterMailBox.IsChecked == true;
        _settings.SpokenCharacters = SelectedSpokenCharacterPolicy();
        _settings.OutputFormat = OutputFormatBox.SelectedIndex == 1 ? AudioOutputFormat.Wav : AudioOutputFormat.Mp3;
        _settings.Mp3BitRate = Math.Clamp(ParseInt(Mp3BitRateBox.Text, 128), 32, 320) * 1000;
        _settings.AllowedExtraCharacters = AllowedExtraBox.Text ?? string.Empty;
        _settings.NumThreads = Math.Clamp(ParseInt(ThreadsBox.Text, _settings.NumThreads), 1, 16);
        _settings.MaxChunkLength = Math.Clamp(ParseInt(ChunkLengthBox.Text, _settings.MaxChunkLength), 64, 20000);
        _settings.ReadAsYouType = ReadAsYouTypeBox.IsChecked == true;

        var outputDir = OutputDirBox.Text.Trim();
        _settings.OutputDirectory = outputDir.Length == 0 ? null : outputDir;

        var voicesDir = VoicesDirBox.Text.Trim();
        var voicesChanged = !string.Equals(voicesDir, _settings.ResolvedVoicesDirectory, StringComparison.OrdinalIgnoreCase);
        _settings.VoicesDirectory = voicesDir.Length == 0 ? null : voicesDir;

        _settings.Save();
        LoadSettingsIntoUi();
        if (voicesChanged) RefreshVoices();
    }

    private void RefreshVoices()
    {
        var directory = _settings.ResolvedVoicesDirectory;
        _voices = VoiceCatalog.Scan(directory);

        _initialising = true;
        VoiceBox.Items.Clear();
        foreach (var voice in _voices) VoiceBox.Items.Add($"{voice.Name}  ({voice.Kind})");
        _initialising = false;

        AboutVoicesText.Text = $"Voices directory: {directory}\n{_voices.Count} voice(s) found.";

        if (_voices.Count == 0)
        {
            SetStatus($"No voices found in {directory}. Install one from the Voices tab.");
            SpeakerBox.Items.Clear();
            LicenseText.Text = string.Empty;
            return;
        }

        var index = 0;
        if (!string.IsNullOrEmpty(_settings.LastVoiceName))
        {
            var found = _voices.ToList().FindIndex(v =>
                string.Equals(v.Name, _settings.LastVoiceName, StringComparison.OrdinalIgnoreCase));
            if (found >= 0) index = found;
        }

        VoiceBox.SelectedIndex = index;
    }

    private async Task<ITtsEngine?> EnsureEngineAsync()
    {
        var voice = SelectedVoice;
        if (voice is null)
        {
            SetStatus("Select a voice first.");
            return null;
        }

        if (_engine is not null && _loadedVoiceName == voice.Name) return _engine;

        DisposeEngine();
        SetStatus($"Loading {voice.Name}...");

        try
        {
            var threads = _settings.NumThreads;
            var engine = await Task.Run(() => SherpaTtsEngine.Load(voice, threads));
            _engine = engine;
            _loadedVoiceName = voice.Name;
            PopulateSpeakers(voice, engine.SpeakerCount);
            SetStatus($"{voice.Name} loaded: {engine.SampleRate} Hz, {engine.SpeakerCount} speaker(s).");
            return engine;
        }
        catch (Exception ex)
        {
            SetStatus($"Could not load {voice.Name}: {ex.Message}");
            ErrorReporter(ex.Message, "Voice failed to load");
            return null;
        }
    }

    /// <summary>The policies the Settings tab offers, in the order they are shown.</summary>
    private static readonly (SpokenCharacterPolicy Policy, string Label)[] SpokenChoices =
    {
        (SpokenCharacterPolicy.LatinOnly, "Letters and digits only (a-z, A-Z, 0-9)"),
        (SpokenCharacterPolicy.AnyLetter, "Any letter or digit, including accents and other scripts"),
        (SpokenCharacterPolicy.Off, "Everything, including punctuation and symbols"),
    };

    private void PopulateOutputFormatChoices()
    {
        if (OutputFormatBox.Items.Count == 0)
        {
            OutputFormatBox.Items.Add("MP3, smaller and plays anywhere");
            OutputFormatBox.Items.Add("WAV, uncompressed");
        }

        OutputFormatBox.SelectedIndex = _settings.OutputFormat == AudioOutputFormat.Wav ? 1 : 0;
    }

    private void PopulateSpokenCharacterChoices()
    {
        if (SpokenCharactersBox.Items.Count == 0)
        {
            foreach (var choice in SpokenChoices) SpokenCharactersBox.Items.Add(choice.Label);
        }

        var index = Array.FindIndex(SpokenChoices, choice => choice.Policy == _settings.SpokenCharacters);
        SpokenCharactersBox.SelectedIndex = index < 0 ? 0 : index;
    }

    private SpokenCharacterPolicy SelectedSpokenCharacterPolicy()
    {
        var index = SpokenCharactersBox.SelectedIndex;
        return index >= 0 && index < SpokenChoices.Length
            ? SpokenChoices[index].Policy
            : SpokenCharacterPolicy.LatinOnly;
    }

    /// <summary>Reads the voice's speaker names, then fills the picker and restores the choice.</summary>
    internal void PopulateSpeakers(VoiceDescriptor? voice, int count)
    {
        _speakerVoiceName = voice?.Name;
        _speakers = voice is null
            ? SpeakerCatalog.Build(count)
            : SpeakerCatalog.Load(voice, count);

        _speakerId = Math.Clamp(_settings.GetSpeakerId(_speakerVoiceName), 0, Math.Max(0, count - 1));

        _favouritesOnly = false;
        ShowFavouritesButton.Content = "Favourites";
        SpeakerSearchBox.Text = string.Empty;
        RefreshSpeakerList();

        var visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;
        SpeakerPanel.Visibility = visibility;
        SpeakerBox.Visibility = visibility;
        SpeakerLabel.Visibility = visibility;
    }

    /// <summary>Applies the search box and the starred speakers to what the picker shows.</summary>
    private void RefreshSpeakerList()
    {
        var favourites = _settings.GetFavouriteSpeakers(_speakerVoiceName);
        var pool = _favouritesOnly
            ? _speakers.Where(speaker => favourites.Contains(speaker.Id)).ToList()
            : _speakers;
        var shown = SpeakerCatalog.Filter(pool, SpeakerSearchBox.Text, favourites);

        _initialising = true;
        SpeakerBox.Items.Clear();
        foreach (var speaker in shown) SpeakerBox.Items.Add(speaker);

        // The selected speaker can be filtered out, and then nothing is selected to change.
        var index = shown.ToList().FindIndex(speaker => speaker.Id == _speakerId);
        SpeakerBox.SelectedIndex = index;
        _initialising = false;

        FavouriteSpeakerButton.IsChecked = _settings.IsFavouriteSpeaker(_speakerVoiceName, _speakerId);

        var count = shown.Count == _speakers.Count
            ? $"{_speakers.Count} speakers"
            : $"{shown.Count} of {_speakers.Count} speakers";

        // The filter can hide the speaker that is still the one a run would use.
        SpeakerCountText.Text = index >= 0 ? count : $"{count}, still using {ActiveSpeakerLabel()}";
    }

    /// <summary>The speaker the next run uses, whatever the picker is filtered down to.</summary>
    internal int SelectedSpeakerId => _speakerId;

    private string ActiveSpeakerLabel() =>
        _speakers.FirstOrDefault(speaker => speaker.Id == _speakerId)?.Label ?? _speakerId.ToString();

    private async Task RunSynthesisAsync(Func<TextReader> readerFactory, string? outputPath,
        long? knownCharacters = null, long startOffset = 0)
    {
        if (_busy)
        {
            SetStatus("Already working. Press Stop first.");
            return;
        }

        var engine = await EnsureEngineAsync();
        if (engine is null) return;

        CancelTypingPlayback();

        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        var token = cancellation.Token;

        var options = _settings.ToChunkerOptions();
        var speakerId = _speakerId;
        var speed = (float)SpeedSlider.Value;
        _runStartOffset = startOffset;
        _spokenLine = -1;
        ShowPauseState(paused: false);
        IProgress<string> encodeProgress = new Progress<string>(SetStatus);

        var progress = new Progress<SynthesisProgress>(p =>
        {
            Progress.Value = p.Percent;
            if (p.TotalCharacters > 0) SetStatus($"{p.Percent}% ({p.CharactersRead} of {p.TotalCharacters} characters)");
            HighlightSpokenLine(p.CharactersRead);
        });

        SetBusy(true);

        try
        {
            var result = await Task.Run(() =>
            {
                // Counting beats a byte length, which overstates the total on non ASCII text.
                var totalCharacters = knownCharacters ?? TextMeasure.CountCharacters(readerFactory);
                using var reader = readerFactory();
                var runner = new SynthesisRunner(engine, options) { SpeakerId = speakerId, Speed = speed };

                if (outputPath is null)
                {
                    using var player = PlayerFactory(engine.SampleRate, token);
                    _player = player;
                    try
                    {
                        runner.Run(reader, totalCharacters, player, progress, token);
                        player.WaitUntilDrained();
                    }
                    finally
                    {
                        _player = null;
                    }
                }
                else
                {
                    // Disposing encodes when the sink is an MP3 one, on this worker thread.
                    var sink = CreateFileSink(outputPath, engine.SampleRate, encodeProgress.Report);
                    try
                    {
                        runner.Run(reader, totalCharacters, sink, progress, token);
                    }
                    finally
                    {
                        (sink as IDisposable)?.Dispose();
                    }
                }

                return (runner.CharactersFiltered, runner.UtterancesSpoken, totalCharacters);
            }, token);

            var (filtered, spoken, total) = result;

            // Everything filtered away is silent failure, and the usual cause is the policy.
            if (spoken == 0 && total > 0)
            {
                SetStatus("Nothing was left to read: every character was filtered. " +
                          "Check \"Characters read aloud\" in Settings.");
                return;
            }

            var note = filtered > 0 ? $" {filtered} character(s) filtered." : string.Empty;

            if (outputPath is null)
            {
                SetStatus($"Finished reading.{note}");
            }
            else
            {
                SetStatusWithFileLink($"Wrote {Path.GetFileName(outputPath)}.{note} ", outputPath);
            }
        }
        catch (OperationCanceledException)
        {
            SetStatus("Stopped.");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            ErrorReporter(ex.Message, "Synthesis failed");
        }
        finally
        {
            SetBusy(false);
            Progress.Value = 0;
            _cancellation = null;
            cancellation.Dispose();
        }
    }

    private void OnReadText(object sender, RoutedEventArgs e) => ReadFrom(0);

    private void OnReadFromHere(object sender, RoutedEventArgs e) => ReadFrom(ChosenStartOffset());

    private void OnLineListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LineList.SelectedItem is not ScriptLineRow row) return;
        ReadFrom(row.Start);
    }

    /// <summary>The line the user picked in the list, or failing that the caret's line.</summary>
    private int ChosenStartOffset()
    {
        if (LineList.SelectedItem is ScriptLineRow row) return row.Start;

        RebuildLineList();
        return _lineMap.StartOf(_lineMap.LineAt(InputText.CaretIndex));
    }

    private void ReadFrom(int startOffset)
    {
        var text = InputText.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetStatus("There is no text to read.");
            return;
        }

        var offset = Math.Clamp(startOffset, 0, text.Length);
        var remainder = text[offset..];

        if (string.IsNullOrWhiteSpace(remainder))
        {
            SetStatus("There is nothing left to read from there.");
            return;
        }

        LastReadStartOffset = offset;
        _ = RunSynthesisAsync(() => new StringReader(remainder), null, remainder.Length, offset);
    }

    /// <summary>Where the last Read started, which is what "read from here" actually decides.</summary>
    internal int LastReadStartOffset { get; private set; } = -1;

    // --- The line list ---

    /// <summary>Rebuilds the list from the editor, keeping the selected line where it can.</summary>
    internal void RebuildLineList()
    {
        _lineMap = LineMap.Build(InputText.Text);

        if (LinePanel.Visibility != Visibility.Visible)
        {
            LineList.Items.Clear();
            return;
        }

        var selected = (LineList.SelectedItem as ScriptLineRow)?.Index ?? -1;

        LineList.Items.Clear();
        foreach (var line in _lineMap.Lines) LineList.Items.Add(new ScriptLineRow(line));

        if (selected >= 0 && selected < LineList.Items.Count) LineList.SelectedIndex = selected;
    }

    /// <summary>Waits for typing to settle, so a long script is not rebuilt on every key.</summary>
    private void ScheduleLineRebuild()
    {
        _lineRebuildTimer ??= CreateLineRebuildTimer();
        _lineRebuildTimer.Stop();
        _lineRebuildTimer.Start();
    }

    private DispatcherTimer CreateLineRebuildTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            RebuildLineList();
        };
        return timer;
    }

    private void OnShowLinesChanged(object sender, RoutedEventArgs e)
    {
        // IsChecked in XAML fires this while the tree is still being built.
        if (LinePanel is null || LineSplitter is null || LineListColumn is null) return;

        var show = ShowLinesBox.IsChecked == true;
        LinePanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        LineSplitter.Visibility = LinePanel.Visibility;
        LineListColumn.Width = show ? new GridLength(300) : new GridLength(0);
        RebuildLineList();
    }

    /// <summary>Follows the run down the list, so the spoken line is visible.</summary>
    private void HighlightSpokenLine(long charactersRead)
    {
        if (LinePanel.Visibility != Visibility.Visible || LineList.Items.Count == 0) return;

        var line = _lineMap.LineAt(_runStartOffset + charactersRead);
        if (line == _spokenLine || line >= LineList.Items.Count) return;

        _spokenLine = line;
        LineList.SelectedIndex = line;
        LineList.ScrollIntoView(LineList.Items[line]);
    }

    private void OnSaveTextToWave(object sender, RoutedEventArgs e)
    {
        var text = InputText.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetStatus("There is no text to write.");
            return;
        }

        var path = AskForAudioPath("tts_output");
        if (path is null) return;

        _ = RunSynthesisAsync(() => new StringReader(text), path, text.Length);
    }

    private void OnReadFile(object sender, RoutedEventArgs e) => _ = ReadOrConvertFileAsync(null);

    private void OnSaveFileToWave(object sender, RoutedEventArgs e)
    {
        var path = FilePathBox.Text.Trim();
        if (!File.Exists(path))
        {
            SetStatus("Choose an existing file first.");
            return;
        }

        var outputPath = AskForAudioPath(Path.GetFileNameWithoutExtension(path));
        if (outputPath is null) return;

        _ = ReadOrConvertFileAsync(outputPath);
    }

    private async Task ReadOrConvertFileAsync(string? outputPath)
    {
        var path = FilePathBox.Text.Trim();
        if (!File.Exists(path))
        {
            SetStatus("Choose an existing file first.");
            return;
        }

        if (!IsPdf(path))
        {
            await RunSynthesisAsync(() => OpenTextFile(path), outputPath);
            return;
        }

        var text = await ExtractPdfAsync(path);
        if (text is null) return;

        await RunSynthesisAsync(() => new StringReader(text), outputPath, text.Length);
    }

    private void OnImportFileToTextTab(object sender, RoutedEventArgs e) =>
        PendingWork = ImportFileToTextTabAsync();

    /// <summary>The last background task a click started, so tests can await it.</summary>
    internal Task PendingWork { get; private set; } = Task.CompletedTask;

    private async Task ImportFileToTextTabAsync()
    {
        var path = FilePathBox.Text.Trim();
        if (!File.Exists(path))
        {
            SetStatus("Choose an existing file first.");
            return;
        }

        string? text;
        if (IsPdf(path))
        {
            text = await ExtractPdfAsync(path);
            if (text is null) return;
        }
        else
        {
            using var reader = OpenTextFile(path);
            text = await reader.ReadToEndAsync();
        }

        InputText.Text = text;
        RebuildLineList();
        Tabs.SelectedIndex = 0;
        SetStatus($"Imported {text.Length:N0} character(s) from {Path.GetFileName(path)}.");
    }

    /// <summary>Extracts a PDF on a worker thread, reporting pages. Null means it failed.</summary>
    private async Task<string?> ExtractPdfAsync(string path)
    {
        if (_busy)
        {
            SetStatus("Already working. Press Stop first.");
            return null;
        }

        var cancellation = new CancellationTokenSource();
        _cancellation = cancellation;
        var progress = new Progress<string>(SetStatus);

        SetBusy(true);

        try
        {
            var extractor = PdfExtractorFactory();
            var result = await Task.Run(
                () => extractor.ExtractAsync(path, progress, cancellation.Token), cancellation.Token);

            var text = result.Text;
            if (text.Trim().Length == 0)
            {
                SetStatus(result.ScannedPages.Count > 0
                    ? "This PDF holds no text layer and nothing could be read from its images. It may be a scan of a kind the Windows reader cannot handle."
                    : "This PDF holds no text.");
                return null;
            }

            FileInfoText.Text = DescribeExtraction(path, result);
            return text;
        }
        catch (OperationCanceledException)
        {
            SetStatus("Stopped.");
            return null;
        }
        catch (Exception ex)
        {
            SetStatus($"Could not read {Path.GetFileName(path)}: {ex.Message}");
            ErrorReporter(ex.Message, "PDF could not be read");
            return null;
        }
        finally
        {
            SetBusy(false);
            _cancellation = null;
        }
    }

    private static string DescribeExtraction(string path, PdfExtractionResult result)
    {
        var name = Path.GetFileName(path);
        var note = $"{name}: {result.Pages.Count} page(s), {result.Text.Length:N0} characters.";

        if (result.ScannedPages.Count > 0)
        {
            note += $" {result.ScannedPages.Count} page(s) had no text layer and were read as images.";
        }

        if (result.EmptyPages.Count > 0)
        {
            note += $" Nothing was read from page(s): {string.Join(", ", result.EmptyPages)}.";
        }

        return note;
    }

    private static bool IsPdf(string path) =>
        PdfTextExtractor.HasPdfExtension(path) || PdfTextExtractor.LooksLikePdf(path);

    // --- Saved scripts ---

    /// <summary>Reloads the script list, keeping the selected title where it still exists.</summary>
    internal void RefreshScripts()
    {
        var selected = (ScriptList.SelectedItem as SavedScript)?.Title;

        ScriptList.Items.Clear();
        foreach (var script in Scripts.List()) ScriptList.Items.Add(script);

        ScriptFolderText.Text = $"Scripts folder: {Scripts.Directory}";

        if (selected is null) return;

        var found = ScriptList.Items.Cast<SavedScript>()
            .FirstOrDefault(s => string.Equals(s.Title, selected, StringComparison.OrdinalIgnoreCase));
        if (found is not null) ScriptList.SelectedItem = found;
    }

    private void OnScriptSelected(object sender, SelectionChangedEventArgs e)
    {
        if (ScriptList.SelectedItem is not SavedScript script) return;

        ScriptTitleBox.Text = script.Title;

        try
        {
            ScriptPreview.Text = Scripts.Load(script.Title);
        }
        catch (IOException ex)
        {
            ScriptPreview.Text = string.Empty;
            SetStatus($"Could not read {script.Title}: {ex.Message}");
        }
    }

    private void OnSaveScript(object sender, RoutedEventArgs e)
    {
        var text = InputText.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetStatus("The Text tab is empty, so there is nothing to save.");
            return;
        }

        var title = ScriptLibrary.ToFileName(ScriptTitleBox.Text);
        if (title is null)
        {
            SetStatus("Give the script a title first.");
            return;
        }

        try
        {
            var existed = Scripts.Exists(title);
            var saved = Scripts.Save(title, text);
            RefreshScripts();
            ScriptList.SelectedItem = ScriptList.Items.Cast<SavedScript>()
                .FirstOrDefault(s => s.Title == saved.Title);

            SetStatus(existed ? $"Replaced {saved.Title}." : $"Saved {saved.Title}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Could not save {title}: {ex.Message}");
            ErrorReporter(ex.Message, "Script could not be saved");
        }
    }

    private void OnOpenScript(object sender, RoutedEventArgs e)
    {
        if (ScriptList.SelectedItem is not SavedScript script)
        {
            SetStatus("Select a script first.");
            return;
        }

        try
        {
            InputText.Text = Scripts.Load(script.Title);
            RebuildLineList();
            Tabs.SelectedIndex = 0;
            SetStatus($"Opened {script.Title}.");
        }
        catch (IOException ex)
        {
            SetStatus($"Could not open {script.Title}: {ex.Message}");
        }
    }

    private void OnRenameScript(object sender, RoutedEventArgs e)
    {
        if (ScriptList.SelectedItem is not SavedScript script)
        {
            SetStatus("Select a script first.");
            return;
        }

        var title = ScriptLibrary.ToFileName(ScriptTitleBox.Text);
        if (title is null)
        {
            SetStatus("Type the new title in the Title box first.");
            return;
        }

        if (!Scripts.Rename(script.Title, title))
        {
            SetStatus($"There is already a script called {title}.");
            return;
        }

        RefreshScripts();
        SetStatus($"Renamed to {title}.");
    }

    private void OnDeleteScript(object sender, RoutedEventArgs e)
    {
        if (ScriptList.SelectedItem is not SavedScript script)
        {
            SetStatus("Select a script first.");
            return;
        }

        if (!Confirm($"Delete the script \"{script.Title}\"? The file is removed from disk.", "Delete script"))
        {
            return;
        }

        try
        {
            Scripts.Delete(script.Title);
            ScriptPreview.Clear();
            RefreshScripts();
            SetStatus($"Deleted {script.Title}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus($"Could not delete {script.Title}: {ex.Message}");
        }
    }

    private void OnOpenScriptFolder(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(Scripts.Directory);
            FolderOpener(Scripts.Directory);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open the scripts folder: {ex.Message}");
        }
    }

    private void OnStop(object sender, RoutedEventArgs e)
    {
        CancelTypingPlayback();
        _cancellation?.Cancel();

        // Resuming first, so a run paused when Stop is pressed does not sit waiting to drain.
        _player?.Resume();
        _player?.Stop();
        ShowPauseState(paused: false);
        SetStatus("Stopping...");
    }

    private void OnTogglePause(object sender, RoutedEventArgs e)
    {
        var player = _player;
        if (player is null)
        {
            SetStatus("Nothing is playing.");
            return;
        }

        if (player.IsPaused)
        {
            player.Resume();
            ShowPauseState(paused: false);
            SetStatus("Playing.");
            return;
        }

        player.Pause();
        ShowPauseState(paused: true);
        SetStatus("Paused. Press Resume to carry on, or Stop to give up.");
    }

    /// <summary>Both tabs carry the button, so both follow the one playback state.</summary>
    private void ShowPauseState(bool paused)
    {
        PauseButton.Content = paused ? "Resume" : "Pause";
        PauseFileButton.Content = PauseButton.Content;
    }

    /// <summary>The playback a run is using, or null when nothing is playing.</summary>
    internal IAudioPlayback? ActivePlayback
    {
        get => _player;
        set => _player = value;
    }

    /// <summary>The player a run plays through. Tests replace it so no device is needed.</summary>
    internal Func<int, CancellationToken, IAudioPlayback> PlayerFactory { get; set; } =
        (sampleRate, token) => new AudioPlayerSink(sampleRate, token);

    private void OnPasteClipboard(object sender, RoutedEventArgs e)
    {
        if (!Clipboard.ContainsText())
        {
            SetStatus("The clipboard holds no text.");
            return;
        }

        InputText.Text = Clipboard.GetText();
        InputText.CaretIndex = InputText.Text.Length;
    }

    private void OnClearText(object sender, RoutedEventArgs e) => InputText.Clear();

    private void OnBrowseInputFile(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Choose a text file or a PDF",
            Filter = "Readable files (*.txt;*.md;*.csv;*.log;*.pdf)|*.txt;*.md;*.csv;*.log;*.pdf|" +
                     "PDF documents (*.pdf)|*.pdf|Text files (*.txt;*.md;*.csv;*.log)|*.txt;*.md;*.csv;*.log|" +
                     "All files (*.*)|*.*",
        };

        if (dialog.ShowDialog(this) != true) return;

        FilePathBox.Text = dialog.FileName;
        var info = new FileInfo(dialog.FileName);
        FileInfoText.Text = $"{info.Name}: {info.Length:N0} bytes.";
    }

    private void OnBrowseVoicesDir(object sender, RoutedEventArgs e)
    {
        var chosen = AskForDirectory(VoicesDirBox.Text);
        if (chosen is not null) VoicesDirBox.Text = chosen;
    }

    private void OnBrowseOutputDir(object sender, RoutedEventArgs e)
    {
        var chosen = AskForDirectory(OutputDirBox.Text);
        if (chosen is not null) OutputDirBox.Text = chosen;
    }

    /// <summary>Shows the explanation for whichever question mark was pressed.</summary>
    private void OnSettingsHelp(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement source) return;

        var topic = SettingsHelp.Find(source.Tag as string);
        if (topic is null) return;

        SettingsHelpTitle.Text = topic.Title;
        SettingsHelpBody.Text = topic.Body;
        SettingsHelpPopup.PlacementTarget = source;
        SettingsHelpPopup.IsOpen = true;
    }

    private void OnApplySettings(object sender, RoutedEventArgs e)
    {
        ApplySettingsFromUi();
        SetStatus("Settings saved.");
    }

    private void OnRescanVoices(object sender, RoutedEventArgs e)
    {
        DisposeEngine();
        RefreshVoices();
    }

    private void RefreshVoiceCatalogue()
    {
        if (_catalogueRows.Count == 0)
        {
            foreach (var voice in DownloadableVoices.All) _catalogueRows.Add(new VoiceCatalogueRow(voice));
            VoiceCatalogueList.ItemsSource = _catalogueRows;
        }

        var directory = VoiceInstallDirectory;
        foreach (var row in _catalogueRows)
        {
            row.Status = VoiceInstaller.IsInstalled(row.Voice, directory) ? "Installed" : "Not installed";
        }

        VoiceTargetText.Text = $"Installing into {directory}";
    }

    /// <summary>Where a downloaded voice goes: the configured folder, or the profile when it is read only.</summary>
    internal string VoiceInstallDirectory => VoiceInstaller.ChooseTargetDirectory(
        _settings.ResolvedVoicesDirectory,
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            InstallPaths.ApplicationFolderName,
            InstallPaths.VoicesFolderName),
        VoiceInstaller.CanWrite);

    private VoiceCatalogueRow? SelectedCatalogueRow => VoiceCatalogueList.SelectedItem as VoiceCatalogueRow;

    private async void OnInstallVoice(object sender, RoutedEventArgs e)
    {
        var row = SelectedCatalogueRow;

        if (row is null)
        {
            VoiceInstallStatus.Text = "Select a voice to install.";
            return;
        }

        if (_installing)
        {
            VoiceInstallStatus.Text = "Already installing. Press Cancel first.";
            return;
        }

        var directory = VoiceInstallDirectory;

        if (VoiceInstaller.IsInstalled(row.Voice, directory))
        {
            VoiceInstallStatus.Text = $"{row.Id} is already installed. Remove it first to download it again.";
            return;
        }

        _installing = true;
        _installCancellation = new CancellationTokenSource();
        InstallVoiceButton.IsEnabled = false;
        RemoveVoiceButton.IsEnabled = false;
        CancelVoiceButton.IsEnabled = true;
        VoiceProgress.Value = 0;
        row.Status = "Installing";

        // Reports are posted, so late ones would overwrite the message the awaiting code leaves.
        var progress = new Progress<VoiceInstallProgress>(report =>
        {
            if (!_installing) return;

            switch (report.Phase)
            {
                case VoiceInstallPhase.Downloading:
                    VoiceProgress.Value = report.Percent;
                    VoiceInstallStatus.Text = $"Downloading {row.Id}: {report.Percent}% of {row.Voice.SizeMb} MB";
                    break;

                case VoiceInstallPhase.Extracting:
                    VoiceProgress.Value = 100;
                    VoiceInstallStatus.Text = $"Unpacking {row.Id}...";
                    break;
            }
        });

        try
        {
            await VoiceInstallerFactory().InstallAsync(row.Voice, directory, progress, _installCancellation.Token);

            VoiceProgress.Value = 100;
            VoiceInstallStatus.Text = $"{row.Id} installed. Licence: {row.Licence}";
            DisposeEngine();
            RefreshVoices();
        }
        catch (OperationCanceledException)
        {
            VoiceProgress.Value = 0;
            VoiceInstallStatus.Text = $"{row.Id} cancelled; nothing was kept.";
        }
        catch (Exception ex)
        {
            VoiceProgress.Value = 0;
            VoiceInstallStatus.Text = $"{row.Id} failed: {ex.Message}";
        }
        finally
        {
            _installCancellation?.Dispose();
            _installCancellation = null;
            _installing = false;
            InstallVoiceButton.IsEnabled = true;
            RemoveVoiceButton.IsEnabled = true;
            CancelVoiceButton.IsEnabled = false;
            RefreshVoiceCatalogue();
        }
    }

    private void OnRemoveVoice(object sender, RoutedEventArgs e)
    {
        var row = SelectedCatalogueRow;

        if (row is null)
        {
            VoiceInstallStatus.Text = "Select a voice to remove.";
            return;
        }

        var directory = VoiceInstallDirectory;

        if (!VoiceInstaller.IsInstalled(row.Voice, directory))
        {
            VoiceInstallStatus.Text = $"{row.Id} is not installed.";
            return;
        }

        if (string.Equals(_loadedVoiceName, row.Id, StringComparison.OrdinalIgnoreCase)) DisposeEngine();

        VoiceInstaller.Remove(row.Voice, directory);
        VoiceInstallStatus.Text = $"{row.Id} removed.";
        RefreshVoices();
        RefreshVoiceCatalogue();
    }

    private void OnCancelVoiceInstall(object sender, RoutedEventArgs e)
    {
        if (!_installing)
        {
            VoiceInstallStatus.Text = "Nothing is installing.";
            return;
        }

        _installCancellation?.Cancel();
        VoiceInstallStatus.Text = "Cancelling...";
    }

    private async void OnVoiceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialising) return;

        var voice = SelectedVoice;
        if (voice is null) return;

        _settings.LastVoiceName = voice.Name;
        _settings.Save();
        LicenseText.Text = ReadLicenseSummary(voice);

        DisposeEngine();
        if (_loadVoiceOnSelection) await EnsureEngineAsync();
    }

    private void OnSpeakerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initialising || SpeakerBox.SelectedItem is not SpeakerInfo speaker) return;

        _speakerId = speaker.Id;
        _settings.SetSpeakerId(_speakerVoiceName, speaker.Id);
        FavouriteSpeakerButton.IsChecked = _settings.IsFavouriteSpeaker(_speakerVoiceName, speaker.Id);
    }

    private void OnSpeakerSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (_speakers.Count == 0) return;
        RefreshSpeakerList();
    }

    private void OnToggleFavouriteSpeaker(object sender, RoutedEventArgs e)
    {
        if (_speakers.Count == 0) return;

        _settings.ToggleFavouriteSpeaker(_speakerVoiceName, _speakerId);
        _settings.Save();

        if (_favouritesOnly && _settings.GetFavouriteSpeakers(_speakerVoiceName).Count == 0)
        {
            _favouritesOnly = false;
            ShowFavouritesButton.Content = "Favourites";
        }

        RefreshSpeakerList();
    }

    private void OnShowFavouriteSpeakers(object sender, RoutedEventArgs e)
    {
        var favourites = _settings.GetFavouriteSpeakers(_speakerVoiceName);
        if (!_favouritesOnly && favourites.Count == 0)
        {
            SetStatus("No starred speakers for this voice yet. Pick one and press Star.");
            return;
        }

        _favouritesOnly = !_favouritesOnly;
        ShowFavouritesButton.Content = _favouritesOnly ? "Show all" : "Favourites";
        RefreshSpeakerList();
    }

    private void OnSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpeedText is null) return;
        SpeedText.Text = $"{e.NewValue:0.00}x";
        _settings.Speed = (float)e.NewValue;
    }

    private void OnReadAsYouTypeChanged(object sender, RoutedEventArgs e)
    {
        if (_initialising) return;
        _settings.ReadAsYouType = ReadAsYouTypeBox.IsChecked == true;
        if (!_settings.ReadAsYouType) CancelTypingPlayback();
    }

    private void OnInputTextChanged(object sender, TextChangedEventArgs e)
    {
        ScheduleLineRebuild();

        if (_initialising || _busy || ReadAsYouTypeBox.IsChecked != true) return;

        var change = e.Changes.FirstOrDefault();
        if (change is null) return;

        var snippet = TypingReader.TextToRead(InputText.Text, change.Offset, change.AddedLength, change.RemovedLength);
        if (string.IsNullOrWhiteSpace(snippet)) return;

        SpeakSnippet(snippet!);
    }

    /// <summary>Queues one word and speaks it after any words already waiting.</summary>
    private async Task SpeakSnippetAsync(string snippet)
    {
        var engine = _engine;
        if (engine is null) return;

        // Drop words once the backlog is long enough to be useless.
        if (Interlocked.Increment(ref _queuedSnippets) > MaxQueuedSnippets)
        {
            Interlocked.Decrement(ref _queuedSnippets);
            return;
        }

        var token = (_typingCancellation ??= new CancellationTokenSource()).Token;
        var speakerId = _speakerId;
        var speed = (float)SpeedSlider.Value;

        try
        {
            await _typingTurnstile.WaitAsync(token);

            try
            {
                await Task.Run(() =>
                {
                    using var player = new AudioPlayerSink(engine.SampleRate, token);
                    engine.Synthesize(snippet, speakerId, speed, samples =>
                    {
                        if (token.IsCancellationRequested) return false;
                        player.WriteSamples(samples.Span);
                        return true;
                    }, token);
                    player.WaitUntilDrained();
                }, token);
            }
            finally
            {
                _typingTurnstile.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Stop, or a full read, cleared the queue.
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            Interlocked.Decrement(ref _queuedSnippets);
        }
    }

    private void CancelTypingPlayback()
    {
        var cancellation = _typingCancellation;
        _typingCancellation = null;
        cancellation?.Cancel();
    }

    private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        CancelTypingPlayback();
        _cancellation?.Cancel();
        _player?.Stop();
        _settings.Save();
        DisposeEngine();
    }

    private void DisposeEngine()
    {
        _engine?.Dispose();
        _engine = null;
        _loadedVoiceName = null;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        Cursor = busy ? System.Windows.Input.Cursors.AppStarting : null;
    }

    /// <summary>How many past messages the history keeps before the oldest falls off.</summary>
    internal const int StatusHistoryLimit = 50;

    private readonly List<string> _statusHistory = new();

    internal IReadOnlyList<string> StatusHistory => _statusHistory;

    private void SetStatus(string message)
    {
        StatusText.Inlines.Clear();
        StatusText.Text = message;
        RecordStatus(message);
    }

    /// <summary>Keeps the last messages, since the bar shows one and loses the rest.</summary>
    private void RecordStatus(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        // Progress overwrites itself many times a second, and none of it is worth keeping.
        if (_statusHistory.Count > 0 && _statusHistory[^1] == message) return;

        _statusHistory.Add(message);
        if (_statusHistory.Count > StatusHistoryLimit) _statusHistory.RemoveAt(0);
    }

    private void OnShowStatusHistory(object sender, RoutedEventArgs e)
    {
        StatusHistoryList.Items.Clear();

        for (var i = _statusHistory.Count - 1; i >= 0; i--) StatusHistoryList.Items.Add(_statusHistory[i]);

        if (StatusHistoryList.Items.Count == 0) StatusHistoryList.Items.Add("Nothing yet.");

        StatusHistoryPopup.IsOpen = true;
    }

    private void OnHideStatusHistory(object sender, RoutedEventArgs e) => StatusHistoryPopup.IsOpen = false;

    /// <summary>Clicking away closes the popup, and the button must not stay pressed.</summary>
    private void OnStatusHistoryClosed(object? sender, EventArgs e) => StatusHistoryButton.IsChecked = false;

    /// <summary>Says what was written and makes the folder a link, since a path alone is not.</summary>
    private void SetStatusWithFileLink(string message, string path)
    {
        var folder = Path.GetDirectoryName(path);

        if (folder is null)
        {
            SetStatus(message + path);
            return;
        }

        var link = new Hyperlink(new Run(folder)) { ToolTip = "Open this folder" };
        link.Click += (_, _) => OpenFolder(folder);

        StatusText.Inlines.Clear();
        StatusText.Inlines.Add(new Run(message));
        StatusText.Inlines.Add(link);
        RecordStatus(message + folder);
    }

    private void OpenFolder(string folder)
    {
        try
        {
            FolderOpener(folder);
        }
        catch (Exception ex)
        {
            SetStatus($"Could not open {folder}: {ex.Message}");
        }
    }

    /// <summary>Offers a file name in the configured format, inside the output folder.</summary>
    private string? AskForAudioPath(string suggestedStem)
    {
        var mp3 = _settings.OutputFormat == AudioOutputFormat.Mp3;

        var dialog = new SaveFileDialog
        {
            Title = mp3 ? "Write MP3 file" : "Write wave file",
            Filter = mp3
                ? "MP3 files (*.mp3)|*.mp3|Wave files (*.wav)|*.wav"
                : "Wave files (*.wav)|*.wav|MP3 files (*.mp3)|*.mp3",
            FileName = suggestedStem + (mp3 ? ".mp3" : ".wav"),
            AddExtension = true,
            DefaultExt = mp3 ? "mp3" : "wav",
        };

        var initial = EnsureOutputDirectory();
        if (initial is not null) dialog.InitialDirectory = initial;

        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    /// <summary>Creates the output folder so the dialog opens there the first time too.</summary>
    private string? EnsureOutputDirectory()
    {
        var directory = _settings.ResolvedOutputDirectory;

        try
        {
            Directory.CreateDirectory(directory);
            return directory;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Directory.Exists(directory) ? directory : null;
        }
    }

    /// <summary>The sink a run writes to, chosen by the file's own extension.</summary>
    private ISampleSink CreateFileSink(string path, int sampleRate, Action<string> progress)
    {
        var isMp3 = string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase);
        if (!isMp3) return new WaveFileSink(path, sampleRate);

        return new Mp3FileSink(path, sampleRate, _settings.Mp3BitRate, progress);
    }

    private static string? AskForDirectory(string? current)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        if (!string.IsNullOrWhiteSpace(current) && Directory.Exists(current)) dialog.SelectedPath = current;
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    private static TextReader OpenTextFile(string path) =>
        new StreamReader(path, detectEncodingFromByteOrderMarks: true);

    private static string ReadLicenseSummary(VoiceDescriptor voice)
    {
        var path = voice.LicensePath ?? voice.ModelCardPath;
        if (path is null || !File.Exists(path)) return string.Empty;

        try
        {
            var text = File.ReadAllText(path);
            return text.Length > 4000 ? text[..4000] + "..." : text;
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }

    private const int MaxQueuedSnippets = 8;

    private static int ParseInt(string text, int fallback) =>
        int.TryParse(text.Trim(), out var value) ? value : fallback;
}
