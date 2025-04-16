// "Responsive Website Preview Generator"
using Microsoft.Extensions.FileProviders;
using shotweb;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Explicitly load appsettings.json
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory()) // Ensure correct path
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

var app = builder.Build();

app.UseCors(); // Must be before app.MapGet()
app.UseWebSockets();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var googleApiKey = builder.Configuration["GoogleApiKey"]!;
ShotWebRouteBuilder.AddStaticImageRoutes(app, googleApiKey);

app.Run();
