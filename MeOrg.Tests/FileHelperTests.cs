using Xunit.Abstractions;

namespace MeOrg.Tests;

public class FileNameHelperTests
{
    [Fact]
    public async Task Test_Increment_Suffix()
    {
        Assert.Equal("/some/testfile (2).jpg", FileHelper.GetFilepathWithIncrementedNumericalSuffix("/some/testfile.jpg"));
        Assert.Equal("/some/testfile (101).jpg", FileHelper.GetFilepathWithIncrementedNumericalSuffix("/some/testfile (100).jpg"));
        Assert.Equal("/some/test (1) file (2000).jpg", FileHelper.GetFilepathWithIncrementedNumericalSuffix("/some/test (1) file (1999).jpg"));
    }
}
