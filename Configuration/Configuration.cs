namespace Order_Handler_App.Configuration;

public class Configuration
{
    public string BotToken { get; set; }
    public string WebhookUrl { get; set; }
    public ulong GuildId { get; set; }
    public ulong OrdersChannelId { get; set; }
    public ulong AdminId { get; set; }
    public ulong CustomerRoleId { get; set; }
}