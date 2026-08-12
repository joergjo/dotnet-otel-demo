namespace OtelDemo.DiceRoller;

internal static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{player} rolled a {result}")]
    internal static partial void LogDiceRoll(this ILogger logger, string player, int result);
    
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Dice Roller application with {telemetryOption}")]
    internal static partial void LogStartup(this ILogger logger, string telemetryOption);
}