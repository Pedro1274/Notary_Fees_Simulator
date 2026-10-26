using System.Globalization;
using SimuladorEmolumentos.Domain;
using SimuladorEmolumentos.Presentation;

namespace SimuladorEmolumentos.WinForms;

public sealed class SimulationForm : Form, ISimulationView
{
    private readonly ComboBox states = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox acts = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox baseInput = new() { Dock = DockStyle.Fill, MaxLength = 30 };
    private readonly Label baseLabel = new() { AutoSize = true };
    private readonly TextBox result = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly Button calculate = new() { Text = "&Calcular", AutoSize = true };
    private readonly Button clear = new() { Text = "&Limpar", AutoSize = true };
    private readonly Button copy = new() { Text = "C&opiar resultado", AutoSize = true, Enabled = false };
    private SimulationPresenter? presenter;
    private bool updating;

    public SimulationForm()
    {
        Text = "SimuladorEmolumentos — Simulador demonstrativo";
        MinimumSize = new Size(660, 620);
        Size = new Size(800, 720);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 11 };
        layout.RowStyles.Clear();
        for (int index = 0; index < 10; index++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Simulação de emolumentos", AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
        layout.Controls.Add(new Label { Text = "DADOS FICTÍCIOS — estimativa demonstrativa, sem validade oficial.", AutoSize = true, ForeColor = Color.DarkRed, Padding = new Padding(0, 8, 0, 8) });
        layout.Controls.Add(new Label { Text = "&UF", AutoSize = true });
        layout.Controls.Add(states);
        layout.Controls.Add(new Label { Text = "&Ato", AutoSize = true });
        layout.Controls.Add(acts);
        layout.Controls.Add(baseLabel);
        layout.Controls.Add(baseInput);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
        buttons.Controls.AddRange(new Control[] { calculate, clear, copy });
        layout.Controls.Add(buttons);
        layout.Controls.Add(new Label { Text = "Sem salvamento: os dados da sessão são descartados ao fechar.
Memória de cálculo e total (selecione o texto para copiar):", AutoSize = true });
        layout.Controls.Add(result);
        Controls.Add(layout);
        states.TabIndex = 0; acts.TabIndex = 1; baseInput.TabIndex = 2;
        calculate.TabIndex = 0; clear.TabIndex = 1; copy.TabIndex = 2;
        result.TabIndex = 10;
        baseInput.AccessibleName = "Base monetária em reais";
        result.AccessibleName = "Memória de cálculo e total";
        AcceptButton = calculate;
        states.SelectedIndexChanged += (_, _) => { if (!updating) presenter?.StateChanged(); };
        acts.SelectedIndexChanged += (_, _) => { if (!updating) presenter?.ActChanged(); };
        baseInput.TextChanged += (_, _) => presenter?.InputsChanged();
        calculate.Click += (_, _) => presenter?.Calculate();
        clear.Click += (_, _) => presenter?.Clear();
        copy.Click += (_, _) =>
        {
            try { Clipboard.SetText(result.Text); }
            catch (System.Runtime.InteropServices.ExternalException)
            { ShowError("Cópia indisponível", "Selecione o resultado e use Ctrl+C para copiar."); }
        };
    }

    public void Bind(SimulationPresenter value) { presenter = value; presenter.Initialize(); }
    public string SelectedState => states.SelectedItem as string ?? "";
    public string SelectedActId => (acts.SelectedItem as Act)?.Id ?? "";
    public string BaseText => baseInput.Text;
    public void SetStates(IReadOnlyList<string> values)
    {
        updating = true;
        try { states.DataSource = values.ToList(); }
        finally { updating = false; }
    }
    public void SetActs(IReadOnlyList<Act> values)
    {
        updating = true;
        try { acts.DisplayMember = nameof(Act.Name); acts.DataSource = values.ToList(); }
        finally { updating = false; }
    }
    public void SetBaseRequired(bool required)
    {
        baseInput.Enabled = required;
        baseLabel.Text = required ? "&Base em reais (obrigatória; maior que zero, até 1 bilhão; ex.: 10000,00)" : "Base monetária: não necessária para este ato";
    }
    public void ClearBase() => baseInput.Clear();
    public void ClearResult() { result.Clear(); copy.Enabled = false; }
    public void ShowResult(CalculationResult value)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        result.Text = string.Join(Environment.NewLine, new[]
        {
            "SIMULAÇÃO DEMONSTRATIVA — DADOS FICTÍCIOS",
            $"UF: {SelectedState} | Ato: {acts.Text}",
            $"Regra: {value.RuleId}", value.Parameters, $"Fórmula: {value.Formula}", ""
        }.Concat(value.Items.Select(item => $"{item.Description}: {item.Amount.ToString("C2", culture)}"))
        .Concat(new[] { "", $"TOTAL: {value.Total.ToString("C2", culture)}" }));
        copy.Enabled = true;
    }
    public void ShowError(string title, string message) => MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    public void SetBusy(bool busy) { calculate.Enabled = !busy; clear.Enabled = !busy; UseWaitCursor = busy; }
}
