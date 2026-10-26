using System.Globalization;
using SimuladorEmolumentos.Domain;

namespace SimuladorEmolumentos.Presentation;

public interface ISimulationView
{
    string SelectedState { get; }
    string SelectedActId { get; }
    string BaseText { get; }
    void SetStates(IReadOnlyList<string> states);
    void SetActs(IReadOnlyList<Act> acts);
    void SetBaseRequired(bool required);
    void ClearBase();
    void ClearResult();
    void ShowResult(CalculationResult result);
    void ShowError(string title, string message);
    void SetBusy(bool busy);
}

public sealed class SimulationPresenter(ISimulationView view, IRuleCatalog catalog,
    ICalculationService service, Action<Exception> diagnose)
{
    private bool busy;
    public void Initialize()
    {
        view.SetStates(catalog.Rules.Select(rule => rule.State).Distinct().Order().ToArray());
        StateChanged();
    }

    public void StateChanged()
    {
        view.ClearResult();
        view.SetActs(catalog.Rules.Where(rule => rule.State == view.SelectedState)
            .Select(rule => rule.Act).Distinct().ToArray());
        ActChanged();
    }

    public void ActChanged()
    {
        view.ClearResult();
        view.ClearBase();
        view.SetBaseRequired(catalog.Rules.Any(rule => rule.State == view.SelectedState
            && rule.Act.Id == view.SelectedActId && rule.RequiresBase));
    }

    public void InputsChanged() => view.ClearResult();

    public void Clear()
    {
        view.ClearBase();
        Initialize();
        view.ClearResult();
    }

    public void Calculate()
    {
        if (busy) return;
        busy = true;
        view.SetBusy(true);
        view.ClearResult();
        try
        {
            var rule = catalog.Find(view.SelectedState, view.SelectedActId);
            decimal? baseAmount = null;
            if (rule.RequiresBase)
            {
                // Não aceitar agrupadores: evita interpretar ponto decimal como milhar.
                if (!decimal.TryParse(view.BaseText.Trim(), NumberStyles.AllowLeadingSign |
                    NumberStyles.AllowDecimalPoint, CultureInfo.GetCultureInfo("pt-BR"), out var parsed))
                    throw new InputException("Informe a base em reais com vírgula decimal, sem separador de milhar (ex.: 10000,00).");
                baseAmount = parsed;
            }
            view.ShowResult(service.Calculate(new(view.SelectedState, view.SelectedActId, baseAmount)));
        }
        catch (InputException exception) { view.ShowError("Confira os campos", exception.Message); }
        catch (RuleUnavailableException exception) { view.ShowError("Regra indisponível", exception.Message); }
        catch (Exception exception)
        {
            diagnose(exception);
            view.ShowError("Falha técnica", "Não foi possível calcular. Tente novamente. Consulte o diagnóstico local se o erro persistir.");
        }
        finally { busy = false; view.SetBusy(false); }
    }
}
