using SimuladorEmolumentos.Domain;
using SimuladorEmolumentos.Presentation;

namespace SimuladorEmolumentos.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var catalog = new InMemoryRuleCatalog();
        using var form = new SimulationForm();
        var presenter = new SimulationPresenter(form, catalog, new CalculationService(catalog), Diagnose);
        form.Bind(presenter);
        Application.Run(form);
    }

    private static void Diagnose(Exception exception)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimuladorEmolumentos");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "diagnostico.log"),
                $"{DateTimeOffset.Now:O} | {exception.GetType().FullName} | operação de cálculo falhou{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
