#define AZURE_MONITOR_OFF

using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Logs;
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
#if AZURE_MONITOR
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName, ServiceNamespace))
    .UseAzureMonitor();
builder.Services.ConfigureOpenTelemetryLoggerProvider(
    configure => configure.AddConsoleExporter());
builder.Services.ConfigureOpenTelemetryTracerProvider(
    configure =>
    {
        configure.AddSource(DiceRollActivitySource.Name);
    });
builder.Services.ConfigureOpenTelemetryMeterProvider(
    configure => configure.AddMeter(DiceMeter.Name));
#else
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName, ServiceNamespace))
    .UseOtlpExporter()
    .WithLogging(logging => logging.AddConsoleExporter())
    .WithTracing(tracing => tracing
        .AddSource(DiceRollActivitySource.Name)
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(DiceMeter.Name)
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel"));
#endif

var app = builder.Build();

app.MapGet("/rolldice/{player?}", (string? player, [FromServices] ILogger<Program> logger) =>
{
    var result = RollDice();
    DiceRollCounter.Add(1);
    if (player is { Length: > 0 })
    {
        Activity.Current?.AddEvent(new ActivityEvent($"player {player} rolled a {result}"));
        logger.LogInformation("{Player} rolled a {Result}", player, result);
    }
    else
    {
        Activity.Current?.AddEvent(new ActivityEvent($"anonymous player rolled a {result}"));
        logger.LogInformation("anonymous player rolled a {Result}", result);
    }

    return Convert.ToString(result);
});

app.Run();