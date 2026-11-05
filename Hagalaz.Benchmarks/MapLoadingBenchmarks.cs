using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Providers;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.Services.Cache.Features.Models.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Benchmarks;

[MemoryDiagnoser]
public class MapTerrainDecodeBenchmarks
{
    private byte[] _terrain = null!;
    private byte[] _landscape = null!;

    [GlobalSetup]
    public void Setup()
    {
        _terrain = ReadFixture("terrain-12850.dat", "6504b39027a67bbecd04ad2420a6d05fd4dce323ac3244fc90758c50bd1a324b");
        _landscape = ReadFixture("landscape-12850.dat", "6bf40b28daefd20e77517d001f37dbd50a315c154e08e7cd61141a9683808dba");
        if (DecodeTerrain().Length != 4 || DecodeLandscape().Length != 5934)
            throw new InvalidDataException("The pinned region fixture did not decode as expected.");
    }

    [Benchmark]
    public TerrainRenderPlane[] DecodeTerrain()
    {
        using var stream = new MemoryStream(_terrain, writable: false);
        return MapRenderDecoder.DecodeTerrain(stream, 12850);
    }

    [Benchmark]
    public MapRenderPlacement[] DecodeLandscape()
    {
        using var stream = new MemoryStream(_landscape, writable: false);
        return MapRenderDecoder.DecodeLandscape(stream);
    }

    private static byte[] ReadFixture(string name, string hash)
    {
        var data = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "maps", name));
        if (Convert.ToHexStringLower(SHA256.HashData(data)) != hash) throw new InvalidDataException($"Fixture hash mismatch: {name}");
        return data;
    }
}

/// <summary>One operation is 32 appearances, including request binding, decode/assembly and response JSON.</summary>
[MemoryDiagnoser]
public class MapAppearanceRequestBenchmarks
{
    private const int KeyCount = 32;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private WebApplication _app = null!;
    private RequestDelegate _single = null!;
    private RequestDelegate _batch = null!;
    private ObjectRenderAppearanceKeyDto[] _keys = null!;
    private byte[] _body = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production", Args = [] });
        builder.Logging.ClearProviders();
        var cachePath = Environment.GetEnvironmentVariable("HAGALAZ_BENCHMARK_CACHE_PATH");
        if (string.IsNullOrWhiteSpace(cachePath)) builder.Services.AddSingleton<IFileStore>(CreateFixture());
        builder.Services.AddGameCache(options => { if (!string.IsNullOrWhiteSpace(cachePath)) options.Path = cachePath; });
        _app = builder.Build();
        _app.MapGroup("/api/v1/cache").MapModelEndpoints();
        var endpoints = ((IEndpointRouteBuilder)_app).DataSources.SelectMany(source => source.Endpoints).OfType<RouteEndpoint>().ToArray();
        _single = endpoints.Single(endpoint => endpoint.RoutePattern.RawText!.EndsWith("/{id:int}/render-appearance", StringComparison.Ordinal)).RequestDelegate!;
        _batch = endpoints.Single(endpoint => endpoint.RoutePattern.RawText!.EndsWith("/objects/render-appearances", StringComparison.Ordinal)).RequestDelegate!;
        _keys = string.IsNullOrWhiteSpace(cachePath)
            ? Enumerable.Range(0, KeyCount).Select(id => new ObjectRenderAppearanceKeyDto(id, 10, id & 3)).ToArray()
            : await SelectRealKeys();
        _body = JsonSerializer.SerializeToUtf8Bytes(_keys, Json);

