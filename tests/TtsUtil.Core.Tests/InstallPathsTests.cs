using TtsUtil.Core.Settings;
using Xunit;

namespace TtsUtil.Core.Tests;

public class InstallPathsTests
{
    private const string AppDir = @"C:\Program Files\TtsUtilWin";
    private const string LocalAppData = @"C:\Users\someone\AppData\Local";
    private const string RoamingAppData = @"C:\Users\someone\AppData\Roaming";

    private static readonly Func<string, bool> NoVoices = _ => false;
    private static readonly Func<string, bool> NoFiles = _ => false;

    // --- Voices ---

    [Fact]
    public void AConfiguredDirectoryBeatsEverythingElse()
    {
        var resolved = InstallPaths.ResolveVoicesDirectory(
            @"D:\my voices", @"E:\env voices", LocalAppData, AppDir, _ => true);

        Assert.Equal(@"D:\my voices", resolved);
    }

    [Fact]
    public void TheEnvironmentVariableBeatsTheInstalledLocation()
    {
        var resolved = InstallPaths.ResolveVoicesDirectory(
            null, @"E:\env voices", LocalAppData, AppDir, _ => true);

        Assert.Equal(@"E:\env voices", resolved);
    }

    [Fact]
    public void BlankOverridesAreIgnored()
    {
        var resolved = InstallPaths.ResolveVoicesDirectory(
            "   ", "", LocalAppData, AppDir, NoVoices);

        Assert.Equal(Path.Combine(LocalAppData, "TtsUtilWin", "voices"), resolved);
    }

    [Fact]
    public void TheInstalledLocationIsUsedWhenItHoldsVoices()
    {
        var installed = Path.Combine(LocalAppData, "TtsUtilWin", "voices");

        var resolved = InstallPaths.ResolveVoicesDirectory(
            null, null, LocalAppData, AppDir, path => path == installed);

        Assert.Equal(installed, resolved);
    }

    [Fact]
    public void AFolderBesideTheExecutableIsUsedWhenItHoldsVoices()
    {
        var beside = Path.Combine(AppDir, "voices");

        var resolved = InstallPaths.ResolveVoicesDirectory(
            null, null, LocalAppData, AppDir, path => path == beside);

        Assert.Equal(beside, resolved);
    }

    [Fact]
    public void TheInstalledLocationWinsOverTheOneBesideTheExecutable()
    {
        var installed = Path.Combine(LocalAppData, "TtsUtilWin", "voices");

        var resolved = InstallPaths.ResolveVoicesDirectory(
            null, null, LocalAppData, AppDir, _ => true);

        Assert.Equal(installed, resolved);
    }

    [Fact]
    public void WithNothingInstalledTheDownloadTargetIsTheUserFolder()
    {
        var resolved = InstallPaths.ResolveVoicesDirectory(
            null, null, LocalAppData, AppDir, NoVoices);

        Assert.Equal(Path.Combine(LocalAppData, "TtsUtilWin", "voices"), resolved);
    }

    [Fact]
    public void APortableCopyKeepsItsVoicesBesideTheExecutable()
    {
        var portableDir = Path.Combine(Path.GetTempPath(), "TtsUtilWinPortableProbe");
        Directory.CreateDirectory(portableDir);
        var marker = Path.Combine(portableDir, "portable.txt");

        try
        {
            File.WriteAllText(marker, string.Empty);

            var resolved = InstallPaths.ResolveVoicesDirectory(
                null, null, LocalAppData, portableDir, NoVoices);

            Assert.Equal(Path.Combine(portableDir, "voices"), resolved);
        }
        finally
        {
            Directory.Delete(portableDir, recursive: true);
        }
    }

    // --- Settings ---

    [Fact]
    public void AnInstalledCopyKeepsSettingsInRoaming()
    {
        var path = InstallPaths.ResolveSettingsPath(AppDir, RoamingAppData, NoFiles);

        Assert.Equal(Path.Combine(RoamingAppData, "TtsUtilWin", "settings.json"), path);
    }

    [Fact]
    public void ThePortableMarkerKeepsSettingsBesideTheExecutable()
    {
        var marker = Path.Combine(AppDir, "portable.txt");

        var path = InstallPaths.ResolveSettingsPath(AppDir, RoamingAppData, file => file == marker);

        Assert.Equal(Path.Combine(AppDir, "settings.json"), path);
    }

    [Fact]
    public void AnExistingLocalSettingsFileAlsoMeansPortable()
    {
        var local = Path.Combine(AppDir, "settings.json");

        var path = InstallPaths.ResolveSettingsPath(AppDir, RoamingAppData, file => file == local);

        Assert.Equal(local, path);
    }

    [Fact]
    public void PortableDetectionMatchesTheSettingsChoice()
    {
        Assert.False(InstallPaths.IsPortable(AppDir, NoFiles));
        Assert.True(InstallPaths.IsPortable(AppDir, file => file.EndsWith("portable.txt", StringComparison.Ordinal)));
    }

    // --- Voice folder detection ---

    [Fact]
    public void HasVoicesIsFalseForMissingOrEmptyDirectories()
    {
        var empty = Path.Combine(Path.GetTempPath(), "TtsUtilWinEmptyProbe");
        Directory.CreateDirectory(empty);

        try
        {
            Assert.False(InstallPaths.HasVoices(Path.Combine(empty, "nope")));
            Assert.False(InstallPaths.HasVoices(empty));

            Directory.CreateDirectory(Path.Combine(empty, "a-voice"));
            Assert.True(InstallPaths.HasVoices(empty));
        }
        finally
        {
            Directory.Delete(empty, recursive: true);
        }
    }
}
