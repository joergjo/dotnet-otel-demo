#define USE_OTLP_EXPORTER

using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static OtelDemo.DiceRoller.Telemetry;

// ReSharper disable once MoveLocalFunctionAfterJumpStatement
int RollDice(Tracer tracer)
{
    // ReSharper disable once ExplicitCallerInfoArgument
    using var span = tracer.StartActiveSpan("rolldice", SpanKind.Internal);
    var result = Random.Shared.Next(1, 7);
    span.SetAttribute("dice.result", result);
    return result;
}

var builder = WebApplication.CreateBuilder(args);
#if USE_OTLP_EXPORTER
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName, ServiceNamespace))
    .UseOtlpExporter()
    .WithLogging(logging => logging.AddConsoleExporter())
    .WithTracing(tracing => tracing
        .AddSource(Name)
        .AddAspNetCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(DiceMeter.Name)
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel"));
#else
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

#endif
builder.Services.AddSingleton(TracerProvider.Default.GetTracer(Name));

var app = builder.Build();

app.MapGet("/rolldice/{player?}", (string? player, [FromServices] Tracer tracer, [FromServices] ILogger<Program> logger) =>
{
    var result = RollDice(tracer);
    DiceRollCounter.Add(1);
    if (Tracer.CurrentSpan.IsRecording)
    {
        if (player is { Length: > 0 })
        {
            Tracer.CurrentSpan.AddEvent($"player {player} rolled a {result}");
            logger.LogInformation("{Player} rolled a {Result}", player, result);
        }
        else
        {
            Tracer.CurrentSpan.AddEvent($"anonymous player rolled a {result}");
            logger.LogInformation("anonymous player rolled a {Result}", result);
        }
    }

    return Convert.ToString(result);
});

app.Run();
