using MeOrg.Exceptions;
using Xunit.Abstractions;

namespace MeOrg.Tests;

public class DirectoryEqualMediaComparerTests : IDisposable
{
    private readonly IConsole _console;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly string _basepath = new(Path.Combine(AppContext.BaseDirectory, "TestFiles/ValidatorTesting"));
    private readonly FileAccess _fileAccess = new FileAccess();
    private readonly DirectoryEqualMediaComparer _comparer;

    public DirectoryEqualMediaComparerTests(ITestOutputHelper output)
    {
        _console = new TestConsole(output);
        _comparer = new DirectoryEqualMediaComparer(_fileAccess, _console);
    }

    public void Dispose()
    {
        _cts.Cancel();
    }

    [Fact]
    public void Compare_Equal_Directories()
    {
        DirectoryInfo source = new(Path.Combine(_basepath, "ContainsFiles", "Source"));
        DirectoryInfo target = new(Path.Combine(_basepath, "ContainsFiles", "Target"));
        int code = _comparer.CompareMediaEquality(source, target, CancellationToken.None);

        Assert.Equal((int)ExitCode.Success, code);
    }

    [Fact]
    public void Compare_Source_Missing_Media_File()
    {
        DirectoryInfo source = new(Path.Combine(_basepath, "SourceMissingMediaFile", "Source"));
        DirectoryInfo target = new(Path.Combine(_basepath, "SourceMissingMediaFile", "Target"));
        ErrorExitException ex = Assert.Throws<ErrorExitException>(() => _comparer.CompareMediaEquality(source, target, CancellationToken.None));
        Assert.Equal(ExitCode.DirectoriesNotInSync, ex.Code);
    }
}
