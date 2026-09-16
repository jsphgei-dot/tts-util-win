using TtsUtil.Core.Tts;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class VoiceInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "ttsutil-installer-" + Guid.NewGuid().ToString("N"));

    public VoiceInstallerTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string Voices => Path.Combine(_root, "voices");

    private static DownloadableVoice Voice => DownloadableVoices.All[0];

    [Fact]
    public void TheCatalogueOffersThreeRecommendedVoices()
    {
        Assert.Equal(3, DownloadableVoices.All.Count(voice => voice.IsRecommended));
    }

    [Fact]
    public void EveryCatalogueEntryIsUsable()
    {
        Assert.NotEmpty(DownloadableVoices.All);

        foreach (var voice in DownloadableVoices.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(voice.Id));
            Assert.False(string.IsNullOrWhiteSpace(voice.Language));
            Assert.False(string.IsNullOrWhiteSpace(voice.Licence));
            Assert.True(voice.SizeMb > 0);
            Assert.EndsWith(".tar.bz2", voice.ArchiveFileName, StringComparison.Ordinal);
            Assert.StartsWith("https://", DownloadableVoices.ArchiveUrl(voice), StringComparison.Ordinal);
        }

        Assert.Equal(DownloadableVoices.All.Count, DownloadableVoices.All.Select(v => v.Id).Distinct().Count());
    }

    [Fact]
    public void FindMatchesRegardlessOfCase()
    {
        Assert.NotNull(DownloadableVoices.Find("KOKORO-EN-V0_19"));
        Assert.Null(DownloadableVoices.Find("not-a-voice"));
    }

    [Fact]
    public void ALinkIsOnlyTakenWhenItPointsAtAnArchiveOverHttps()
    {
        Assert.Equal("my-voice", VoiceArchiveLink.NameFrom("https://example.com/models/my-voice.tar.bz2"));
        Assert.Equal("my-voice.tar.bz2",
            VoiceArchiveLink.FileNameFrom("https://example.com/models/my-voice.tar.bz2?download=true"));
        Assert.Equal("a voice", VoiceArchiveLink.NameFrom("https://example.com/a%20voice.zip"));

        Assert.Null(VoiceArchiveLink.NameFrom("http://example.com/my-voice.tar.bz2"));
        Assert.Null(VoiceArchiveLink.NameFrom("https://example.com/my-voice.onnx"));
        Assert.Null(VoiceArchiveLink.NameFrom("not a link"));
        Assert.Null(VoiceArchiveLink.NameFrom(null));
    }

    [Fact]
    public async Task ALinkedArchiveIsUnpackedBesideTheOtherVoices()
    {
        var installer = new VoiceInstaller(
            new FakeDownloader(),
            new FakeExtractor(_ => WriteUsableVoice(Path.Combine(Voices, "my-voice"))),
            _root);

        var directory = await installer.InstallFromLinkAsync("https://example.com/my-voice.tar.bz2", Voices);

        Assert.Equal(Path.Combine(Voices, "my-voice"), directory);
    }

    [Fact]
    public async Task ALinkedArchiveWithNoModelInItKeepsNothing()
    {
        var installer = new VoiceInstaller(
            new FakeDownloader(),
            new FakeExtractor(_ => Directory.CreateDirectory(Path.Combine(Voices, "my-voice"))),
            _root);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            installer.InstallFromLinkAsync("https://example.com/my-voice.tar.bz2", Voices));

        Assert.False(Directory.Exists(Path.Combine(Voices, "my-voice")));
        Assert.False(File.Exists(Path.Combine(_root, "my-voice.tar.bz2")));
    }

    [Fact]
    public void TheTargetDirectoryFallsBackWhenThePreferredOneIsReadOnly()
    {
        Assert.Equal(@"C:\preferred", VoiceInstaller.ChooseTargetDirectory(@"C:\preferred", @"C:\fallback", _ => true));
        Assert.Equal(@"C:\fallback", VoiceInstaller.ChooseTargetDirectory(@"C:\preferred", @"C:\fallback", _ => false));
    }

    [Fact]
    public void CanWriteReportsTrueForATemporaryDirectoryAndLeavesNoProbeBehind()
    {
        var directory = Path.Combine(_root, "probe-me");

        Assert.True(VoiceInstaller.CanWrite(directory));
        Assert.Empty(Directory.GetFiles(directory));
    }

    [Fact]
    public async Task InstallDownloadsExtractsAndReportsProgress()
    {
        var reports = new System.Collections.Concurrent.ConcurrentQueue<VoiceInstallProgress>();
        var installer = new VoiceInstaller(
            new FakeDownloader(bytes: 2048),
            new FakeExtractor(archive => WriteUsableVoice(Path.Combine(Voices, Voice.Id))),
            _root);

        var directory = await installer.InstallAsync(Voice, Voices, new Progress<VoiceInstallProgress>(reports.Enqueue));

        Assert.Equal(Path.Combine(Voices, Voice.Id), directory);
        Assert.True(VoiceInstaller.IsInstalled(Voice, Voices));

        // Progress<T> hands callbacks to the thread pool, so the first report can arrive late.
        await WaitForPhaseAsync(reports, VoiceInstallPhase.Downloading);
        await WaitForPhaseAsync(reports, VoiceInstallPhase.Finished);
    }

    [Fact]
    public async Task InstallRemovesTheFolderWhenTheArchiveHoldsNoModel()
    {
        var installer = new VoiceInstaller(
            new FakeDownloader(),
            new FakeExtractor(_ => Directory.CreateDirectory(Path.Combine(Voices, Voice.Id))),
            _root);

        await Assert.ThrowsAsync<InvalidOperationException>(() => installer.InstallAsync(Voice, Voices));

        Assert.False(Directory.Exists(Path.Combine(Voices, Voice.Id)));
    }

    [Fact]
    public async Task InstallLeavesNothingBehindWhenTheDownloadFails()
    {
        var installer = new VoiceInstaller(
            new ThrowingDownloader(),
            new FakeExtractor(_ => { }),
            _root);

        await Assert.ThrowsAsync<HttpRequestExceptionStandIn>(() => installer.InstallAsync(Voice, Voices));

        Assert.False(Directory.Exists(Path.Combine(Voices, Voice.Id)));
        Assert.False(File.Exists(Path.Combine(_root, Voice.ArchiveFileName)));
    }

    [Fact]
    public async Task InstallReplacesAPreviousCopy()
    {
        var target = Path.Combine(Voices, Voice.Id);
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "stale.txt"), "old");

        var installer = new VoiceInstaller(
            new FakeDownloader(),
            new FakeExtractor(_ => WriteUsableVoice(target)),
            _root);

        await installer.InstallAsync(Voice, Voices);

        Assert.False(File.Exists(Path.Combine(target, "stale.txt")));
        Assert.True(VoiceInstaller.IsInstalled(Voice, Voices));
    }

    [Fact]
    public async Task InstallDeletesTheArchiveWhenItIsDone()
    {
        var installer = new VoiceInstaller(
            new FakeDownloader(),
            new FakeExtractor(_ => WriteUsableVoice(Path.Combine(Voices, Voice.Id))),
            _root);

        await installer.InstallAsync(Voice, Voices);

        Assert.False(File.Exists(Path.Combine(_root, Voice.ArchiveFileName)));
    }

    [Fact]
    public async Task InstallHonoursCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        var installer = new VoiceInstaller(new CancellingDownloader(), new FakeExtractor(_ => { }), _root);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => installer.InstallAsync(Voice, Voices, null, source.Token));

        Assert.False(Directory.Exists(Path.Combine(Voices, Voice.Id)));
    }

    [Fact]
    public void RemoveDeletesAnInstalledVoiceAndIgnoresAMissingOne()
    {
        var target = Path.Combine(Voices, Voice.Id);
        WriteUsableVoice(target);
        Assert.True(VoiceInstaller.IsInstalled(Voice, Voices));

        VoiceInstaller.Remove(Voice, Voices);

        Assert.False(Directory.Exists(target));
        VoiceInstaller.Remove(Voice, Voices);
    }

    [Fact]
    public void IsInstalledRejectsAFolderWithoutAModel()
    {
        Directory.CreateDirectory(Path.Combine(Voices, Voice.Id));

        Assert.False(VoiceInstaller.IsInstalled(Voice, Voices));
    }

    private static void WriteUsableVoice(string directory)
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "tokens.txt"), "a 1");
        File.WriteAllBytes(Path.Combine(directory, "model.onnx"), new byte[64]);
    }

    private static async Task WaitForPhaseAsync(
        System.Collections.Concurrent.ConcurrentQueue<VoiceInstallProgress> reports, VoiceInstallPhase phase)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            if (reports.Any(report => report.Phase == phase)) return;
            await Task.Delay(10);
        }

        Assert.Fail($"No {phase} report arrived. Saw: {string.Join(", ", reports.Select(r => r.Phase))}");
    }

    private sealed class FakeDownloader : IVoiceDownloader
    {
        private readonly int _bytes;

        public FakeDownloader(int bytes = 64) => _bytes = bytes;

        public Task DownloadAsync(string url, string destinationPath, IProgress<VoiceInstallProgress>? progress,
            CancellationToken cancellationToken)
        {
            File.WriteAllBytes(destinationPath, new byte[_bytes]);

            progress?.Report(new VoiceInstallProgress
            {
                Phase = VoiceInstallPhase.Downloading,
                BytesReceived = _bytes,
                TotalBytes = _bytes,
            });

            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDownloader : IVoiceDownloader
    {
        public Task DownloadAsync(string url, string destinationPath, IProgress<VoiceInstallProgress>? progress,
            CancellationToken cancellationToken) => throw new HttpRequestExceptionStandIn();
    }

    private sealed class CancellingDownloader : IVoiceDownloader
    {
        public Task DownloadAsync(string url, string destinationPath, IProgress<VoiceInstallProgress>? progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeExtractor : IArchiveExtractor
    {
        private readonly Action<string> _onExtract;

        public FakeExtractor(Action<string> onExtract) => _onExtract = onExtract;

        public Task ExtractAsync(string archivePath, string destinationDirectory,
            CancellationToken cancellationToken)
        {
            _onExtract(archivePath);
            return Task.CompletedTask;
        }
    }

    public sealed class HttpRequestExceptionStandIn : Exception
    {
    }
}
