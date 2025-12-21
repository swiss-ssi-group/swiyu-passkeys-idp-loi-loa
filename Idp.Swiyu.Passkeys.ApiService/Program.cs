using Duende.AspNetCore.Authentication.JwtBearer.DPoP;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:5001";
        options.Audience = "https://localhost:5001/resources";

        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.ValidateIssuer = true;
        //options.TokenValidationParameters.ValidAudience = "https://localhost:5001/resources";

        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidTypes = ["at+jwt"];
    });

// layers DPoP onto the "token" scheme above
builder.Services.ConfigureDPoPTokensForScheme("Bearer", opt =>
{
    opt.ValidationMode = ExpirationValidationMode.IssuedAt; // IssuedAt is the default.
});

builder.Services.AddAuthorization();

var app = builder.Build();

IdentityModelEventSource.ShowPII = true;
JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseAuthorization();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
