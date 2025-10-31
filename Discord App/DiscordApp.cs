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

    internal async Task StartAsync()
    {
        Client.Log += message =>
        {
            _ = Task.Run(() =>
            {
                LoggingService.WriteLog(message.Message);
                return Task.CompletedTask;
            });
            return Task.CompletedTask;
        };

        await new MessageHandler().Initialize(Client);

        Client.Ready += ClientReady;
        await Client.LoginAsync(TokenType.Bot, AppContext.Configuration.WebhookUrl).ConfigureAwait(false);
        await Client.StartAsync();
        await Task.Delay(-1);
    }

    private static Task ClientReady()
    {
        AppContext.Guild = Client.GetGuild(AppContext.Configuration.GuildId);
        return Task.CompletedTask;
    }
}