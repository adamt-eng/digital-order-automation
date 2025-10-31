using Discord.WebSocket;

namespace Order_Handler_App.src.Core;

internal static class AppContext
{
    internal static SocketGuild Guild;
    internal static EnvironmentConfig Config { get; } = new();
}