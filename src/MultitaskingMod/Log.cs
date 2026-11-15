using MelvorModLoader;

namespace MultitaskingMod;

internal static class Log
{
    private static readonly ModLogger s_logger = new("Multitasking");

    public static void Info(string message) => s_logger.Info(message);

    public static void Error(string message) => s_logger.Error(message);
}
