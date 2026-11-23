using System.Text.RegularExpressions;
using SesliAsistan.Business.Interfaces;

namespace SesliAsistan.Business.Speech;

public record KeyTerm(string Term, string Stem, double Score);

public interface IKeyTermExtractor
{
    IReadOnlyList<KeyTerm> ExtractKeyTerms(
        string text,
        string? title = null,
        IReadOnlyList<string>? boldTerms = null,
        int maxTerms = 15);
}

public class KeyTermExtractor : IKeyTermExtractor
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "acaba", "altı", "ama", "ancak", "arada", "artık", "asla", "aslında", "ayrıca", "az",
        "bana", "bazen", "bazı", "belki", "ben", "benden", "beni", "benim", "beri", "beş",
        "bile", "bin", "bir", "biraz", "biri", "birkaç", "birkez", "birçok", "birşey", "biz",
        "bizden", "bize", "bizi", "bizim", "böyle", "böylece", "bu", "buna", "bunda", "bundan",
        "bunlar", "bunları", "bunların", "bunu", "bunun", "burada", "çok", "çünkü", "da", "daha",
        "dahi", "de", "defa", "değil", "diğer", "diye", "dokuz", "dolayı", "dolayısıyla", "dört",
        "edecek", "eden", "ederek", "edilecek", "ediliyor", "edilmesi", "ediyor", "eğer", "elli",
        "en", "etmesi", "etti", "ettiği", "ettiğini", "gibi", "göre", "halen", "hangi", "hatta",
        "hem", "henüz", "hep", "hepsi", "her", "herhangi", "herkes", "herkesin", "hiç", "hiçbir",
        "için", "iki", "ile", "ilgili", "ise", "işte", "itibaren", "itibariyle", "kadar", "karşın",
        "katrilyon", "kendi", "kendine", "kendini", "kendisi", "kendisine", "kendisini", "kez",
        "ki", "kim", "kimden", "kime", "kimi", "kimse", "kırk", "milyar", "milyon", "mu", "mü",
        "mı", "nasıl", "ne", "neden", "nedenle", "nerde", "nerede", "nereye", "nitekim", "ona",
        "ondan", "onlar", "onlardan", "onlara", "onların", "onlarca", "onu", "onun", "orada",
        "oysa", "oysaki", "öbürü", "önce", "önceden", "ötürü", "öyle", "sekiz", "sen", "senden",
        "sene", "seni", "senin", "siz", "sizden", "size", "sizi", "sizin", "sonra", "sonradan",
        "şayet", "şey", "şeyden", "şeyler", "şimdi", "şöyle", "şu", "şuna", "şunda", "şundan",
        "şunları", "şunu", "tarafından", "trilyon", "tüm", "üç", "üzere", "var", "vardı", "ve",
        "veya", "veyahut", "ya", "yahut", "yalnız", "yani", "yapacak", "yapılan", "yapılması",
        "yapıyor", "yedi", "yerine", "yine", "yok", "yoksa", "yüz", "zaten"
    };

    public IReadOnlyList<KeyTerm> ExtractKeyTerms(
        string text,
        string? title = null,
        IReadOnlyList<string>? boldTerms = null,
        int maxTerms = 15)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<KeyTerm>();

        var titleStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(title))
        {
            var titleWords = Regex.Split(title.ToLowerInvariant(), @"[^\p{L}0-9]+");
            foreach (var w in titleWords)
            {
                if (w.Length >= 2 && !StopWords.Contains(w))
                    titleStems.Add(GetF5Stem(w));
            }
        }

        var boldStems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (boldTerms is not null)
        {
            foreach (var b in boldTerms)
            {
                var bWords = Regex.Split(b.ToLowerInvariant(), @"[^\p{L}0-9]+");
                foreach (var w in bWords)
                {
                    if (w.Length >= 2 && !StopWords.Contains(w))
                        boldStems.Add(GetF5Stem(w));
                }
            }
        }

        // 1. Tekil kelime frekansları (F5 kök bazında)
        var words = Regex.Split(text.ToLowerInvariant(), @"[^\p{L}0-9]+");
        var stemCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var canonicalTerms = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < words.Length; i++)
        {
            var word = words[i];
            if (word.Length < 3 || StopWords.Contains(word)) continue;

            var stem = GetF5Stem(word);
            stemCounts[stem] = stemCounts.GetValueOrDefault(stem, 0) + 1;

            if (!canonicalTerms.TryGetValue(stem, out var existing) || word.Length > existing.Length)
            {
                canonicalTerms[stem] = word;
            }
        }

        // 2. Çok kelimeli ifadeler (2-3 kelimeli n-gramlar, ör. "veri tabanı", "yapay zeka")
        var multiWordCounts = new Dictionary<string, (string Canonical, int Count, string Stem)>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < words.Length - 1; i++)
        {
            var w1 = words[i];
            var w2 = words[i + 1];
            if (w1.Length >= 3 && w2.Length >= 3 && !StopWords.Contains(w1) && !StopWords.Contains(w2))
            {
                var bigram = $"{w1} {w2}";
                var bigramStem = $"{GetF5Stem(w1)}_{GetF5Stem(w2)}";
                if (!multiWordCounts.ContainsKey(bigramStem))
                    multiWordCounts[bigramStem] = (bigram, 1, bigramStem);
                else
                {
                    var prev = multiWordCounts[bigramStem];
                    multiWordCounts[bigramStem] = (prev.Canonical, prev.Count + 1, bigramStem);
                }
            }
        }

        // 3. Puanlama (TF + Başlık + Koyu bonusu)
        var termCandidates = new List<KeyTerm>();

        // Çok kelimeliler (en az 2 kez geçiyorsa veya başlık/kalında geçiyorsa aday)
        foreach (var (_, (canonical, count, stem)) in multiWordCounts)
        {
            if (count >= 2 || titleStems.Contains(GetF5Stem(canonical.Split(' ')[0])))
            {
                double score = count * 2.5;
                if (titleStems.Contains(GetF5Stem(canonical.Split(' ')[0]))) score += 4.0;
                termCandidates.Add(new KeyTerm(canonical, stem, score));
            }
        }

        // Tek kelimeliler
        foreach (var (stem, count) in stemCounts)
        {
            double score = count * 1.0;
            if (titleStems.Contains(stem)) score += 3.0;
            if (boldStems.Contains(stem)) score += 2.0;

            var canonical = canonicalTerms[stem];
            termCandidates.Add(new KeyTerm(canonical, stem, score));
        }

        // 4. Tekilleştirme: Aynı F5 köküne sahip adayları birleştir, en yüksek puanlıyı tut
        var deduped = new Dictionary<string, KeyTerm>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in termCandidates.OrderByDescending(c => c.Score))
        {
            if (!deduped.ContainsKey(candidate.Stem))
            {
                deduped[candidate.Stem] = candidate;
            }
        }

        return deduped.Values
            .OrderByDescending(c => c.Score)
            .Take(maxTerms)
            .ToList();
    }

    public static string GetF5Stem(string word)
    {
        var clean = word.ToLowerInvariant();
        return clean.Length > 5 ? clean[..5] : clean;
    }
}
