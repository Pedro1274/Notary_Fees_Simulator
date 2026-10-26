using SimuladorEmolumentos.Domain;
using Xunit;

namespace SimuladorEmolumentos.Domain.Tests;

public sealed class CalculationTests
{
    private readonly CalculationService service = new(new InMemoryRuleCatalog());

    [Theory]
    [InlineData("SP", "certidao", null, "35.00")]
    [InlineData("RJ", "certidao", null, "42.50")]
    [InlineData("SP", "escritura", "10000", "100.00")]
    [InlineData("RJ", "escritura", "10000", "120.00")]
    [InlineData("SP", "escritura", "100.50", "1.01")]
    [InlineData("RJ", "escritura", "100.50", "1.21")]
    public void Calculate_CombinacaoCadastrada_TotalEsperado(string state, string act, string? input, string expected)
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        var request = new CalculationRequest(state, act, input is null ? null : decimal.Parse(input, culture));
        var result = service.Calculate(request);
        Assert.Equal(decimal.Parse(expected, culture), result.Total);
        Assert.Equal(result.Items.Sum(item => item.Amount), result.Total);
        Assert.NotEmpty(result.RuleId);
        Assert.NotEmpty(result.Formula);
        Assert.NotEmpty(result.Parameters);
        Assert.Equal(result.Total, service.Calculate(request).Total);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("1000000000.01")]
    public void Calculate_BaseInvalida_Rejeita(string? input)
    {
        decimal? amount = input is null ? null : decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);
        Assert.Throws<InputException>(() => service.Calculate(new("SP", "escritura", amount)));
    }

    [Fact]
    public void Calculate_LimiteMaximo_Aceita() =>
        Assert.Equal(10_000_000m, service.Calculate(new("SP", "escritura", 1_000_000_000m)).Total);

    [Theory]
    [InlineData("MG", "certidao")]
    [InlineData("SP", "inexistente")]
    public void Calculate_CombinacaoSemSuporte_Rejeita(string state, string act) =>
        Assert.Throws<RuleUnavailableException>(() => service.Calculate(new(state, act, null)));

    [Fact]
    public void Calculate_ParametroEmRegraFixa_Rejeita() =>
        Assert.Throws<InputException>(() => service.Calculate(new("SP", "certidao", 10m)));
}
