using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Order_Handler_App.Services;
using AppContext = Order_Handler_App.Core.AppContext;

namespace Order_Handler_App.Discord_App;

internal class DiscordApp
{
    private static readonly DiscordSocketClient Client = new(new DiscordSocketConfig
    {
        AlwaysDownloadUsers = true,
        GatewayIntents = GatewayIntents.GuildMessageReactions |
                         GatewayIntents.GuildMessages |
                         GatewayIntents.MessageContent |
                         GatewayIntents.Guilds |
                         GatewayIntents.GuildMembers
    });

    internal static async Task StartAsync()
    {
        Client.Log += OnClientOnLog;

        await new MessageHandler().Initialize(Client);

        Client.Ready += ClientReady;

        await Client.LoginAsync(TokenType.Bot, AppContext.Configuration.BotToken).ConfigureAwait(false);
        await Client.StartAsync();
        await Task.Delay(-1);
    }

    private static Task OnClientOnLog(LogMessage message)
    {
        LoggingService.WriteLog(message.Message);
        return Task.CompletedTask;
    }

    private static Task ClientReady()
    {
        AppContext.Guild = Client.GetGuild(AppContext.Configuration.GuildId);
        return Task.CompletedTask;
    }
}