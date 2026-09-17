/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Runtime.InteropServices;

namespace TtsUtil.Core.Settings;

/// <summary>Turns the synthesis thread setting into a thread count, where no setting means automatic.</summary>
public static class ThreadPlan
{
    /// <summary>Range a reader may set by hand.</summary>
    public const int Minimum = 1;

    public const int Maximum = 16;

    /// <summary>Most of the speed of the heaviest voice arrives by four threads.</summary>
    public const int AutomaticCeiling = 4;

    /// <summary>A machine this wide has cores to spare, so automatic takes the last of the speed too.</summary>
    public const int LargeMachineProcessors = 32;

    public static int Resolve(int? setting) =>
        Resolve(setting, Environment.ProcessorCount, RuntimeInformation.ProcessArchitecture == Architecture.X86);

    /// <summary>Half the processors, kept within the automatic range, leaving the rest of the machine free.</summary>
    public static int Resolve(int? setting, int processorCount, bool x86 = false) =>
        setting is int chosen
            ? Math.Clamp(chosen, Minimum, Maximum)
            : Math.Clamp(processorCount / 2 * (x86 ? 2 : 1), Minimum, CeilingFor(processorCount, x86));

    /// <summary>The 32 bit build renders less per thread, so its automatic range is doubled.</summary>
    public static int CeilingFor(int processorCount, bool x86) =>
        Math.Min(Maximum, AutomaticCeiling * (processorCount >= LargeMachineProcessors ? 2 : 1) * (x86 ? 2 : 1));
}