        // Warm framework metadata/cache snapshots and verify the benchmark measures equal successful work.
        using var batchResponse = new MemoryStream();
        await Execute(_batch, null, batchResponse);
        using var batch = JsonDocument.Parse(batchResponse.ToArray());
        if (batch.RootElement.GetArrayLength() != KeyCount) throw new InvalidDataException("Batch did not return every appearance.");
        long individualBytes = 0;
        for (var index = 0; index < KeyCount; index++)
        {
            using var singleResponse = new MemoryStream();
            await Execute(_single, _keys[index], singleResponse);
            individualBytes += singleResponse.Length;
            using var single = JsonDocument.Parse(singleResponse.ToArray());
            var row = batch.RootElement[index];
            if (row.GetProperty("status").GetInt32() != 200 || !JsonElement.DeepEquals(single.RootElement, row.GetProperty("appearance")))
                throw new InvalidDataException("Single and batch appearance results differ.");
        }
        if (await IndividualRequests32() != individualBytes || await BatchRequest32() != batchResponse.Length)
            throw new InvalidDataException("The counting sink did not receive the full serialized response.");
        Console.WriteLine($"Appearance dataset: {(string.IsNullOrWhiteSpace(cachePath) ? "deterministic in-memory models" : "region 12850 real cache")}; 32 unique keys; SHA256 {Convert.ToHexStringLower(SHA256.HashData(_body))}");
    }

    [Benchmark(Baseline = true)]
    public async Task<long> IndividualRequests32()
    {
        long bytes = 0;
        // This loop is the actual 32-request workload, not artificial benchmark repetition.
        foreach (var key in _keys)
        {
            using var response = new CountingResponseStream();
            await Execute(_single, key, response);
            bytes += response.Length;
        }
        return bytes;
    }

    [Benchmark]
    public async Task<long> BatchRequest32()
    {
        using var response = new CountingResponseStream();
        await Execute(_batch, null, response);
        return response.Length;
    }

    private async Task Execute(RequestDelegate handler, ObjectRenderAppearanceKeyDto? key, Stream response)
    {
        using var scope = _app.Services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Response.Body = response;
        using var body = new MemoryStream(key is null ? _body : [], writable: false);
        context.Request.Method = key is null ? "POST" : "GET";
        if (key is null)
        {
            context.Request.Body = body;
            context.Request.ContentType = "application/json";
            context.Request.ContentLength = body.Length;
            context.Features.Set<IHttpRequestBodyDetectionFeature>(new RequestBody());
        }
        else
        {
            context.Request.RouteValues["id"] = key.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
            context.Request.QueryString = new QueryString($"?shape={key.Shape}&rotation={key.Rotation}");
        }
        await handler(context);
        if (context.Response.StatusCode != 200) throw new InvalidDataException($"Appearance returned HTTP {context.Response.StatusCode}.");
    }

    private async Task<ObjectRenderAppearanceKeyDto[]> SelectRealKeys()
    {
        var region = new MapRenderReader(_app.Services.GetRequiredService<ICacheAPI>()).Read(12850, null);
        if (region.LandscapeStatus != "available") throw new InvalidDataException("Region 12850 landscape is unavailable in the selected cache.");
        var selected = new List<ObjectRenderAppearanceKeyDto>();
        foreach (var key in region.Objects.Where(placement => placement.Plane == 0).Select(placement => new ObjectRenderAppearanceKeyDto(placement.Id, placement.Shape, placement.Rotation)).Distinct())
        {
            try
            {
                using var response = new MemoryStream();
                await Execute(_single, key, response);
                selected.Add(key);
                if (selected.Count == KeyCount) return selected.ToArray();
            }
            catch (Exception error) when (error is IOException or NotSupportedException) { /* Known unavailable appearances are not successful benchmark work. */ }
        }
        throw new InvalidDataException("The cache does not provide 32 successful ground-plane appearances for region 12850.");
    }

    [GlobalCleanup]
    public async Task Cleanup() { if (_app is not null) await _app.DisposeAsync(); }

    private sealed class RequestBody : IHttpRequestBodyDetectionFeature { public bool CanHaveBody => true; }

    // Do not measure an artificial ever-growing captured response buffer. Setup captures JSON separately.
    private sealed class CountingResponseStream : Stream
    {
        private long _bytes;
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _bytes;
        public override long Position { get => _bytes; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override void Write(byte[] buffer, int offset, int count) => _bytes += count;
        public override void Write(ReadOnlySpan<byte> buffer) => _bytes += buffer.Length;
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _bytes += buffer.Length;
            return ValueTask.CompletedTask;
        }
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _bytes += count;
            return Task.CompletedTask;
        }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    private static IFileStore CreateFixture()
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        var entry = new ReferenceTableEntry(0);
        entry.InitializeEntries(KeyCount);
        var members = new byte[KeyCount][];
        for (var id = 0; id < KeyCount; id++)
        {
            entry.AddEntry(id, new ReferenceTableChildEntry(id));
            members[id] = [1, 1, 10, 1, 0, 0, 65, 0, 128, 70, 0, (byte)id, 0];
        }
        table.AddEntry(0, entry);
        using var encoded = new ReferenceTableCodec().Encode(table);
        // Real legacy model bytecode, also used by the model assembly regression fixtures.
        byte[] model = [0, 1, 3, 1, 2, 64, 65, 65, 0, 42, 0, 0, 0, 1, 0, 2, 74, 54, 84, 0, 3, 0, 1, 1, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
        return new BenchmarkFileStore(new Dictionary<(int, int), byte[]> {
            [(255, 16)] = ContainerBytes(encoded.ToArray()),
            [(16, 0)] = ContainerBytes(CacheBenchmarkFixture.EncodeArchive(members)),
            [(7, 0)] = ContainerBytes(model)
        });
    }

    private static byte[] ContainerBytes(byte[] data)
    {
        using var container = new Container(new MemoryStream(data, writable: false));
        return container.Encode();
    }
}
