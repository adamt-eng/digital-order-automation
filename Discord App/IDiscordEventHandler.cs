using Discord.WebSocket;
using System.Threading.Tasks;

namespace Order_Handler_App.Discord_App;

internal interface IDiscordEventHandler
{
    internal Task Initialize(DiscordSocketClient client);
}