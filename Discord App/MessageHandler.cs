using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using Order_Handler_App.Helpers;
using Order_Handler_App.Services;
using AppContext = Order_Handler_App.Core.AppContext;

namespace Order_Handler_App.Discord_App;

internal class MessageHandler : IDiscordEventHandler
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
            var msgId = socketMessage.Id;
            var channelId = socketMessage.Channel.Id;
            var author = socketMessage.Author.Username;
            var timestamp = DateTime.UtcNow.ToString("u");

            LoggingService.WriteLog($"[RECEIVED] {timestamp} | Msg:{msgId} | Author:{author} | Channel:{channelId}");

            // Filter messages
            if (channelId != AppContext.Configuration.OrdersChannelId)
            {
                return;
            }

            try
            {
                var embed = socketMessage.Embeds.FirstOrDefault();
                if (embed == null)
                {
                    LoggingService.WriteLog($"[WARN] No embed found in message {msgId}.", ConsoleColor.Yellow);
                    return;
                }

                var fields = embed.Fields;
                if (fields.Length < 5)
                {
                    LoggingService.WriteLog($"[WARN] Insufficient embed fields in message {msgId}.", ConsoleColor.Yellow);
                    return;
                }

                var firstField = fields[0];
                var orderId = RegexHelper.OrderIdPattern().Match(firstField.Value).ToString();
                var paymentStatus = firstField.Name;
                var discordUserId = ulong.Parse(fields[4].Value.Trim());

                var guildUser = AppContext.Guild.GetUser(discordUserId);
                if (guildUser == null)
                {
                    LoggingService.WriteLog($"[ERROR] Invalid Discord user ID {discordUserId} for order {orderId}.", ConsoleColor.Red);
                    return;
                }

                if (paymentStatus.Contains("PAID", StringComparison.OrdinalIgnoreCase))
                {
                    await guildUser.AddRoleAsync(AppContext.Configuration.CustomerRoleId).ConfigureAwait(false);
                    LoggingService.WriteLog($"[INFO] Role added for user:{discordUserId} | Order:{orderId} | Status:{paymentStatus}", ConsoleColor.Green);
                }
                else
                {
                    await guildUser.RemoveRoleAsync(AppContext.Configuration.CustomerRoleId).ConfigureAwait(false);
                    LoggingService.WriteLog($"[INFO] Role removed for user:{discordUserId} | Order:{orderId} | Status:{paymentStatus}", ConsoleColor.Cyan);
                }
            }
            catch (FormatException fex)
            {
                LoggingService.WriteLog($"[ERROR] User ID format invalid: {fex.Message}", ConsoleColor.Red);
            }
            catch (Exception ex)
            {
                LoggingService.WriteLog($"[EXCEPTION] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", ConsoleColor.Red);
            }
        });

        return Task.CompletedTask;
    }
}