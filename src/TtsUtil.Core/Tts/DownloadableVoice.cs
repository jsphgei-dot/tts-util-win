/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Tts;

/// <summary>A voice model that can be downloaded from the sherpa-onnx release page.</summary>
public sealed class DownloadableVoice
{
    public string Id { get; init; } = string.Empty;

    public string Language { get; init; } = string.Empty;

    public VoiceModelKind Kind { get; init; }

    public int SizeMb { get; init; }

    public string Licence { get; init; } = string.Empty;

    /// <summary>True for the permissively licensed voices offered by default.</summary>
    public bool IsRecommended { get; init; }

    /// <summary>True where the strict character setting would remove the whole script.</summary>
    public bool NonLatinScript { get; init; }

    public string ArchiveFileName => Id + ".tar.bz2";
}

/// <summary>The curated set of voices the program offers to install.</summary>
public static class DownloadableVoices
{
    public const string BaseUrl = "https://github.com/k2-fsa/sherpa-onnx/releases/download/tts-models/";

    public static IReadOnlyList<DownloadableVoice> All { get; } = new[]
    {
        new DownloadableVoice
        {
            Id = "vits-piper-en_US-ljspeech-high",
            Language = "English (US), single speaker",
            Kind = VoiceModelKind.Vits,
            SizeMb = 110,
            Licence = "LJ Speech data set, public domain",
            IsRecommended = true,
        },
        new DownloadableVoice
        {
            Id = "vits-piper-en_US-libritts_r-medium",
            Language = "English (US), 900+ speakers",
            Kind = VoiceModelKind.Vits,
            SizeMb = 78,
            Licence = "LibriTTS-R, CC BY 4.0",
            IsRecommended = true,
        },
        new DownloadableVoice
        {
            Id = "kokoro-en-v0_19",
            Language = "English, 11 voices",
            Kind = VoiceModelKind.Kokoro,
            SizeMb = 305,
            Licence = "Apache 2.0",
            IsRecommended = true,
        },
        new DownloadableVoice
        {
            Id = "vits-piper-en_GB-alan-medium",
            Language = "English (GB), single speaker",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-en_US-amy-low",
            Language = "English (US), small and fast",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-en_US-lessac-medium",
            Language = "English (US), single speaker",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-es_ES-davefx-medium",
            Language = "Spanish (Spain)",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-es_MX-ald-medium",
            Language = "Spanish (Mexico)",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-fr_FR-siwis-medium",
            Language = "French",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-de_DE-thorsten-medium",
            Language = "German",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-it_IT-paola-medium",
            Language = "Italian",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-pt_BR-faber-medium",
            Language = "Portuguese (Brazil)",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-nl_NL-ronnie-medium",
            Language = "Dutch",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-pl_PL-darkman-medium",
            Language = "Polish",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-sv_SE-nst-medium",
            Language = "Swedish",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-tr_TR-fahrettin-medium",
            Language = "Turkish",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-vi_VN-vais1000-medium",
            Language = "Vietnamese",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
        },
        new DownloadableVoice
        {
            Id = "vits-piper-ru_RU-irina-medium",
            Language = "Russian",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
            NonLatinScript = true,
        },
        new DownloadableVoice
        {
            Id = "vits-piper-uk_UA-ukrainian_tts-medium",
            Language = "Ukrainian",
            Kind = VoiceModelKind.Vits,
            SizeMb = 77,
            Licence = "See MODEL_CARD in the folder",
            NonLatinScript = true,
        },
        new DownloadableVoice
        {
            Id = "vits-piper-el_GR-rapunzelina-low",
            Language = "Greek",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
            NonLatinScript = true,
        },
        new DownloadableVoice
        {
            Id = "vits-piper-ar_JO-kareem-medium",
            Language = "Arabic",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
            NonLatinScript = true,
        },
        new DownloadableVoice
        {
            Id = "vits-piper-hi_IN-pratham-medium",
            Language = "Hindi",
            Kind = VoiceModelKind.Vits,
            SizeMb = 64,
            Licence = "See MODEL_CARD in the folder",
            NonLatinScript = true,
        },
        new DownloadableVoice
        {
            Id = "vits-icefall-zh-aishell3",
            Language = "Chinese, 174 speakers",
            Kind = VoiceModelKind.Vits,
            SizeMb = 30,
            Licence = "See the folder",
            NonLatinScript = true,
        },
        new DownloadableVoice
        {
            Id = "vits-melo-tts-zh_en",
            Language = "Chinese and English",
            Kind = VoiceModelKind.Vits,
            SizeMb = 159,
            Licence = "See the folder",
            NonLatinScript = true,
        },
    };

    public static string ArchiveUrl(DownloadableVoice voice) => BaseUrl + voice.ArchiveFileName;

    public static DownloadableVoice? Find(string id) =>
        All.FirstOrDefault(voice => string.Equals(voice.Id, id, StringComparison.OrdinalIgnoreCase));
}
