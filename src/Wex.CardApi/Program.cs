using Microsoft.EntityFrameworkCore;
using Wex.CardApi.Endpoints;
using Wex.CardApi.Infrastructure;
using Wex.CardApi.Services;
using Wex.CardApi.Services.Exchange;

var builder = WebApplication.CreateBuilder(args);

// Persistence
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Treasury exchange-rate provider: typed HttpClient + in-memory cache.
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IExchangeRateProvider, TreasuryExchangeRateProvider>(client =>
{
    var baseUrl = builder.Configuration["Treasury:BaseUrl"]
        ?? "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/";
    client.BaseAddress = new Uri(baseUrl);
});
// NOTE: add resilience here when desired:
//   builder.Services.AddHttpClient<...>().AddStandardResilienceHandler();
//   (package: Microsoft.Extensions.Http.Resilience)

// Application services
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<TransactionService>();

// API explorer + Swagger UI + consistent error bodies
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

// SKELETON ONLY: materialise the schema from the EF model on startup.
// Replaced by an EF Core migration once the model stabilises.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");

app.MapCardEndpoints();
app.MapTransactionEndpoints();

app.Run();

// Exposed so the integration test project can host the app via WebApplicationFactory<Program>.
public partial class Program { }
