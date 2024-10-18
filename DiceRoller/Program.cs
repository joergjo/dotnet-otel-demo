using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static OtelDemo.DiceRoller.Telemetry;

// ReSharper disable once MoveLocalFunctionAfterJumpStatement
int RollDice()
{
    // ReSharper disable once ExplicitCallerInfoArgument
    using var activity = DiceRollActivitySource.StartActivity("rolldice");
    var result = Random.Shared.Next(1, 7);
    activity?.SetTag("result", result);
    return result;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName))
    .UseOtlpExporter()
    .WithLogging()
    .WithTracing(tracing => tracing
        .AddSource(DiceRollActivitySource.Name)   
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(DiceMeter.Name)
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel"));

var app = builder.Build();

app.MapGet("/rolldice/{player?}", (string? player, [FromServices] ILogger<Program> logger) =>
{
    var result = RollDice();
    DiceRollCounter.Add(1);
    if (player is { Length: > 0 })
    {
        logger.LogInformation("{Player} rolled a {Result}", player, result);
    }
    else
    {
        logger.LogInformation("anonymous player rolled a {Result}", result);
    }
    return Convert.ToString(result);
});

app.Run();
