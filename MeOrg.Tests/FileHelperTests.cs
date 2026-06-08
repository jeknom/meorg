using MeOrg.Extensions;
using Xunit.Abstractions;

namespace MeOrg.Tests;

public class FileNameHelperTests
{
    private readonly IConsole _console;

    public FileNameHelperTests(ITestOutputHelper output)
    {
        _console = new TestConsole(output, new OrganizeRunMetrics());
    }

    [Fact]
    public async Task Test_Increment_Suffix()
    {
        Assert.Equal("/some/testfile (2).jpg", FileHelper.GetFilepathWithIncrementedNumericalSuffix("/some/testfile.jpg"));
        Assert.Equal("/some/testfile (101).jpg", FileHelper.GetFilepathWithIncrementedNumericalSuffix("/some/testfile (100).jpg"));
        Assert.Equal("/some/test (1) file (2000).jpg", FileHelper.GetFilepathWithIncrementedNumericalSuffix("/some/test (1) file (1999).jpg"));
    }

    [Theory]
    [InlineData("quick-time.MOV", "2024-09-21")]
    [InlineData("exif-create-date.HEIC", "2026-04-25")]
    // This one has the exif create date stripped and it should pull the exif modified date instead
    [InlineData("exif-create-date-stripped.HEIC", "2026-04-25")]
    public async Task Test_Extract_Creation_Date_Time_Metadata(string filename, string expectedDate)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestFiles/CreationTimeExtraction", filename);
        Assert.True(FileHelper.TryExtractMediaMetadataCreationDateTime(path, _console, out DateTime createdDateTime));
        Assert.Equal(expectedDate, createdDateTime.ToMeorgDateString());
    }
}
