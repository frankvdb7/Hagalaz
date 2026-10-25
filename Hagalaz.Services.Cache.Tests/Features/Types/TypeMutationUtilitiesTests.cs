using System.Text;
using Hagalaz.Services.Cache.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public class TypeMutationUtilitiesTests
{
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
