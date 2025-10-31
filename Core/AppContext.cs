using Discord.WebSocket;
using Order_Handler_App.Configuration;

namespace Order_Handler_App.Core;

internal class AppContext
{
    internal static SocketGuild Guild;
    internal static readonly ConfigurationManager ConfigurationManager = new("config.json");
    internal static readonly Configuration.Configuration Configuration = ConfigurationManager.Load();
}