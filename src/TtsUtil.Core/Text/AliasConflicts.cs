/*
 * TTS Util Win
 *
 * Licensed under the Apache License, Version 2.0.
 */

namespace TtsUtil.Core.Text;

/// <summary>Why a rule never gets to fire.</summary>
public enum AliasClash
{
    /// <summary>An earlier rule looks for the same thing, so this one is never reached.</summary>
    SameWord,

    /// <summary>An earlier rule matches inside this one and takes the text first.</summary>
    EatenByEarlier,
}

/// <summary>One rule that cannot fire, and the earlier rule standing in its way.</summary>
public sealed record AliasConflict(int Index, AliasRule Rule, int EarlierIndex, AliasRule Earlier, AliasClash Clash)
{
    /// <summary>A line for the reader, naming both rules.</summary>
    public string Describe() => Clash == AliasClash.SameWord
        ? $"{Rule.Match} is listed twice, so only the first is used"
        : $"{Rule.Match} never fires: {Earlier.Match} above it matches inside it first";
}

/// <summary>Finds rules that a rule above them already swallows, which is the one way a list can
/// quietly do nothing.</summary>
public static class AliasConflicts
{
    /// <summary>Every rule that cannot fire, in the order they are listed.</summary>
    public static IReadOnlyList<AliasConflict> Find(IEnumerable<AliasRule> rules)
    {
        var live = rules.Select((rule, index) => (rule, index))
            .Where(pair => pair.rule.Enabled && pair.rule.Match.Length > 0)
            .ToList();

        var found = new List<AliasConflict>();

        for (var later = 0; later < live.Count; later++)
        {
            for (var earlier = 0; earlier < later; earlier++)
            {
                var clash = ClashBetween(live[earlier].rule, live[later].rule);
                if (clash is null) continue;

                found.Add(new AliasConflict(
                    live[later].index, live[later].rule, live[earlier].index, live[earlier].rule, clash.Value));

                break;
            }
        }

        return found;
    }

    /// <summary>One line naming what is wrong, or null when every rule can fire.</summary>
    public static string? Summarize(IEnumerable<AliasRule> rules)
    {
        var found = Find(rules);
        if (found.Count == 0) return null;

        return found.Count == 1
            ? found[0].Describe()
            : $"{found.Count} rules never fire, starting with {found[0].Describe()}";
    }

    private static AliasClash? ClashBetween(AliasRule earlier, AliasRule later)
    {
        var comparison = earlier.MatchCase || later.MatchCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        if (string.Equals(earlier.Match, later.Match, comparison)) return AliasClash.SameWord;

        // A whole word rule only takes text the longer rule needs when it stands alone in it.
        if (later.Match.IndexOf(earlier.Match, comparison) < 0) return null;
        if (earlier.WholeWord && !StandsAlone(later.Match, earlier.Match, comparison)) return null;

        return AliasClash.EatenByEarlier;
    }

    /// <summary>True when the shorter text sits inside the longer one with no letter either side.</summary>
    private static bool StandsAlone(string text, string part, StringComparison comparison)
    {
        for (var at = text.IndexOf(part, comparison); at >= 0; at = text.IndexOf(part, at + 1, comparison))
        {
            var before = at == 0 || !IsWordCharacter(text[at - 1]);
            var end = at + part.Length;
            var after = end == text.Length || !IsWordCharacter(text[end]);

            if (before && after) return true;
        }

        return false;
    }

    private static bool IsWordCharacter(char value) => char.IsLetterOrDigit(value) || value == '_';
}
