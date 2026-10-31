using SimuladorEmolumentos.Domain;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IRuleCatalog, InMemoryRuleCatalog>();
builder.Services.AddSingleton<ICalculationService, CalculationService>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();
app.MapGet("/api/catalog", (IRuleCatalog catalog) => catalog.Rules.Select(rule => new
{
    rule.State, ActId = rule.Act.Id, ActName = rule.Act.Name, rule.RequiresBase
}));
app.MapPost("/api/simulations", (CalculationRequest request, ICalculationService service) =>
{
    try { return Results.Ok(service.Calculate(request)); }
    catch (InputException exception)
    { return Results.Problem(detail: exception.Message, statusCode: 400, title: "Confira os campos"); }
    catch (RuleUnavailableException exception)
    { return Results.Problem(detail: exception.Message, statusCode: 400, title: "Regra indisponível"); }
});
app.Run();
