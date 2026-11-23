using System.Text.RegularExpressions;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.Business.Speech;

public interface IDefinitionMatcher
{
    string? FindDefinition(string term, string text);
}

public class DefinitionMatcher : IDefinitionMatcher
{
    // Türkçe tanım ekleri: -dır/-dir/-dur/-dür/-tır/-tir/-tur/-tür
    private static readonly Regex CopulaRegex = new(
        @"(?:dır|dir|dur|dür|tır|tir|tur|tür)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string? FindDefinition(string term, string text)
    {
        if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(text))
            return null;

        var sentences = TextChunker.SplitIntoSentences(text);
        if (sentences.Count == 0) return null;

        var termStem = KeyTermExtractor.GetF5Stem(term);
        var escapedTerm = Regex.Escape(term);

        // 1. "X: Tanım..." kalıbı (İki nokta üst üste)
        var colonPattern = new Regex($@"^{escapedTerm}\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
        var colonMatch = colonPattern.Match(text);
        if (colonMatch.Success)
        {
            var def = colonMatch.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(def)) return def;
        }

        // 2. "... denir" kalıbı
        foreach (var sentence in sentences)
        {
            if (ContainsTerm(sentence, term, termStem) &&
                Regex.IsMatch(sentence, @"\bdenir\b", RegexOptions.IgnoreCase))
            {
                return CleanSentence(sentence);
            }
        }

        // 3. "... olarak adlandırılır" kalıbı
        foreach (var sentence in sentences)
        {
            if (ContainsTerm(sentence, term, termStem) &&
                Regex.IsMatch(sentence, @"\b(?:olarak\s+)?adlandırılır\b", RegexOptions.IgnoreCase))
            {
                return CleanSentence(sentence);
            }
        }

        // 4. "... -dır/-dir/-dur/-dür/-tır/-tir/-tur/-tür" ek varyantları kalıbı
        foreach (var sentence in sentences)
        {
            if (ContainsTerm(sentence, term, termStem) && CopulaRegex.IsMatch(sentence))
            {
                return CleanSentence(sentence);
            }
        }

        // 5. Fallback: Kalıp bulunamadığında terimin geçtiği en uygun (en yüksek puanlı/anlamlı) cümleyi seç
        var candidateSentences = sentences
            .Where(s => ContainsTerm(s, term, termStem))
            .ToList();

        if (candidateSentences.Count > 0)
        {
            // En uzun ve bilgilendirici cümleyi seç (veya ilk geçen açıklayıcı cümle)
            var best = candidateSentences.OrderByDescending(s => s.Length).First();
            return CleanSentence(best);
        }

        return null;
    }

    private static bool ContainsTerm(string sentence, string term, string termStem)
    {
        if (sentence.Contains(term, StringComparison.OrdinalIgnoreCase))
            return true;

        var words = Regex.Split(sentence.ToLowerInvariant(), @"[^\p{L}0-9]+");
        return words.Any(w => KeyTermExtractor.GetF5Stem(w).Equals(termStem, StringComparison.OrdinalIgnoreCase));
    }

    private static string CleanSentence(string sentence)
    {
        var clean = sentence.Trim();
        if (!clean.EndsWith('.') && !clean.EndsWith('!') && !clean.EndsWith('?'))
            clean += ".";
        return clean;
    }
}
