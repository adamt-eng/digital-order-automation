using Order_Handler_App.Discord_App;
using System.Threading.Tasks;

namespace Order_Handler_App;

internal class Program
{
    private static async Task Main()
    {
        var app = new DiscordApp();
        await app.StartAsync();
    }
}