using FluentAssertions;
using SesliAsistan.Business.Speech;
using Xunit;

namespace SesliAsistan.Tests;

public class DefinitionPatternTests
{
    private readonly DefinitionMatcher _matcher = new();

    [Fact]
    public void FindDefinition_CopulaVariants_DirDurDurTurKaliplariniYakalayabilir()
    {
        var text = "Mikroişlemci, bilgisayarın tüm aritmetik ve mantıksal işlemlerini yürüten merkezi birimdir. Diğer donanımlar buna bağlıdır.";
        var def = _matcher.FindDefinition("Mikroişlemci", text);

        def.Should().NotBeNull();
        def.Should().Contain("merkezi birimdir");
    }

    [Fact]
    public void FindDefinition_DenirKalibi_TanimCumlesiniBulur()
    {
        var text = "Verilerin bellekte belirli bir hiyerarşide tutulması yöntemine veri yapısı denir. Bu yapılar algoritmaların temelini oluşturur.";
        var def = _matcher.FindDefinition("veri yapısı", text);

        def.Should().NotBeNull();
        def.Should().Contain("veri yapısı denir");
    }

    [Fact]
    public void FindDefinition_ColonKalibi_IkiNoktaUstUsteFormatiniYakalayabilir()
    {
        var text = "Normalizasyon: Veritabanında veri tekrarını önlemek ve veri bütünlüğünü sağlamak için uygulanan tasarım kuralıdır.";
        var def = _matcher.FindDefinition("Normalizasyon", text);

        def.Should().NotBeNull();
        def.Should().Contain("veri tekrarını önlemek");
    }

    [Fact]
    public void FindDefinition_AdlandirilirKalibi_TanimCumlesiniBulur()
    {
        var text = "Bilgisayar ağlarında veri paketlerinin yönlendirilmesini sağlayan cihazlar yönlendirici olarak adlandırılır. Paket iletiminde önemli role sahiptir.";
        var def = _matcher.FindDefinition("yönlendirici", text);

        def.Should().NotBeNull();
        def.Should().Contain("yönlendirici olarak adlandırılır");
    }

    [Fact]
    public void FindDefinition_KalipBulunamadiginda_EnUygunCumleyeDuser()
    {
        // Özel kalıp yok ama terim geçiyor
        var text = "İşletim sistemi tüm donanım kaynaklarını yönetir ve kullanıcıya bir arayüz sağlar. Bilgisayarlar bu yazılım olmadan kullanılamaz.";
        var def = _matcher.FindDefinition("İşletim sistemi", text);

        def.Should().NotBeNull();
        def.Should().Contain("donanım kaynaklarını yönetir");
    }

    [Fact]
    public void FindDefinition_TerimMetindeYoksaVeyaBosMetinse_NullDoner()
    {
        var nullResult = _matcher.FindDefinition("Kuantum", "Bu metinde sadece klasik fizik konuları anlatılmaktadır.");
        var emptyTextResult = _matcher.FindDefinition("Algoritma", "");
        var emptyTermResult = _matcher.FindDefinition("", "Herhangi bir metin.");

        nullResult.Should().BeNull();
        emptyTextResult.Should().BeNull();
        emptyTermResult.Should().BeNull();
    }
}
