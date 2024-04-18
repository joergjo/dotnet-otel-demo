using Microsoft.AspNetCore.Mvc;
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
builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService(ServiceName))
        .AddConsoleExporter();
});
builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(ServiceName))
    .WithTracing(tracing => tracing
        .AddSource(DiceRollActivitySource.Name)   
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddMeter(DiceMeter.Name)
        .AddMeter("Microsoft.AspNetCore.Hosting")
        .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
        .AddOtlpExporter());

var app = builder.Build();

app.MapGet("/rolldice/{player?}", (string? player, [FromServices] ILogger<Program> logger) =>
{
    var result = RollDice();
    DiceRollCounter.Add(1);
    if (player is not null)
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



