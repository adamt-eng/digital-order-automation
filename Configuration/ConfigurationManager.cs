using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Order_Handler_Bot.Configuration;

public class ConfigurationManager(string configFilePath)
{
    private readonly JsonSerializerSettings _jsonSettings = new() { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented };

    public Configuration Load()
    {
        if (!File.Exists(configFilePath))
        {
            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(new Configuration(), _jsonSettings));
            Program.WriteLog($"Please fill in the required settings in {configFilePath}.", ConsoleColor.Red);
            Program.WriteLog("Press enter to exit.", ConsoleColor.Yellow);
            Console.Read();
            Environment.Exit(1);
        }
        else
        {
            var configuration = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configFilePath), _jsonSettings);
            if (string.IsNullOrEmpty(configuration.BotToken) ||
                string.IsNullOrEmpty(configuration.WebhookUrl) ||
                configuration.GuildId == 0 ||
                configuration.OrdersChannelId == 0 ||
                configuration.AdminId == 0 ||
                configuration.CustomerRoleId == 0)
            {
                Program.WriteLog($"One or more properties in {configFilePath} are not set properly.", ConsoleColor.Red);
                Program.WriteLog("Press enter to exit.", ConsoleColor.Yellow);
                Console.Read();
                Environment.Exit(1);
            }
            else
            {
                return configuration;
            }
        }

        throw new InvalidOperationException("Unreachable code.");
    }
}
