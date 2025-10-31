using System;

namespace Order_Handler_App.src.Core;

internal class EnvironmentConfig
{
    internal string AppToken { get; }
    internal ulong GuildId { get; }
    internal ulong OrdersChannelId { get; }
    internal ulong CustomerRoleId { get; }

    internal EnvironmentConfig()
    {
        AppToken = GetEnv("DISCORD_APP_TOKEN");
        GuildId = GetUlong("GUILD_ID");
        OrdersChannelId = GetUlong("ORDERS_CHANNEL_ID");
        CustomerRoleId = GetUlong("CUSTOMER_ROLE_ID");
    }

    private static string GetEnv(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing environment variable: {key}");
        }

        return value;
    }

    private static ulong GetUlong(string key)
    {
        var value = GetEnv(key);
        if (!ulong.TryParse(value, out var result))
        {
            throw new FormatException($"Invalid ulong value for {key}: {value}");
        }

        return result;
    }
}