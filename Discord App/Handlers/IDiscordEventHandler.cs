using System.Threading.Tasks;
using Discord.WebSocket;

namespace Order_Handler_App.Discord_App.Handlers;

internal interface IDiscordEventHandler
{
    internal Task Initialize(DiscordSocketClient client);
}