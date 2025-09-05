// This using brings in our Web-layer composition helpers:
// - AddWebComposition(builder.Configuration) to register MVC, Application, and Persistence services.
// - UseWebStartupAsync() to configure the request pipeline, localization, routing, and run DB migrations+seeding in Development.
using Darwin.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------
// Dependency Injection Composition
// Registers Controllers+Views, FluentValidation integration,
// Application (AutoMapper + validators), and Infrastructure persistence (DbContext + seeder).
// Keep Program.cs slim by pushing all registrations to an extension method.
// ----------------------------------------------
builder.Services.AddWebComposition(builder.Configuration);

var app = builder.Build();

// ----------------------------------------------
// HTTP Pipeline & Routing
// Also applies DB migrations + seeding in Development (inside UseWebStartupAsync).
// Keeps Program.cs minimal; all middleware/routing lives in an extension.
// ----------------------------------------------
await app.UseWebStartupAsync();

app.Run();
