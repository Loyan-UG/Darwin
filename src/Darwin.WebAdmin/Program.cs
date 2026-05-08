using Serilog;

// This using brings in our Web-layer composition helpers:
// - AddWebComposition(builder.Configuration) to register MVC, Application, and Persistence services.
// - UseWebStartupAsync() to configure the request pipeline, localization, routing, and run DB migrations+seeding in Development.
using Darwin.WebAdmin.Extensions;
using Darwin.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsEnvironment("Testing") &&
    string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Persistence:Provider"] = "SqlServer",
        ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Darwin_WebAdmin_SmokeTests;Trusted_Connection=True;TrustServerCertificate=True"
    });
}

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["DataProtection:RequireKeyEncryption"] = "false",
        ["Email:Provider"] = "SMTP"
    });
}

// Serilog bootstrap (read from appsettings)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

if (!builder.Environment.IsEnvironment("Testing"))
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .CreateLogger();
}

builder.Host.UseSerilog(); // requires Serilog.AspNetCore


// ----------------------------------------------
// Dependency Injection Composition
// Registers Controllers+Views, FluentValidation integration,
// Application (AutoMapper + validators), and Infrastructure persistence (DbContext + seeder).
// Keep Program.cs slim by pushing all registrations to an extension method.
// ----------------------------------------------
builder.Services.AddWebComposition(builder.Configuration);


var app = builder.Build();

app.UseSerilogRequestLogging(); // middleware


// ----------------------------------------------
// HTTP Pipeline & Routing
// Also applies DB migrations + seeding in Development (inside UseWebStartupAsync).
// Keeps Program.cs minimal; all middleware/routing lives in an extension.
// ----------------------------------------------
await app.UseWebStartupAsync();

app.Run();

/// <summary>
/// Entry point marker used by WebAdmin integration tests with WebApplicationFactory.
/// </summary>
public partial class Program { }
