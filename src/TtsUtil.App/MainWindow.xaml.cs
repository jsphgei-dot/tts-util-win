/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.IO;
using System.Windows;
using System.Windows.Controls;
using TtsUtil.Core;
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
    private AudioPlayerSink? _player;
    private readonly SemaphoreSlim _typingTurnstile = new(1, 1);
    private int _queuedSnippets;
    private bool _initialising = true;
    private bool _busy;

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
    }

    /// <summary>Speaks one completed word during playback on input mode.</summary>
    internal Action<string> SpeakSnippet { get; set; }

    /// <summary>Reports an error as (message, title). Defaults to a modal dialog.</summary>
    internal Action<string, string> ErrorReporter { get; set; } = (message, title) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);

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
            SetStatus($"No voices found in {directory}. Run scripts\\FetchVoices.ps1 to download one.");
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
            PopulateSpeakers(engine.SpeakerCount);
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

    internal void PopulateSpeakers(int count)
    {
        _initialising = true;
        SpeakerBox.Items.Clear();
        for (var i = 0; i < count; i++) SpeakerBox.Items.Add(i.ToString());
        SpeakerBox.SelectedIndex = Math.Clamp(_settings.SpeakerId, 0, Math.Max(0, count - 1));
        _initialising = false;

        var visibility = count > 1 ? Visibility.Visible : Visibility.Collapsed;
        SpeakerBox.Visibility = visibility;
        SpeakerLabel.Visibility = visibility;
    }

    private async Task RunSynthesisAsync(Func<TextReader> readerFactory, long totalCharacters, string? outputPath)
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
        var speakerId = Math.Max(0, SpeakerBox.SelectedIndex);
        var speed = (float)SpeedSlider.Value;
        var progress = new Progress<SynthesisProgress>(p =>
        {
            Progress.Value = p.Percent;
            if (p.TotalCharacters > 0) SetStatus($"{p.Percent}% ({p.CharactersRead} of {p.TotalCharacters} characters)");
        });

        SetBusy(true);

        try
        {
            var filtered = await Task.Run(() =>
            {
                using var reader = readerFactory();
                var runner = new SynthesisRunner(engine, options) { SpeakerId = speakerId, Speed = speed };

                if (outputPath is null)
                {
                    using var player = new AudioPlayerSink(engine.SampleRate, token);
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
                    using var sink = new WaveFileSink(outputPath, engine.SampleRate);
                    runner.Run(reader, totalCharacters, sink, progress, token);
                }

                return runner.CharactersFiltered;
            }, token);

            var note = filtered > 0 ? $" {filtered} character(s) filtered." : string.Empty;
            SetStatus(outputPath is null ? $"Finished reading.{note}" : $"Wrote {outputPath}.{note}");
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

    private void OnReadText(object sender, RoutedEventArgs e)
    {
        var text = InputText.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetStatus("There is no text to read.");
            return;
        }

        _ = RunSynthesisAsync(() => new StringReader(text), text.Length, null);
    }

    private void OnSaveTextToWave(object sender, RoutedEventArgs e)
    {
        var text = InputText.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            SetStatus("There is no text to write.");
            return;
        }

        var path = AskForWavePath("tts_output.wav");
        if (path is null) return;

        _ = RunSynthesisAsync(() => new StringReader(text), text.Length, path);
    }

    private void OnReadFile(object sender, RoutedEventArgs e)
    {
        var path = FilePathBox.Text.Trim();
        if (!File.Exists(path))
        {
            SetStatus("Choose an existing text file first.");
            return;
        }

        var length = new FileInfo(path).Length;
        _ = RunSynthesisAsync(() => OpenTextFile(path), length, null);
    }

    private void OnSaveFileToWave(object sender, RoutedEventArgs e)
    {
        var path = FilePathBox.Text.Trim();
        if (!File.Exists(path))
        {
            SetStatus("Choose an existing text file first.");
            return;
        }

        var outputPath = AskForWavePath(Path.GetFileNameWithoutExtension(path) + ".wav");
        if (outputPath is null) return;

        var length = new FileInfo(path).Length;
        _ = RunSynthesisAsync(() => OpenTextFile(path), length, outputPath);
    }

    private void OnStop(object sender, RoutedEventArgs e)
    {
        CancelTypingPlayback();
        _cancellation?.Cancel();
        _player?.Stop();
        SetStatus("Stopping...");
    }

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
            Title = "Choose a text file",
            Filter = "Text files (*.txt;*.md;*.csv;*.log)|*.txt;*.md;*.csv;*.log|All files (*.*)|*.*",
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
        if (_initialising || SpeakerBox.SelectedIndex < 0) return;
        _settings.SpeakerId = SpeakerBox.SelectedIndex;
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
        var speakerId = Math.Max(0, SpeakerBox.SelectedIndex);
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

    private void SetStatus(string message) => StatusText.Text = message;

    private string? AskForWavePath(string suggestedName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Write wave file",
            Filter = "Wave files (*.wav)|*.wav",
            FileName = suggestedName,
            AddExtension = true,
            DefaultExt = "wav",
        };

        var initial = _settings.ResolvedOutputDirectory;
        if (Directory.Exists(initial)) dialog.InitialDirectory = initial;

        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
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
