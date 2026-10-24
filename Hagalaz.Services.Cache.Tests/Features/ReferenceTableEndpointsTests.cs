using System.IO;
using System.Linq;
using System.Text.Json;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class ReferenceTableEndpointsTests
{
    [TestMethod]
    public async Task SparseReferenceTableResponse_ReportsCountAndIdCapacitySeparately()
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 2);
        table.AddEntry(1, CreateEntry(0));
        table.AddEntry(100, CreateEntry(1));

        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<ICacheAPI>(new CacheApiStub(table));
        await using var app = builder.Build();
        app.MapGroup("/api/v1/cache").MapReferenceTableEndpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(route => route.RoutePattern.RawText == "/api/v1/cache/indexes/{indexId:int}/reference-table");

        var context = new DefaultHttpContext
        {
            RequestServices = app.Services
        };
        context.Request.RouteValues["indexId"] = "0";
        context.Request.Method = "GET";
        context.Response.Body = new MemoryStream();

        await endpoint.RequestDelegate!(context);

        Assert.AreEqual(StatusCodes.Status200OK, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        var response = document.RootElement;
        Assert.AreEqual(2, response.GetProperty("count").GetInt32());
        Assert.AreEqual(101, response.GetProperty("capacity").GetInt32());
        CollectionAssert.AreEqual(
            new[] { 1, 100 },
            response.GetProperty("entries").EnumerateArray()
                .Select(entry => entry.GetProperty("fileId").GetInt32())
                .ToArray());
    }

    private static ReferenceTableEntry CreateEntry(int index)
    {
        var entry = new ReferenceTableEntry(index);
        entry.InitializeEntries(0);
        return entry;
    }

    private sealed class CacheApiStub(IReferenceTable table) : ICacheAPI
    {
        public int GetFileId(int indexId, string fileName) => throw new NotSupportedException();

        public int GetFileCount(int indexId) => throw new NotSupportedException();

        public int GetFileCount(int indexId, int fileId) => throw new NotSupportedException();

        public IReferenceTable ReadReferenceTable(int indexId) => table;

        public IContainer ReadContainer(int indexId, int fileId) => throw new NotSupportedException();

        public IArchive ReadArchive(int indexId, int fileId) => throw new NotSupportedException();

        public MemoryStream Read(int indexId, int fileId, int subFileId) => throw new NotSupportedException();

        public IContainer ReadContainer(int indexId, int fileId, int[] xteaKeys) => throw new NotSupportedException();

        public MemoryStream Read(int indexId, int fileId) => throw new NotSupportedException();

        public void Write(int indexId, int fileId, IContainer container) => throw new NotSupportedException();

        public void WriteRawFile(int indexId, int fileId, MemoryStream data) => throw new NotSupportedException();

        public void CopyRawFile(int indexId, int fileId, int sourceIndexId, int sourceFileId) => throw new NotSupportedException();

        public IChecksumTable CreateChecksumTable() => throw new NotSupportedException();
    }
}
