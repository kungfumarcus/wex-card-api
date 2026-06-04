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
    })
    // Retry (with backoff + jitter), total + per-attempt timeouts, circuit breaker.
    .AddStandardResilienceHandler();

// Application services
builder.Services.AddScoped<CardService>();
builder.Services.AddScoped<TransactionService>();

// API explorer + Swagger UI + consistent error bodies
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply EF Core migrations on startup so the schema is present and versioned.
// (For production you may prefer running migrations as a separate deploy step.)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Serve the static console UI from wwwroot (index.html at "/").
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();

app.MapHealthChecks("/health");

app.MapCardEndpoints();
app.MapTransactionEndpoints();
app.MapCurrencyEndpoints();

app.Run();

// Exposed so the integration test project can host the app via WebApplicationFactory<Program>.
public partial class Program { }
