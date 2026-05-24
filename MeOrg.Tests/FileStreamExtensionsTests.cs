using MeOrg.Extensions;

namespace MeOrg.Tests;

public class FileStreamExtensionsTests
{
    private readonly string _testFilesBase = new(Path.Combine(AppContext.BaseDirectory, "TestFiles/DuplicateTesting"));

    [Fact]
    public void Generated_Hash_Matches_On_Duplicate_File_Contents()
    {
        using FileStream aStream = File.Open(Path.Combine(_testFilesBase, "duplicate-a.jpg"), FileMode.Open);
        string aHash = aStream.GenerateSampledHash();

        using FileStream bStream = File.Open(Path.Combine(_testFilesBase, "duplicate-b.jpg"), FileMode.Open);
        string bHash = bStream.GenerateSampledHash();

        Assert.Equal(aHash, bHash);
    }

    [Fact]
    public void Generated_Hash_Mismatch_On_Differing_File_Contents()
    {
        using FileStream aStream = File.Open(Path.Combine(_testFilesBase, "unique-a.png"), FileMode.Open);
        string aHash = aStream.GenerateSampledHash();

        using FileStream bStream = File.Open(Path.Combine(_testFilesBase, "unique-b.png"), FileMode.Open);
        string bHash = bStream.GenerateSampledHash();

        Assert.NotEqual(aHash, bHash);
    }
}
