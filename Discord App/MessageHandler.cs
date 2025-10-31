using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Order_Handler_App.Core;
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
                if (socketMessage.Channel.Id == AppContext.Configuration.OrdersChannelId)
                {
                    try
                    {
                        string orderId, paymentStatus;

                        var discordUserId = 0UL;

                        if (socketMessage.Author.Id == Convert.ToUInt64(AppContext.Configuration.WebhookUrl.Split('/')[5]))
                        {
                            var embed = socketMessage.Embeds.First();

                            var firstField = embed.Fields[0];
                            orderId = RegexHelper.OrderIdPattern().Match(firstField.Value).ToString();
                            paymentStatus = firstField.Name;

                            // Get User ID
                            {
                                if (!File.Exists(Constants.CorrectedOrdersTxt))
                                {
                                    var create = File.Create(Constants.CorrectedOrdersTxt);
                                    await create.DisposeAsync().ConfigureAwait(false);
                                }

                                // Check if the order was corrected
                                // If it was, get the correct Discord User ID
                                var correctedOrder = false;
                                foreach (var line in (await File.ReadAllLinesAsync(Constants.CorrectedOrdersTxt).ConfigureAwait(false)).Where(line => line.Contains(orderId)))
                                {
                                    discordUserId = Convert.ToUInt64(line.Replace($"{orderId}:", string.Empty));
                                    correctedOrder = true;
                                }

                                if (!correctedOrder)
                                {
                                    discordUserId = Convert.ToUInt64(embed.Fields[3].Value.Trim());
                                }
                            }
                        }
                        else
                        {
                            return;
                        }

                        var guildUser = AppContext.Guild.GetUser(discordUserId);
                        if (guildUser == null)
                        {
                            // Log that the ID was invalid and add a reaction to the message to indicate failure for the admin
                            LoggingService.WriteLog($"Discord User ID Invalid For Order: {orderId}", ConsoleColor.Red);
                            await ((RestUserMessage)await socketMessage.Channel.GetMessageAsync(socketMessage.Id).ConfigureAwait(false)).AddReactionAsync(new Emoji("👎")).ConfigureAwait(false);
                            return;
                        }

                        if (paymentStatus.Contains("PAID"))
                        {
                            // Add 'Customer' role
                            await guildUser.AddRoleAsync(AppContext.Configuration.CustomerRoleId).ConfigureAwait(false);

                            // Confirm order fulfillment by reacting to the order's embed message with a checkmark
                            await socketMessage.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);

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
                        // In-case of exception, inform admin using reaction and write log
                        await ((RestUserMessage)await socketMessage.Channel.GetMessageAsync(socketMessage.Id).ConfigureAwait(false)).AddReactionAsync(new Emoji("🚫")).ConfigureAwait(false);
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