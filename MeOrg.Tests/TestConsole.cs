using Xunit.Abstractions;

namespace MeOrg.Tests;

public class TestConsole : IConsole
{
    private readonly ITestOutputHelper _testOutput;
    private readonly OrganizeRunMetrics _metrics;

    public TestConsole(ITestOutputHelper testOutput, OrganizeRunMetrics metrics)
    {
        _testOutput = testOutput;
        _metrics = metrics;
    }

    public Task<bool> Confirm(string question, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void WriteErrorLine(string message)
    {
        _testOutput.WriteLine($"{DateTime.Now:HH:mm:ss:ff} - Error: {message}");
    }

    public void WriteException(Exception ex)
    {
        _testOutput.WriteLine(ex.ToString());
    }

    public void WriteInfoLine(string message)
    {
        _testOutput.WriteLine($"{DateTime.Now:HH:mm:ss:ff} - Info: {message}");
    }

    public void WriteInputs(string source, string target, int dayOffset, bool dedupe, bool promptUser)
    {
        _testOutput.WriteLine("\nINPUTS");
        _testOutput.WriteLine($"Source: '{source}'\nTarget: '{target}'\nDay offset: '{dayOffset}'\nDedupe: '{dedupe}'\nPrompt user: '{promptUser}'");
        _testOutput.WriteLine("\n");
    }

    public void WriteReport()
    {
        _testOutput.WriteLine("\nREPORT");
        _testOutput.WriteLine("Pre-existing target directory lookup creation time: {0}s", _metrics.PreExistingTargetDirLookupCreationTime.TotalSeconds.ToString());
        _testOutput.WriteLine("Pre-existing target media hash generation time: {0}s", _metrics.PreExistingTargetMediaHashGenerationTime.TotalSeconds.ToString());
        _testOutput.WriteLine("Source file processing time: {0}s", _metrics.SourceFileProcessingTime.TotalSeconds.ToString());
        _testOutput.WriteLine("Organized files: {0}", _metrics.CopyCount.ToString());
        _testOutput.WriteLine("Duplicates filtered: {0}", _metrics.DuplicateCount.ToString());
        _testOutput.WriteLine("Total seconds elapsed: {0}s", _metrics.ElapsedSeconds.ToString());
        _testOutput.WriteLine("\n");
    }
}