using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using TtsUtil.Core.Update;
using Xunit;

namespace TtsUtil.Core.Tests;

public sealed class UpdateTests
{
    [Fact]
    public void AManifestIsReadIntoItsParts()
    {
        var manifest = UpdateManifest.Parse(Json(code: 9, sha: new string('a', 64)));

        Assert.NotNull(manifest);
        Assert.Equal("0.9.0-beta", manifest!.VersionName);
        Assert.Equal(9, manifest.VersionCode);
        Assert.True(manifest.Setup!.IsUsable);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("{}")]
    [InlineData("{ QVersionNameQ: QQ, QVersionCodeQ: 3 }")]
    public void TextThatIsNotAManifestGivesNothingRatherThanThrowing(string json)
    {
        Assert.Null(UpdateManifest.Parse(json.Replace('Q', '"')));
    }

    [Fact]
    public void AnUnusableDownloadIsRejectedBeforeAnythingIsFetched()
    {
        // Plain HTTP, and a hash that is not a SHA256, are both reasons not to start.
        Assert.False(new UpdateDownload { Url = "http://example.com/x.zip", Sha256 = new string('a', 64) }.IsUsable);
        Assert.False(new UpdateDownload { Url = "https://example.com/x.zip", Sha256 = "short" }.IsUsable);
    }

    [Fact]
    public void ACopyChecksOnceADayAndNotOnEveryStart()
    {
        var now = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);

        Assert.True(UpdateChecker.IsDue(enabled: true, lastCheckUtc: null, now));
        Assert.True(UpdateChecker.IsDue(true, now.AddDays(-2), now));
        Assert.False(UpdateChecker.IsDue(true, now.AddHours(-1), now));
        Assert.False(UpdateChecker.IsDue(enabled: false, lastCheckUtc: null, now));
    }

    [Theory]
    [InlineData(6, false, 0, UpdateAction.None)]          // the running build is the newest
    [InlineData(5, false, 0, UpdateAction.OfferSetup)]    // installed, so setup can do the work
    [InlineData(5, true, 0, UpdateAction.ShowLink)]       // portable copies are only pointed at it
    [InlineData(5, false, 6, UpdateAction.None)]          // already said no to this one
    public void WhoIsOfferedWhat(int runningCode, bool portable, int dismissed, UpdateAction expected)
    {
        var manifest = UpdateManifest.Parse(Json(code: 6, sha: new string('a', 64)));

        Assert.Equal(expected, UpdateDecision.For(manifest, runningCode, portable, dismissed));
    }

    [Fact]
    public async Task ADownloadThatMatchesItsHashYieldsTheSetupProgram()
    {
        using var temp = new TempFolder();
        var zip = BuildSetupZip(temp.Path);

        var (result, setupPath) = await FetchAsync(zip, Hash(zip), temp.Path);

        Assert.Equal(UpdateFetchResult.Ready, result);
        Assert.EndsWith("TtsUtilWin-0.9.0-beta-setup.exe", setupPath);
    }

    [Fact]
    public async Task ADownloadThatDoesNotMatchItsHashIsThrownAway()
    {
        using var temp = new TempFolder();
        var zip = BuildSetupZip(temp.Path);

        var (result, setupPath) = await FetchAsync(zip, new string('b', 64), temp.Path);

        Assert.Equal(UpdateFetchResult.HashMismatch, result);
        Assert.Null(setupPath);
    }

    private static async Task<(UpdateFetchResult Result, string? SetupPath)> FetchAsync(string zipPath,
        string sha256, string workingDirectory)
    {
        using var client = new HttpClient(new FileHandler(zipPath));
        using var installer = new UpdateInstaller(client);

        var download = new UpdateDownload
        {
            Url = "https://example.invalid/setup.zip",
            Bytes = new FileInfo(zipPath).Length,
            Sha256 = sha256,
        };

        return await installer.FetchAsync(download, Path.Combine(workingDirectory, "work"), null,
            CancellationToken.None);
    }

    private static string BuildSetupZip(string folder)
    {
        var staging = Path.Combine(folder, "staging");
        Directory.CreateDirectory(staging);
        File.WriteAllText(Path.Combine(staging, "TtsUtilWin-0.9.0-beta-setup.exe"), "not really a program");

        var zip = Path.Combine(folder, "release.zip");
        ZipFile.CreateFromDirectory(staging, zip);
        return zip;
    }

    private static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
    }

    private static string Json(int code, string sha) => $@"{{
        QVersionNameQ: Q0.9.0-betaQ,
        QVersionCodeQ: {code},
        QReleaseUrlQ: Qhttps://example.invalid/releasesQ,
        QSetupQ: {{ QUrlQ: Qhttps://example.invalid/setup.zipQ, QBytesQ: 1234, QSha256Q: Q{sha}Q }}
    }}".Replace('Q', '"');

    /// <summary>Serves one local file, so the fetch path is exercised without a network.</summary>
    private sealed class FileHandler : HttpMessageHandler
    {
        private readonly string _path;

        public FileHandler(string path) => _path = path;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(File.ReadAllBytes(_path)),
            });
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TtsUtilWinUpdate",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
