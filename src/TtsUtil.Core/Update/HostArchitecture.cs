/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

using System.Runtime.InteropServices;

namespace TtsUtil.Core.Update;

/// <summary>The architecture a copy runs as, spelled the way a published manifest spells it.</summary>
public static class HostArchitecture
{
    public const string X64 = "x64";

    public const string Arm64 = "arm64";

    public const string X86 = "x86";

    /// <summary>What this process is, which is not always what the machine could run.</summary>
    public static string Current => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.Arm64 => Arm64,
        Architecture.X86 => X86,
        _ => X64,
    };
}
