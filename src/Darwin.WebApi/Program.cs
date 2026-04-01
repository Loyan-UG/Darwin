using Darwin.WebApi.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Keep Program.cs minimal by delegating registration to a composition extension.
builder.Services.AddWebApiComposition(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline, apply dev DB bootstrap, and map controllers.
await app.UseWebApiStartupAsync();

await app.RunAsync();



/// <summary>
/// Entry point marker used by integration tests with WebApplicationFactory.
/// </summary>
public partial class Program { }
