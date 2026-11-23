using System.Text;
using System.Text.RegularExpressions;

namespace MadanuruVoterSeva.Backend.Services;

internal static class TeluguNameSearch
{
    private static readonly Dictionary<char, string> Consonants = new()
    {
        ['క'] = "k", ['ఖ'] = "kh", ['గ'] = "g", ['ఘ'] = "gh", ['ఙ'] = "ng",
        ['చ'] = "ch", ['ఛ'] = "chh", ['జ'] = "j", ['ఝ'] = "jh", ['ఞ'] = "ny",
        ['ట'] = "t", ['ఠ'] = "th", ['డ'] = "d", ['ఢ'] = "dh", ['ణ'] = "n",
        ['త'] = "t", ['థ'] = "th", ['ద'] = "d", ['ధ'] = "dh", ['న'] = "n",
        ['ప'] = "p", ['ఫ'] = "ph", ['బ'] = "b", ['భ'] = "bh", ['మ'] = "m",
        ['య'] = "y", ['ర'] = "r", ['ఱ'] = "r", ['ల'] = "l", ['ళ'] = "l",
        ['వ'] = "v", ['శ'] = "sh", ['ష'] = "sh", ['స'] = "s", ['హ'] = "h"
    };

    private static readonly Dictionary<char, string> Vowels = new()
    {
        ['అ'] = "a", ['ఆ'] = "a", ['ఇ'] = "i", ['ఈ'] = "i", ['ఉ'] = "u", ['ఊ'] = "u",
        ['ఋ'] = "ru", ['ౠ'] = "ru", ['ఌ'] = "lu", ['ౡ'] = "lu",
        ['ఎ'] = "e", ['ఏ'] = "e", ['ఐ'] = "ai", ['ఒ'] = "o", ['ఓ'] = "o", ['ఔ'] = "au"
    };

    private static readonly Dictionary<char, string> VowelSigns = new()
    {
        ['ా'] = "a", ['ి'] = "i", ['ీ'] = "i", ['ు'] = "u", ['ూ'] = "u",
        ['ృ'] = "ru", ['ౄ'] = "ru", ['ౢ'] = "lu", ['ౣ'] = "lu",
        ['ె'] = "e", ['ే'] = "e", ['ై'] = "ai", ['ొ'] = "o", ['ో'] = "o", ['ౌ'] = "au",
        ['్'] = ""
    };

    public static string NormalizeRoman(string value) =>
        Regex.Replace(value.ToLowerInvariant().Replace('-', ' '), @"([aeiou])\1+", "$1");

    public static string Romanize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";

        var result = new StringBuilder();
        var implicitVowel = false;
        foreach (var letter in name)
        {
            if (VowelSigns.TryGetValue(letter, out var sign))
            {
                result.Append(sign);
                implicitVowel = false;
                continue;
            }

            if (letter is '\u200C' or '\u200D') continue;
            if (implicitVowel) result.Append('a');
            implicitVowel = Consonants.TryGetValue(letter, out var consonant);

            if (implicitVowel) result.Append(consonant);
            else if (Vowels.TryGetValue(letter, out var vowel)) result.Append(vowel);
            else if (letter is 'ం' or 'ఁ') result.Append('n');
            else if (letter == 'ః') result.Append('h');
            else if (char.IsAsciiLetter(letter) || char.IsWhiteSpace(letter)) result.Append(letter);
        }
        if (implicitVowel) result.Append('a');
        return NormalizeRoman(result.ToString());
    }

    // Match one edit across adjacent name words, allowing the final typed word to be partial.
    public static bool HasOneEditPrefix(string? name, string[] typedWords)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;

        var words = name.Replace('-', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int start = 0; start <= words.Length - typedWords.Length; start++)
        {
            int edits = 0;
            int index = 0;
            for (; index < typedWords.Length; index++)
            {
                var typed = typedWords[index].AsSpan();
                var actual = words[start + index].AsSpan();
                bool last = index == typedWords.Length - 1;
                if (last ? actual.StartsWith(typed, StringComparison.Ordinal) : actual.SequenceEqual(typed))
                    continue;

                // Keep short words exact to avoid broad matches on common initials.
                if (++edits > 1 || typed.Length < 3 || !MatchesEditedWord(typed, actual, last)) break;
            }
            if (index == typedWords.Length && edits == 1) return true;
        }
        return false;
    }

    private static bool MatchesEditedWord(ReadOnlySpan<char> typed, ReadOnlySpan<char> actual, bool allowPrefix)
    {
        if (!allowPrefix) return IsOneEditApart(typed, actual);

        int shortest = typed.Length - 1;
        int longest = Math.Min(actual.Length, typed.Length + 1);
        for (int length = shortest; length <= longest; length++)
        {
            if (IsOneEditApart(typed, actual[..length])) return true;
        }
        return false;
    }

    private static bool IsOneEditApart(ReadOnlySpan<char> typed, ReadOnlySpan<char> actual)
    {
        if (Math.Abs(typed.Length - actual.Length) > 1) return false;
        int index = 0;
        while (index < Math.Min(typed.Length, actual.Length) && typed[index] == actual[index]) index++;

        if (typed.Length > actual.Length) return typed[(index + 1)..].SequenceEqual(actual[index..]);
        if (actual.Length > typed.Length) return typed[index..].SequenceEqual(actual[(index + 1)..]);
        if (index == typed.Length) return false;

        return typed[(index + 1)..].SequenceEqual(actual[(index + 1)..]) ||
            (index + 1 < typed.Length && typed[index] == actual[index + 1] &&
                typed[index + 1] == actual[index] && typed[(index + 2)..].SequenceEqual(actual[(index + 2)..]));
    }
}
