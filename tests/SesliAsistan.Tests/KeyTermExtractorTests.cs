using FluentAssertions;
using SesliAsistan.Business.Speech;
using Xunit;

namespace SesliAsistan.Tests;

public class KeyTermExtractorTests
{
    private readonly KeyTermExtractor _extractor = new();

    [Fact]
    public void ExtractKeyTerms_F5KokBirlestirme_AyniKokeSahipKelimeleriBirlestirir()
    {
        // "algoritma", "algoritmanın", "algoritmalar" -> kök "algor"
        var text = "Algoritma tasarımı önemlidir. Algoritmanın karmaşıklığı hesaplanır. Farklı algoritmalar karşılaştırılır.";
        var terms = _extractor.ExtractKeyTerms(text, maxTerms: 5);

        terms.Should().ContainSingle(t => t.Stem == "algor");
    }

    [Fact]
    public void ExtractKeyTerms_DurakKelimeTemizligi_StopWordsListesindekiKelimeleriEler()
    {
        var text = "Bu ve veya çünkü ama ancak gibi kelimeler ders notlarında çok sık geçer.";
        var terms = _extractor.ExtractKeyTerms(text);

        terms.Select(t => t.Term.ToLower()).Should().NotContain(new[] { "bu", "ve", "veya", "çünkü", "ama", "ancak", "gibi", "çok" });
    }

    [Fact]
    public void ExtractKeyTerms_BasliktakiTerimeEkPuan_BasliktaGecenTeriminPuaniYuksekOlur()
    {
        var text = "Mikroişlemci mimarisinde veri yolları ve komut çevrimleri incelenir. Bellek yapısı önemlidir.";
        var termsWithTitle = _extractor.ExtractKeyTerms(text, title: "Mikroişlemci Mimarisi");
        var termsWithoutTitle = _extractor.ExtractKeyTerms(text);

        var scoreWithTitle = termsWithTitle.First(t => t.Stem == "mikro").Score;
        var scoreWithoutTitle = termsWithoutTitle.First(t => t.Stem == "mikro").Score;

        scoreWithTitle.Should().BeGreaterThan(scoreWithoutTitle);
    }

    [Fact]
    public void ExtractKeyTerms_CokKelimeliIfadeTespiti_IkiKelimeliNgramlariYakalayabilir()
    {
        var text = "Veri tabanı sistemleri dersinde ilişkisel veri tabanı yönetimi anlatılmaktadır. İlişkisel veri tabanı tasarımı kritiktir.";
        var terms = _extractor.ExtractKeyTerms(text);

        terms.Should().Contain(t => t.Term.Contains("veri tabanı") || t.Stem.Contains("veri"));
    }

    [Fact]
    public void ExtractKeyTerms_AyniKokeSahipKavramlar_Tekillestirilir()
    {
        var text = "Programlama temelleri öğrenilirken nesne tabanlı programlama ve fonksiyonel programlama modelleri incelenir.";
        var terms = _extractor.ExtractKeyTerms(text);

        var progTerms = terms.Where(t => t.Stem == "progr").ToList();
        progTerms.Should().HaveCount(1);
    }

    [Fact]
    public void ExtractKeyTerms_BosVeyaKisaMetin_BosListeDoner()
    {
        var emptyTerms = _extractor.ExtractKeyTerms("");
        var whitespaceTerms = _extractor.ExtractKeyTerms("   ");

        emptyTerms.Should().BeEmpty();
        whitespaceTerms.Should().BeEmpty();
    }

    [Fact]
    public void ExtractKeyTerms_KelimeFrekansi_SikGecenKelimeDahaYuksekPuanAlir()
    {
        var text = "Yapay zeka sistemleri geleceği şekillendiriyor. Yapay zeka algoritmaları gelişiyor. Yapay zeka ve makine öğrenimi güçlüdür. Ağaç yapısı kullanılır.";
        var terms = _extractor.ExtractKeyTerms(text);

        var yzTerm = terms.FirstOrDefault(t => t.Stem.Contains("yapay"));
        var agacTerm = terms.FirstOrDefault(t => t.Stem.Contains("ağaç") || t.Stem.Contains("agac"));

        yzTerm.Should().NotBeNull();
        if (agacTerm != null)
        {
            yzTerm!.Score.Should().BeGreaterThan(agacTerm.Score);
        }
    }
}
