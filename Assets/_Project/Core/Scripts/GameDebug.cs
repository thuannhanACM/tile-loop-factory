using System.Diagnostics;
using System.Runtime.CompilerServices;

public enum LogTopic { General, Gameplay, Audio, UI, Network, Physics, AI }

public static class GameDebug
{
    [Conditional("ENABLE_LOGS")]
    public static void Log(
        string message,
        LogTopic topic = LogTopic.General,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0) =>
        UnityEngine.Debug.Log(Format(topic, message, file, member, line));

    [Conditional("ENABLE_LOGS")]
    public static void LogWarning(
        string message,
        LogTopic topic = LogTopic.General,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0) =>
        UnityEngine.Debug.LogWarning(Format(topic, message, file, member, line));

    [Conditional("ENABLE_LOGS")]
    public static void LogError(
        string message,
        LogTopic topic = LogTopic.General,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0) =>
        UnityEngine.Debug.LogError(Format(topic, message, file, member, line));

    private static string Format(
        LogTopic topic, string msg, string file, string member, int line)
    {
        var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
        return $"[{topic}] {fileName}.{member}:{line} — {msg}";
    }
}
