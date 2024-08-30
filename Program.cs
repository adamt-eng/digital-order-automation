using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using Order_Handler_Bot.Configuration;

namespace Order_Handler_Bot;

internal partial class Program
{
    [GeneratedRegex("[0-9A-Z]{5}")]
    private static partial Regex OrderID_Regex();

    private static SocketGuild _guild;
    private static readonly DiscordSocketClient Client = new(new DiscordSocketConfig
    {
        AlwaysDownloadUsers = true,
        GatewayIntents = GatewayIntents.GuildMessageReactions | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.Guilds | GatewayIntents.GuildMembers
    });

    private static readonly ConfigurationManager ConfigurationManager = new("config.json");
    private static readonly Configuration.Configuration Configuration = ConfigurationManager.Load();

    private static async Task Main()
    {
        const string correctedOrdersTxt = "Corrected Orders.txt";

        var webhookUrl = Configuration.WebhookUrl;
        var customerRoleId = Configuration.CustomerRoleId;

        Console.Title = "Order Handler";

        Client.Log += message =>
        {
            WriteLog(message.Message, ConsoleColor.Gray);
            return Task.CompletedTask;
        };

        Client.MessageReceived += async message =>
        {
            if (message.Channel.Id == Configuration.OrdersChannelId)
            {
                try
                {
                    string orderId, paymentStatus;

                    var discordUserId = new ulong();

                    // A message from the admin is to correct an order that has an invalid Discord User ID
                    // The message should contain a Discord User ID and should be a reply to the message containing the order's embed

                    if (message.Author.Id == Configuration.AdminId)
                    {
                        // Get the order's embed
                        var embed = message.Channel.GetMessageAsync(message.Reference.MessageId.Value).Result.Embeds.First();

                        // Read order data
                        var firstField = embed.Fields[0];
                        paymentStatus = firstField.Name;
                        orderId = OrderID_Regex().Match(firstField.Value).ToString();

                        discordUserId = Convert.ToUInt64(message.CleanContent);

                        var embedBuilder = embed.ToEmbedBuilder();

                        // Update Discord User ID with the one the admin sent
                        WriteLog($"Corrected Order: {embedBuilder.Fields[3].Value} -> {discordUserId}", ConsoleColor.Green);

                        embedBuilder.Fields[3].Value = discordUserId;

                        // Update order's embed
                        await new DiscordWebhookClient(webhookUrl).ModifyMessageAsync((ulong)message.Reference.MessageId, properties => { properties.Embeds = new[] { embedBuilder.Build() }; }).ConfigureAwait(false);

                        // Add orderId and updated discordUserId to "Corrected Orders.txt"
                        // This is needed because we only corrected the Discord User ID on the file database
                        // And so if the user refunds, the notification would be sent with the incorrect Discord User ID
                        // But since we corrected the order and saved it in the "Corrected Orders.txt" file
                        // We can now check first if the order is in "Corrected Orders.txt" before proceeding
                        await File.AppendAllTextAsync(correctedOrdersTxt, $"\n{orderId}:{discordUserId}").ConfigureAwait(false);
                    }
                    else if (message.Author.Id == Convert.ToUInt64(webhookUrl.Split('/')[5]))
                    {
                        var embed = message.Embeds.First();

                        var firstField = embed.Fields[0];
                        orderId = OrderID_Regex().Match(firstField.Value).ToString();
                        paymentStatus = firstField.Name;

                        // Get User ID
                        {
                            if (!File.Exists(correctedOrdersTxt))
                            {
                                var create = File.Create(correctedOrdersTxt);
                                await create.DisposeAsync().ConfigureAwait(false);
                            }

                            // Check if the order was corrected
                            // If it was, get the correct Discord User ID
                            var correctedOrder = false;
                            foreach (var line in (await File.ReadAllLinesAsync(correctedOrdersTxt).ConfigureAwait(false)).Where(line => line.Contains(orderId)))
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

                    var guildUser = _guild.GetUser(discordUserId);
                    if (guildUser == null)
                    {
                        // Log that the ID was invalid and add a reaction to the message to indicate failure for the admin
                        WriteLog($"Discord User ID Invalid For Order: {orderId}", ConsoleColor.Red);
                        await ((RestUserMessage)await message.Channel.GetMessageAsync(message.Id).ConfigureAwait(false)).AddReactionAsync(new Emoji("👎")).ConfigureAwait(false);
                        return;
                    }

                    if (paymentStatus.Contains("PAID"))
                    {
                        // Add 'Customer' role
                        await guildUser.AddRoleAsync(customerRoleId).ConfigureAwait(false);

                        // Confirm order fulfillment by reacting to the order's embed message with a checkmark
                        await message.AddReactionAsync(new Emoji("✅")).ConfigureAwait(false);

                        WriteLog($"Order Fulfilled: {orderId}", ConsoleColor.Green);
                    }
                    else
                    {
                        // Remove 'Customer' role
                        await guildUser.RemoveRoleAsync(customerRoleId).ConfigureAwait(false);

                        WriteLog($"Removed Customer Role: {discordUserId} ({orderId})", ConsoleColor.Green);
                    }
                }
                catch (Exception exception)
                {
                    // In-case of exception, inform admin using reaction and write log
                    await ((RestUserMessage)await message.Channel.GetMessageAsync(message.Id).ConfigureAwait(false)).AddReactionAsync(new Emoji("🚫")).ConfigureAwait(false);
                    WriteLog($"Exception: {exception}", ConsoleColor.Red);
                }
            }
        };

        Client.Ready += ClientReady;

        await Client.LoginAsync(TokenType.Bot, Configuration.BotToken).ConfigureAwait(false);
        await Client.StartAsync().ConfigureAwait(false);
        await Task.Delay(-1).ConfigureAwait(false);

        return;

        static Task ClientReady()
        {
            _guild = Client.GetGuild(Configuration.GuildId);
            return Task.CompletedTask;
        }
    }

    internal static void WriteLog(string log, ConsoleColor consoleColor)
    {
        log = $"{DateTime.Now:dd/MM/yyyy HH:mm:ss} {log}\n";

        Console.ForegroundColor = consoleColor;
        Console.Write(log);
        Console.ResetColor();

        File.AppendAllText("Logs.txt", log);
    }
}