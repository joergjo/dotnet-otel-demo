using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OtelDemo.DiceRoller;

public static class Telemetry
{
    private const string Name = $"{nameof(OtelDemo)}.{nameof(DiceRoller)}";

    public const string ServiceName = "dice-roller";
    public const string ServiceNamespace = "dotnet-otel-demo";
    public static readonly ActivitySource DiceRollActivitySource = new(Name);  
    public static readonly Meter DiceMeter = new Meter(Name, "1.0.0");
    public static readonly Counter<int> DiceRollCounter = DiceMeter.CreateCounter<int>("dice_rolls", description: "Counts the number of dice rolls");
}