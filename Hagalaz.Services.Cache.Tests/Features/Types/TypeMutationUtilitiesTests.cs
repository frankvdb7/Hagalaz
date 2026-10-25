using System.Text;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public class TypeMutationUtilitiesTests
{
    [TestMethod]
    public void BuildSingleChunkArchive_WithSingleEntry_ReturnsExactBytes()
    {
        var entry = new MemoryStream([1, 2, 3]);

        using var result = TypeMutationUtilities.BuildSingleChunkArchive([entry]);

        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, result.ToArray());
    }

    [TestMethod]
    public void BuildSingleChunkArchive_WithMultipleEntries_AppendsLengthDeltas()
    {
        var first = new MemoryStream([1, 2]);
        var second = new MemoryStream([3]);

        using var result = TypeMutationUtilities.BuildSingleChunkArchive([first, second]);

        CollectionAssert.AreEqual(new byte[]
        {
            1, 2, 3,
            0, 0, 0, 2,
            255, 255, 255, 255,
            1
        }, result.ToArray());
    }

    [TestMethod]
    public void BuildSingleChunkArchive_WithMultipleEntries_RoundTripsThroughArchiveDecoder()
    {
        var firstBytes = new byte[] { 1, 2 };
        var secondBytes = new byte[] { 3 };
        using var first = new MemoryStream(firstBytes);
        using var second = new MemoryStream(secondBytes);

        using var encoded = TypeMutationUtilities.BuildSingleChunkArchive([first, second]);
        using var container = new Container(new MemoryStream(encoded.ToArray()));
        using var archive = new ArchiveDecoder().Decode(container, 2);

        Assert.IsNotNull(archive.Entries);
        CollectionAssert.AreEqual(firstBytes, archive.Entries[0].ToArray());
        CollectionAssert.AreEqual(secondBytes, archive.Entries[1].ToArray());
    }

    [TestMethod]
    public async Task ReadImageRequestStreamAsync_WithRawBody_ReadsBody()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "image/png";
        context.Request.Body = new MemoryStream(Encoding.ASCII.GetBytes("raw-bytes"));

        await using var result = await TypeMutationUtilities.ReadImageRequestStreamAsync(context.Request);

        Assert.AreEqual("raw-bytes", Encoding.ASCII.GetString(result.ToArray()));
    }

    [TestMethod]
    public async Task ReadImageRequestStreamAsync_WithMultipartFile_ReadsUploadedFile()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "multipart/form-data; boundary=unit-test";

        var fileBytes = Encoding.ASCII.GetBytes("file-bytes");
        var fileStream = new MemoryStream(fileBytes);
        var formFile = new FormFile(fileStream, 0, fileBytes.Length, "file", "sprite.png");

        var files = new FormFileCollection { formFile };
        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>(), files);
        context.Features.Set<IFormFeature>(new FormFeature(form));

        await using var result = await TypeMutationUtilities.ReadImageRequestStreamAsync(context.Request);

        Assert.AreEqual("file-bytes", Encoding.ASCII.GetString(result.ToArray()));
    }
}
