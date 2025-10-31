using System.Text.RegularExpressions;

namespace Order_Handler_App.Helpers;

internal static partial class RegexHelper
{
    [GeneratedRegex("[0-9A-Z]{5}-[A-Z][1-4]-[0-9]{4}")]
    internal static partial Regex OrderIdPattern();
}