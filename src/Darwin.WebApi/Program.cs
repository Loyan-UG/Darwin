using Darwin.WebApi.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Keep Program.cs minimal by delegating registration to a composition extension.
builder.Services.AddWebApiComposition(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline and map controllers.
app.UseWebApiStartup();

await app.RunAsync();


