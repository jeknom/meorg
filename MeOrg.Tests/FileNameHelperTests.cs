using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;

namespace MeOrg.Tests;

public class FileNameHelperTests
{
    [Fact]
    public async Task Test_Increment_Suffix()
    {
        Assert.Equal("/some/testfile (2).jpg", FileNameHelper.GetNextPossiblePath("/some/testfile.jpg"));
        Assert.Equal("/some/testfile (101).jpg", FileNameHelper.GetNextPossiblePath("/some/testfile (100).jpg"));
        Assert.Equal("/some/test (1) file (2000).jpg", FileNameHelper.GetNextPossiblePath("/some/test (1) file (1999).jpg"));
    }
}
