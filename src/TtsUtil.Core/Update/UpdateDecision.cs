/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Update;

/// <summary>What the window should do about a published release.</summary>
public enum UpdateAction
{
    /// <summary>Say nothing. There is nothing newer, or the reader has already said no to it.</summary>
    None,

    /// <summary>Mention it and link to the release page, which is all a portable copy can do.</summary>
    ShowLink,

    /// <summary>Offer to fetch the setup program and hand over to it.</summary>
    OfferSetup,
}

/// <summary>Where the published manifest lives, and where a reader is sent to fetch a build.</summary>
public static class UpdateSource
{
    /// <summary>The one repository, which carries the source, the manifest and the downloads. A
    /// copy built before 0.12.0 asks the retired distribution repository instead.</summary>
    public const string ManifestUrl =
        "https://raw.githubusercontent.com/jsphgei-dot/tts-util-win/master/latest.json";

    public const string ReleasesUrl = "https://github.com/jsphgei-dot/tts-util-win/releases";
}

/// <summary>Turns a manifest and the state of this copy into one of three outcomes.</summary>
public static class UpdateDecision
{
    /// <summary>
    /// A portable copy is only ever told where to look: replacing a folder it may be running
    /// from is the reader's business. An installed copy can be offered the setup program,
    /// which already knows how to upgrade in place.
    /// </summary>
    public static UpdateAction For(UpdateManifest? manifest, int runningVersionCode, bool portable,
        int dismissedVersionCode, string architecture)
    {
        if (manifest is null || !manifest.IsNewerThan(runningVersionCode)) return UpdateAction.None;
        if (manifest.VersionCode == dismissedVersionCode) return UpdateAction.None;
        if (portable) return UpdateAction.ShowLink;

        return manifest.SetupFor(architecture)?.IsUsable == true
            ? UpdateAction.OfferSetup
            : UpdateAction.ShowLink;
    }
}
