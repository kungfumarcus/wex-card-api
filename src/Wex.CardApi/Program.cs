using Microsoft.EntityFrameworkCore;
using Wex.CardApi.Endpoints;
using Wex.CardApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Persistence
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// API explorer + Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Liveness probe
builder.Services.AddHealthChecks();

var app = builder.Build();

// SKELETON ONLY: materialise the schema from the EF model on startup.
// This is intentionally simple for the skeleton; it will be replaced by an
// EF Core migration (dotnet ef migrations add ...) once the model stabilises
// with the currency-conversion features.
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
