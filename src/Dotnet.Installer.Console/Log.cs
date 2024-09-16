namespace Dotnet.Installer.Console;

public static class Log
{
    private static void Print(string prefix, string message) => System.Console.WriteLine(string.Concat(prefix, message));
    
    private static void Print(string prefix, string message, ConsoleColor textColor)
    {
        System.Console.ForegroundColor = textColor;
        System.Console.WriteLine(string.Concat(prefix, message));
        System.Console.ResetColor();
    }

    public static void Fatal(string message) => Print("[FATAL] ", message, ConsoleColor.DarkRed);
    public static void Error(string message) => Print("[ERROR] ", message, ConsoleColor.Red);
    public static void Warning(string message) => Print("[WARNING] ", message, ConsoleColor.Yellow);
    public static void Info(string message) => Print("[INFO] ", message);
    public static void Debug(string message) => Print("[DEBUG] ", message, ConsoleColor.Gray);
}