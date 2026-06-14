namespace GitGet.Core.Interfaces;

/// <summary>
/// Interface for running Node.js scripts. Allows mocking in tests.
/// </summary>
public interface INodeScriptRunner
{
    Task<string> RunScriptAsync(string[] arguments, CancellationToken ct = default);
}