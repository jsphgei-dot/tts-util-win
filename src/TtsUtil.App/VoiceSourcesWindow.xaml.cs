/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace TtsUtil.App;

/// <summary>Points the reader at the places voices can be downloaded from, each link opening in
/// the browser.</summary>
public partial class VoiceSourcesWindow : Window
{
    /// <summary>The places worth looking, as a name, a line about it and its address.</summary>
    internal static IReadOnlyList<(string Name, string About, string Url)> Sources { get; } = new[]
    {
        ("sherpa-onnx voice models",
            "The release page the built in list downloads from. Every entry is a .tar.bz2 that can be pasted straight into the box.",
            "https://github.com/k2-fsa/sherpa-onnx/releases/tag/tts-models"),
        ("The sherpa-onnx model guide",
            "The same models written up by language and by kind, with a note on what each one needs.",
            "https://k2-fsa.github.io/sherpa/onnx/tts/pretrained_models/index.html"),
        ("Model archives on Hugging Face",
            "Mirrors of those archives, and newer ones that have not reached the release page yet.",
            "https://huggingface.co/csukuangfj"),
        ("Piper voice samples",
            "Somewhere to hear the Piper voices before downloading one. Take the matching archive from the pages above.",
            "https://rhasspy.github.io/piper-samples/"),
    };

    public VoiceSourcesWindow(Action<string> open)
    {
        InitializeComponent();
        SourceList.ItemsSource = Sources.Select(source => BuildEntry(source, open)).ToList();
    }

    private static StackPanel BuildEntry((string Name, string About, string Url) source, Action<string> open)
    {
        var link = new Hyperlink(new Run(source.Name)) { ToolTip = source.Url };
        link.Click += (_, _) => open(source.Url);

        var heading = new TextBlock { FontWeight = FontWeights.SemiBold };
        heading.Inlines.Add(link);

        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        panel.Children.Add(heading);
        panel.Children.Add(new TextBlock
        {
            Text = source.About,
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.DimGray,
        });

        return panel;
    }
}
