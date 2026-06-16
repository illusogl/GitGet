using System.Diagnostics;

namespace GitGet.WebUI;

static class Log
{
    public static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GitGet", "debug.log");

    static Log()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
    }

    public static void Info(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
        try { File.AppendAllText(LogPath, line + "\n"); } catch { }
        try { Console.WriteLine(line); } catch { }
    }

    public static void Fatal(string msg)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] [FATAL] {msg}";
        try { File.AppendAllText(LogPath, line + "\n"); } catch { }
        try { Console.WriteLine(line); } catch { }
    }
}
