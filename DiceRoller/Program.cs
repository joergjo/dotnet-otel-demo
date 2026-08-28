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
static async Task<int> RollDice(Tracer tracer, string? player)
{
    // ReSharper disable once ExplicitCallerInfoArgument
    using var span = tracer.StartActiveSpan("rolldice", SpanKind.Internal);
    KeyValuePair<string, object?>[] tags = player is { Length: > 0 } ? [new ("player", player)] : [];

    var stopWatch = Stopwatch.StartNew();
    // Simulate work
    await Task.Delay(Random.Shared.Next(1, 100));
    var result = Random.Shared.Next(1, 7);
    span.SetAttribute("dice.result", result);
    DiceRollHistogram.Record(stopWatch.ElapsedMilliseconds, tags: tags);

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
            .AddSource(Name)
            .AddAspNetCoreInstrumentation())
        .WithMetrics(metrics => metrics
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .SetExemplarFilter(ExemplarFilterType.TraceBased)
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
            configure.AddSource(Name);
        });
    builder.Services.ConfigureOpenTelemetryMeterProvider(
        configure => configure.AddMeter(DiceMeter.Name));
}
builder.Services.AddSingleton(TracerProvider.Default.GetTracer(Name));

var app = builder.Build();

app.MapGet("/rolldice/{player?}", async (string? player, [FromServices] Tracer tracer, [FromServices] ILogger<Program> logger) =>
{
    var result = await RollDice(tracer, player);
    DiceRollCounter.Add(1);
    if (Tracer.CurrentSpan.IsRecording)
    {
        if (player is { Length: > 0 })
        {
            Tracer.CurrentSpan.AddEvent($"player {player} rolled a {result}");
            logger.LogDiceRoll(player, result);
        }
        else
        {
            Tracer.CurrentSpan.AddEvent($"anonymous player rolled a {result}");
            logger.LogDiceRoll("anonymous", result);
        }
    }

    return Convert.ToString(result);
});

var telemetryOption = useOtlpExport ? "OTLP Export" : "Azure Monitor Distro";
app.Logger.LogStartup(telemetryOption);
app.Run();
