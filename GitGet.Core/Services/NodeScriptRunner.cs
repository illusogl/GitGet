using System.Diagnostics;
using System.Text.Json;
using GitGet.Core.Interfaces;

namespace GitGet.Core.Services;

/// <summary>
/// Runs Node.js scripts and returns parsed JSON results.
/// Used as a bridge between C# and JavaScript (Node.js) for GitHub API calls.
/// </summary>
public class NodeScriptRunner : INodeScriptRunner
{
    private readonly string _scriptPath;
    private readonly string? _nodePath;

    public NodeScriptRunner(string scriptPath, string? nodePath = null)
    {
        _scriptPath = Path.GetFullPath(scriptPath);
        _nodePath = nodePath ?? "node";
    }

    /// <summary>
    /// Execute the script with given arguments and return parsed JSON.
    /// </summary>
    public async Task<T?> RunAsync<T>(string[] arguments, CancellationToken ct = default) where T : class
    {
        var result = await RunScriptAsync(arguments, ct);
        return JsonSerializer.Deserialize<T>(result);
    }

    /// <summary>
    /// Execute the script and return raw JSON string.
    /// </summary>
    public async Task<string> RunScriptAsync(string[] arguments, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _nodePath,
            Arguments = $"\"{_scriptPath}\" {string.Join(" ", arguments.Select(EscapeArg))}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };
        var output = new System.Text.StringBuilder();
        var error = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) error.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Node.js script execution timed out after 30 seconds.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Node.js script failed with exit code {process.ExitCode}: {error}");
        }

        var result = output.ToString().Trim();
        if (string.IsNullOrWhiteSpace(result))
        {
            throw new InvalidOperationException(
                $"Node.js script produced no output. Error: {error}");
        }

        return result;
    }

    private static string EscapeArg(string arg)
    {
        // Escape double quotes and wrap in quotes if contains spaces
        if (string.IsNullOrEmpty(arg)) return "\"\"";
        if (!arg.Contains(' ') && !arg.Contains('"')) return arg;
        return $"\"{arg.Replace("\"", "\\\"")}\"";
    }
}