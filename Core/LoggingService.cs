using System;

namespace Order_Handler_App.Core;

internal static class LoggingService
{
    internal static void WriteLog(string message, ConsoleColor consoleColor = ConsoleColor.Gray)
    {
        message = $"{DateTime.Now:dd/MM/yyyy HH:mm:ss} {message}";

        Console.ForegroundColor = consoleColor;
        Console.Write(message + Environment.NewLine);
        Console.ResetColor();
    }
}
