using System.Collections.Concurrent;
using Xunit.Abstractions;

namespace MeOrg.Tests;

public class TestConsole : IConsole
{
    private readonly ITestOutputHelper _testOutput;
    public ConcurrentStack<string> Logs { get; private set; } = new();

    public TestConsole(ITestOutputHelper testOutput, OrganizeRunMetrics metrics)
    {
        _testOutput = testOutput;
    }

    public Task<bool> Confirm(string question, CancellationToken cancellationToken)
    {
        return Task.FromResult(true);
    }

    public void SetYesToAll()
    {
    }

    public void WriteErrorLine(string message)
    {
        Logs.Push(message);
        _testOutput.WriteLine($"{DateTime.Now:HH:mm:ss:ff} - Error: {message}");
    }

    public void WriteException(Exception ex)
    {
        string exStr = ex.ToString();
        Logs.Push(exStr);
        _testOutput.WriteLine(exStr);
    }

    public void WriteInfoLine(string message)
    {
        Logs.Push(message);
        _testOutput.WriteLine($"{DateTime.Now:HH:mm:ss:ff} - Info: {message}");
    }

    public void WriteInputs(string source, string target, int dayOffset, bool dedupe, bool promptUser)
    {
        _testOutput.WriteLine("\nINPUTS");
        _testOutput.WriteLine($"Source: '{source}'\nTarget: '{target}'\nDay offset: '{dayOffset}'\nDedupe: '{dedupe}'\nPrompt user: '{promptUser}'");
        _testOutput.WriteLine("\n");
    }

    public void WriteReport(OrganizeRunMetrics metrics)
    {
        _testOutput.WriteLine("\nREPORT");
        _testOutput.WriteLine("Duplicates: {0}", metrics.DuplicateCount.ToString());
        _testOutput.WriteLine("Copied media: {0}", metrics.CopyCount.ToString());
        _testOutput.WriteLine("Target hashing time: {0}s", metrics.TargetMediaHashGenerationTime.TotalSeconds.ToString());
        _testOutput.WriteLine("Source processing time: {0}s", metrics.SourceFileProcessingTime.TotalSeconds.ToString());
        _testOutput.WriteLine("Total elapsed time: {0}s", metrics.ElapsedSeconds.ToString());
        _testOutput.WriteLine("\n");
    }
}