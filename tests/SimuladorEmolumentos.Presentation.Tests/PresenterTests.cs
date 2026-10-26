using SimuladorEmolumentos.Domain;
using SimuladorEmolumentos.Presentation;
using Xunit;

namespace SimuladorEmolumentos.Presentation.Tests;

public sealed class PresenterTests
{
    private readonly FakeView view = new();
    private readonly InMemoryRuleCatalog catalog = new();
    private SimulationPresenter Create(ICalculationService? service = null, Action<Exception>? diagnose = null) =>
        new(view, catalog, service ?? new CalculationService(catalog), diagnose ?? (_ => { }));

    [Fact]
    public void Calculate_BaseValida_ExibeResultado()
    {
        var presenter = Create();
        presenter.Calculate();
        Assert.Equal(100m, view.Result!.Total);
        Assert.Null(view.Error);
        Assert.False(view.Busy);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("10.00")]
    [InlineData("1.000,00")]
    [InlineData("1000000000,01")]
    public void Calculate_EntradaInvalida_LimpaResultadoEExibeErro(string input)
    {
        var presenter = Create(); presenter.Calculate();
        view.BaseText = input; presenter.Calculate();
        Assert.Null(view.Result);
        Assert.Equal("Confira os campos", view.Error);
        Assert.False(view.Busy);
    }

    [Fact]
    public void Clear_AposCalculo_RestauraSelecaoELimpa()
    {
        var presenter = Create(); presenter.Calculate(); presenter.Clear();
        Assert.Null(view.Result); Assert.Equal("", view.BaseText);
        Assert.Equal("certidao", view.SelectedActId);
        Assert.False(view.BaseRequired);
    }

    [Fact]
    public void InputsChanged_AposCalculo_InvalidaResultado()
    {
        var presenter = Create(); presenter.Calculate(); presenter.InputsChanged();
        Assert.Null(view.Result);
    }

    [Fact]
    public void StateChanged_NovaUf_AtualizaAtosEInvalida()
    {
        var presenter = Create(); presenter.Calculate();
        view.SelectedState = "RJ"; presenter.StateChanged(); presenter.Calculate();
        Assert.Equal(42.50m, view.Result!.Total);
        Assert.False(view.BaseRequired);
    }

    [Fact]
    public void ActChanged_NovoAto_InvalidaELimpaBase()
    {
        var presenter = Create(); presenter.Calculate(); presenter.ActChanged();
        Assert.Null(view.Result); Assert.Equal("", view.BaseText); Assert.True(view.BaseRequired);
    }

    [Fact]
    public void Calculate_UfSemSuporte_ExibeErroEspecifico()
    {
        view.SelectedState = "MG"; Create().Calculate();
        Assert.Equal("Regra indisponível", view.Error); Assert.Null(view.Result);
    }

    [Fact]
    public void Calculate_FalhaTecnica_RegistraDiagnosticoELiberaView()
    {
        Exception? logged = null;
        Create(new FailingService(), exception => logged = exception).Calculate();
        Assert.NotNull(logged); Assert.Equal("Falha técnica", view.Error);
        Assert.Null(view.Result); Assert.False(view.Busy);
    }

    private sealed class FailingService : ICalculationService
    {
        public CalculationResult Calculate(CalculationRequest request) => throw new InvalidOperationException();
    }

    private sealed class FakeView : ISimulationView
    {
        public string SelectedState { get; set; } = "SP";
        public string SelectedActId { get; set; } = "escritura";
        public string BaseText { get; set; } = "10000,00";
        public CalculationResult? Result { get; private set; }
        public string? Error { get; private set; }
        public bool Busy { get; private set; }
        public bool BaseRequired { get; private set; }
        public void SetStates(IReadOnlyList<string> states) => SelectedState = states[0];
        public void SetActs(IReadOnlyList<Act> acts) => SelectedActId = acts[0].Id;
        public void SetBaseRequired(bool required) => BaseRequired = required;
        public void ClearBase() => BaseText = "";
        public void ClearResult() => Result = null;
        public void ShowResult(CalculationResult result) => Result = result;
        public void ShowError(string title, string message) => Error = title;
        public void SetBusy(bool busy) => Busy = busy;
    }
}
