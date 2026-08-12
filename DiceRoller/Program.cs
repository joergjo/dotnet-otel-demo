using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OtelDemo.DiceRoller;
using static OtelDemo.DiceRoller.Telemetry;

// ReSharper disable once MoveLocalFunctionAfterJumpStatement
static int RollDice()
{
    // ReSharper disable once ExplicitCallerInfoArgument
    using var activity = DiceRollActivitySource.StartActivity("rolldice");
    var result = Random.Shared.Next(1, 7);
    activity?.SetTag("result", result);
    return result;
}

var builder = WebApplication.CreateBuilder(args);
var useOtlpExport = builder.Configuration.GetValue("UseOtlpExport", true);

if (useOtlpExport)
{
     builder.Logging.AddOpenTelemetry(logging =>
     {
         logging.IncludeFormattedMessage = true;
         logging.IncludeScopes = true;
         logging.ParseStateValues = true;
     });
    builder.Services
        .AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(ServiceName, ServiceNamespace))
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
}
else
{
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
}

var app = builder.Build();

app.MapGet("/rolldice/{player?}", (string? player, [FromServices] ILogger<Program> logger) =>
{
    var result = RollDice();
    DiceRollCounter.Add(1);
    if (player is { Length: > 0 })
    {
        Activity.Current?.AddEvent(new ActivityEvent($"player {player} rolled a {result}"));
        logger.LogDiceRoll(player, result);
    }
    else
    {
        Activity.Current?.AddEvent(new ActivityEvent($"anonymous player rolled a {result}"));
        logger.LogDiceRoll("anonymous player", result);
    }

    return Convert.ToString(result);
});

var telemetryOption = useOtlpExport ? "OTLP Export" : "Azure Monitor Distro";
app.Logger.LogStartup(telemetryOption);
app.Run();