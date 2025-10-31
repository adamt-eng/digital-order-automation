using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using Order_Handler_App.Helpers;
using Order_Handler_App.Services;
using AppContext = Order_Handler_App.Core.AppContext;

namespace Order_Handler_App.Discord_App;

internal class MessageHandler
{
    public Task Initialize(DiscordSocketClient client)
    {
        client.MessageReceived += HandleMessageAsync;
        return Task.CompletedTask;
    }

    private static Task HandleMessageAsync(SocketMessage socketMessage)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Only filter to messages sent in the #orders channel
                if (socketMessage.Channel.Id == AppContext.Configuration.OrdersChannelId)
                {
                    try
                    {
                        var embed = socketMessage.Embeds.First();
                        var fields = embed.Fields;
                        var firstField = fields[0];
                        
                        var orderId = RegexHelper.OrderIdPattern().Match(firstField.Value).ToString();
                        var paymentStatus = firstField.Name;

                        var discordUserId = ulong.Parse(fields[4].Value.Trim());
                        
                        var guildUser = AppContext.Guild.GetUser(discordUserId);
                        if (guildUser == null)
                        {
                            LoggingService.WriteLog($"Discord User ID Invalid For Order: {orderId}", ConsoleColor.Red);
                            return;
                        }

                        if (paymentStatus.Contains("PAID"))
                        {
                            // Add 'Customer' role
                            await guildUser.AddRoleAsync(AppContext.Configuration.CustomerRoleId).ConfigureAwait(false);

                            LoggingService.WriteLog($"Order Fulfilled: {orderId}", ConsoleColor.Green);
                        }
                        else
                        {
                            // Remove 'Customer' role
                            await guildUser.RemoveRoleAsync(AppContext.Configuration.CustomerRoleId).ConfigureAwait(false);

                            LoggingService.WriteLog($"Removed Customer Role: {discordUserId} ({orderId})", ConsoleColor.Green);
                        }
                    }
                    catch (Exception exception)
                    {
                        LoggingService.WriteLog($"Exception: {exception}", ConsoleColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.WriteLog($"MessageReceived Exception: {ex}");
            }
        });
        return Task.CompletedTask;
    }
}