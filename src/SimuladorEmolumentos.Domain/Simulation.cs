namespace SimuladorEmolumentos.Domain;

public sealed record Act(string Id, string Name);
public sealed record CalculationRequest(string State, string ActId, decimal? BaseAmount);
public sealed record CalculationItem(string Description, decimal Amount);
public sealed record CalculationResult(string RuleId, string Formula, string Parameters,
    IReadOnlyList<CalculationItem> Items)
{
    public decimal Total => Items.Sum(item => item.Amount);
}

public sealed class InputException(string message) : Exception(message) { }
public sealed class RuleUnavailableException(string message) : Exception(message) { }

public sealed record CalculationRule(string Id, string State, Act Act, decimal FixedFee, decimal Rate)
{
    public bool RequiresBase => Rate > 0m;
    public string Formula => RequiresBase
        ? $"Base × {Rate * 100m:0.##}% (arredondamento por item, duas casas)."
        : $"Tarifa fixa de {FixedFee.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))}.";

    public CalculationResult Calculate(decimal? baseAmount)
    {
        if (baseAmount < 0m)
            throw new InputException("A base monetária não pode ser negativa.");
        if (RequiresBase && (baseAmount is null || baseAmount <= 0m || baseAmount > 1_000_000_000m))
            throw new InputException("Informe uma base maior que zero e de até R$ 1.000.000.000,00.");
        if (!RequiresBase && baseAmount is not null)
            throw new InputException("Este ato não utiliza base monetária.");
        decimal amount = RequiresBase ? baseAmount!.Value * Rate : FixedFee;
        var item = new CalculationItem(Act.Name, decimal.Round(amount, 2, MidpointRounding.AwayFromZero));
        string parameters = RequiresBase
            ? $"Base: {baseAmount!.Value.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))}"
            : "Nenhum parâmetro adicional.";
        return new CalculationResult(Id, Formula, parameters, Array.AsReadOnly(new[] { item }));
    }
}

public interface IRuleCatalog
{
    IReadOnlyList<CalculationRule> Rules { get; }
    CalculationRule Find(string state, string actId);
}

public sealed class InMemoryRuleCatalog : IRuleCatalog
{
    public IReadOnlyList<CalculationRule> Rules { get; } = Array.AsReadOnly(new[]
    {
        new CalculationRule("DEMO-SP-CERT-1", "SP", new Act("certidao", "Certidão demonstrativa"), 35m, 0m),
        new CalculationRule("DEMO-RJ-CERT-1", "RJ", new Act("certidao", "Certidão demonstrativa"), 42.50m, 0m),
        new CalculationRule("DEMO-SP-ESCR-1", "SP", new Act("escritura", "Escritura demonstrativa"), 0m, 0.01m),
        new CalculationRule("DEMO-RJ-ESCR-1", "RJ", new Act("escritura", "Escritura demonstrativa"), 0m, 0.012m)
    });

    public CalculationRule Find(string state, string actId) =>
        Rules.FirstOrDefault(rule => rule.State == state && rule.Act.Id == actId)
        ?? throw new RuleUnavailableException("Não há regra disponível para este ato e UF.");
}

public interface ICalculationService
{
    CalculationResult Calculate(CalculationRequest request);
}

public sealed class CalculationService(IRuleCatalog catalog) : ICalculationService
{
    public CalculationResult Calculate(CalculationRequest request) =>
        catalog.Find(request.State, request.ActId).Calculate(request.BaseAmount);
}
