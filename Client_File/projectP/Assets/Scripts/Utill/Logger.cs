using UnityEngine;

public static class Logger
{
    public static bool Enabled { get; set; } = true;

    public static void Log(object message)
    {
        if (!Enabled) return;
        Debug.Log(message);
    }

    public static void Log(string tag, object message)
    {
        if (!Enabled) return;
        Debug.Log($"[{tag}] {message}");
    }

    public static void Warning(object message)
    {
        if (!Enabled) return;
        Debug.LogWarning(message);
    }

    public static void Warning(string tag, object message)
    {
        if (!Enabled) return;
        Debug.LogWarning($"[{tag}] {message}");
    }

    public static void Error(object message)
    {
        if (!Enabled) return;
        Debug.LogError(message);
    }

    public static void Error(string tag, object message)
    {
        if (!Enabled) return;
        Debug.LogError($"[{tag}] {message}");
    }

    public static void Exception(System.Exception exception)
    {
        if (!Enabled) return;
        Debug.LogException(exception);
    }
}
