using Hagalaz.Services.Cache.Features.Models.Dtos;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Security;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public sealed class ModelAppearanceTests
{
    [TestMethod]
    public async Task ObjectCatalog_PagesActualIdsAndSearchesNamesWithoutReadingModels()
    {
        var store = new MemoryFileStore();
        var fence = Definition("objects").SkipLast(1).Concat(new byte[] { 2, 70, 101, 110, 99, 101, 0, 0 }).ToArray();
        SeedDefinitions(store, 16, (2, Definition("objects")), (7, fence));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapGroup("/types").MapObjectDefinitionsEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        const string url = "/api/v1/cache/types/objects/catalog";
        var page = await client.GetFromJsonAsync<SearchDefinitionsDto<GetObjectCatalogEntryDto>>($"{url}?offset=1&limit=1");
        Assert.IsNotNull(page);
        Assert.AreEqual(2, page.TotalMatches);
        Assert.AreEqual(new GetObjectCatalogEntryDto(7, "Fence", 10), page.Items.Single());
        var search = await client.GetFromJsonAsync<SearchDefinitionsDto<GetObjectCatalogEntryDto>>($"{url}?query=fEnCe");
        Assert.IsNotNull(search);
        Assert.AreEqual(1, search.TotalMatches);
        Assert.AreEqual(7, search.Items.Single().Id);
        Assert.AreEqual(0, store.GetReadCount(7, 0));
        // The reused name index follows object-index writes.
        (await client.PostAsJsonAsync("/api/v1/cache/types/objects/7/name", new { name = "Gate" })).EnsureSuccessStatusCode();
        var renamed = await client.GetFromJsonAsync<SearchDefinitionsDto<GetObjectCatalogEntryDto>>($"{url}?query=gate");
        Assert.IsNotNull(renamed);
        Assert.AreEqual(7, renamed.Items.Single().Id);
        var stale = await client.GetFromJsonAsync<SearchDefinitionsDto<GetObjectCatalogEntryDto>>($"{url}?query=fence");
        Assert.AreEqual(0, stale!.TotalMatches);
    }

    [TestMethod]
    public async Task ObjectCatalog_FiltersByShapeWithNameAndIdSearchAndRejectsInvalidShapes()
    {
        static byte[] Definition(byte shape, string name) => [1, 1, shape, 1, 0x80, 0, 0, 5, 2, .. System.Text.Encoding.ASCII.GetBytes(name), 0, 0];
        var store = new MemoryFileStore();
        SeedDefinitions(store, 16, (1, Definition(0, "Stone wall")), (2, Definition(0, "Oak wall")), (3, Definition(10, "Stone table")), (4, Definition(22, "Rug")));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapGroup("/types").MapObjectDefinitionsEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        const string url = "/api/v1/cache/types/objects/catalog";
        async Task<int[]> Ids(string parameters) => (await client.GetFromJsonAsync<SearchDefinitionsDto<GetObjectCatalogEntryDto>>($"{url}?{parameters}"))!.Items.Select(entry => entry.Id).ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2 }, await Ids("shapes=0,1,2,3,9"));
        CollectionAssert.AreEqual(new[] { 1 }, await Ids("shapes=0&query=stone"));
        CollectionAssert.AreEqual(new[] { 3 }, await Ids("shapes=10&query=3"));
        CollectionAssert.AreEqual(Array.Empty<int>(), await Ids("shapes=10&query=1"));
        CollectionAssert.AreEqual(new[] { 2 }, await Ids("shapes=0&offset=1&limit=1"));
        using var invalid = await client.GetAsync($"{url}?shapes=23");
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [TestMethod]
    public async Task ObjectImage_RendersARealObjectShapeAndRejectsMissingShapes()
    {
        const int objectId = 45291, modelId = 48743;
        var store = new MemoryFileStore();
        SeedDefinition(store, 16, [1, 1, 10, 1, 0x80, 0, modelId >> 8 & 255, modelId & 255, 0], id: objectId);
        store.Set(7, modelId, ContainerBytes(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "models", $"model-{modelId}.dat"))));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var response = await client.GetAsync($"/api/v1/cache/types/objects/{objectId}/image?width=192&height=128&shape=10");
        Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual("image/png", response.Content.Headers.ContentType?.MediaType);
        using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(await response.Content.ReadAsByteArrayAsync());
        Assert.AreEqual((192, 128), (image.Width, image.Height));
        var drawn = 0;
        image.ProcessPixelRows(rows => { for (var y = 0; y < rows.Height; y++) foreach (var pixel in rows.GetRowSpan(y)) if (pixel.A > 0) drawn++; });
        Assert.IsGreaterThan(1000, drawn);

        using var missing = await client.GetAsync($"/api/v1/cache/types/objects/{objectId}/image?shape=22");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, missing.StatusCode);
        using var invalid = await client.GetAsync($"/api/v1/cache/types/objects/{objectId}/image?width=7");
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ObjectTransformId_DistinguishesHiddenShortFromRealExtendedId(bool extended)
    {
        byte[] prefix=[92,255,255,0,1,0,2,0];
        byte[] target=extended?[128,0,127,255]:[127,255];
        using var data=new MemoryStream(prefix.Concat(target).Append((byte)0).ToArray());
        var definition=(ObjectType)new ObjectTypeCodec().Decode(0,data);
        Assert.IsNotNull(definition.TransformToIDs);
        CollectionAssert.AreEqual(new[]{extended?32767:-1,2},definition.TransformToIDs);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public async Task ObjectRenderAppearance_12163UsesZeroStateCanoeVariantInSingleAndBatch(int rotation)
    {
        var store=new MemoryFileStore();
        SeedDefinitions(store,16,(12163,File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","objects","object-12163.dat"))),
            (12144,File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","objects","object-12144.dat"))));
        store.Set(7,11990,ContainerBytes(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","models","model-11990.dat"))));
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapModelEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        var selected=await client.GetFromJsonAsync<GetObjectRenderAppearanceDto>($"/api/v1/cache/types/objects/12144/render-appearance?shape=10&rotation={rotation}");
        var source=await client.GetFromJsonAsync<GetObjectRenderAppearanceDto>($"/api/v1/cache/types/objects/12163/render-appearance?shape=10&rotation={rotation}");
        Assert.IsNotNull(selected);Assert.IsNotNull(source);
        Assert.AreEqual(12163,source.Id);Assert.AreEqual(10,source.Shape);Assert.AreEqual(rotation,source.Rotation);
        Assert.HasCount(1,source.Parts);Assert.IsTrue(source.Parts[0].Indices.Length>0);
        Assert.AreEqual(12163,source.Parts[0].Id);
        Assert.AreEqual(selected.SizeX,source.SizeX);Assert.AreEqual(selected.SizeY,source.SizeY);
        Assert.AreEqual(selected.ContourMode,source.ContourMode);
        CollectionAssert.AreEqual(selected.Parts[0].Positions,source.Parts[0].Positions);
        CollectionAssert.AreEqual(selected.Parts[0].Indices,source.Parts[0].Indices);
        CollectionAssert.AreEqual(selected.Parts[0].FaceTextureUvs,source.Parts[0].FaceTextureUvs);
        Assert.IsTrue(source.Warnings.Any(warning=>warning.Contains("object 12144")&&warning.Contains("zero-valued")));
        using var response=await client.PostAsJsonAsync("/api/v1/cache/types/objects/render-appearances",new[]{new ObjectRenderAppearanceKeyDto(12163,10,rotation)});
        var rows=await response.Content.ReadFromJsonAsync<ObjectRenderAppearanceResultDto[]>();
        Assert.IsNotNull(rows);Assert.HasCount(1,rows);Assert.AreEqual(200,rows[0].Status);
        var batch=rows[0].Appearance;Assert.IsNotNull(batch);Assert.AreEqual(12163,batch.Id);
        CollectionAssert.AreEqual(source.Parts[0].Positions,batch.Parts[0].Positions);
        CollectionAssert.AreEqual(source.Warnings,batch.Warnings);
    }

    [TestMethod]
    public async Task ObjectRenderAppearance_ReportsTheClientFloorShadowStrength()
    {
        var store=new MemoryFileStore();
        byte[] model=[1,1,10,1,0,0];
        SeedDefinitions(store,16,(1,[..model,0]),(2,[..model,64,0]),(3,[..model,98,0]),(4,[92,255,255,255,255,0,1,0,0,1,0]));
        store.Set(7,0,ContainerBytes(LegacyModel));
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapModelEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        async Task<GetObjectRenderAppearanceDto> Appearance(int id)=>(await client.GetFromJsonAsync<GetObjectRenderAppearanceDto>($"/api/v1/cache/types/objects/{id}/render-appearance?shape=10&rotation=0"))!;
        var casting=await Appearance(1);
        var positions=casting.Parts[0].Positions;
        var radius=(int)(Math.Sqrt(Enumerable.Range(0,positions.Length/3).Max(vertex=>(long)positions[vertex*3]*positions[vertex*3]+(long)positions[vertex*3+2]*positions[vertex*3+2]))+0.99);
        Assert.AreEqual(Math.Max(1,Math.Min(30,radius/4)),casting.ShadowStrength);
        Assert.AreEqual(0,(await Appearance(2)).ShadowStrength);
        Assert.AreEqual(15,(await Appearance(3)).ShadowStrength);
        Assert.AreEqual(15,(await Appearance(4)).ShadowStrength);
    }

    [TestMethod]
    [DataRow(true,false)]
    [DataRow(false,false)]
    [DataRow(true,true)]
    [DataRow(false,true)]
    public async Task ObjectRenderAppearance_StaticTransformHonorsSelectorFallbackAndHiddenTargets(bool selector,bool hidden)
    {
        var store=new MemoryFileStore();
        // Opcode 92: varbit, varp, fallback, one indexed target. Never choose an arbitrary visible child.
        var indexed=selector&&hidden?32767:1;
        var fallback=!selector&&hidden?32767:2;
        byte[] root=[92,255,255,selector?(byte)0:(byte)255,selector?(byte)1:(byte)255,(byte)(fallback>>8),(byte)fallback,0,(byte)(indexed>>8),(byte)indexed,0];
        SeedDefinitions(store,16,(0,root),(1,[1,1,10,1,0,0,14,2,0]),(2,[1,1,10,1,0,0,14,3,0]));
        store.Set(7,0,ContainerBytes(LegacyModel));
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapModelEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        var appearance=await client.GetFromJsonAsync<GetObjectRenderAppearanceDto>("/api/v1/cache/types/objects/0/render-appearance?shape=10&rotation=0");
        Assert.IsNotNull(appearance);Assert.AreEqual(0,appearance.Id);
        if(hidden)
        {
            Assert.HasCount(0,appearance.Parts);Assert.IsTrue(appearance.Warnings.Any(w=>w.Contains("Hidden")));
            using var response=await client.PostAsJsonAsync("/api/v1/cache/types/objects/render-appearances",new[]{new ObjectRenderAppearanceKeyDto(0,10,0)});
            var rows=await response.Content.ReadFromJsonAsync<ObjectRenderAppearanceResultDto[]>();
            Assert.IsNotNull(rows);Assert.HasCount(1,rows);Assert.AreEqual(200,rows[0].Status);Assert.IsNull(rows[0].Problem);
            var invisible=rows[0].Appearance;Assert.IsNotNull(invisible);Assert.HasCount(0,invisible.Parts);
            Assert.AreEqual(0,store.GetReadCount(7,0));
        }
        else {Assert.HasCount(1,appearance.Parts);Assert.AreEqual(selector?2:3,appearance.SizeX);Assert.IsTrue(appearance.Warnings.Any(w=>w.Contains(selector?"object 1":"object 2")));}
    }

    [TestMethod]
    public async Task ObjectRenderAppearance_OffsetsFallbackFacesWhenCombiningModelParts()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store,16,[1,1,10,2,0x80,0,0xbe,0x67,0x80,0,0xbe,0x67,0],id:45291);
        store.Set(7,48743,ContainerBytes(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","models","model-48743.dat"))));
        await using var app = await CacheServiceTestHost.StartAsync(store,group=>group.MapModelEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        var raw=await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/models/48743");
        Assert.IsNotNull(raw);
        Assert.IsNotNull(raw.FirstTexelFaces);
        Assert.HasCount(47,raw.FirstTexelFaces);
        Assert.IsTrue(raw.Warnings.Any(warning=>warning.Contains("First-texel texture fallback on 47 faces")));
        var appearance=await client.GetFromJsonAsync<GetObjectRenderAppearanceDto>("/api/v1/cache/types/objects/45291/render-appearance?shape=10&rotation=0");
        Assert.IsNotNull(appearance);
        Assert.HasCount(1,appearance.Parts);
        var part=appearance.Parts[0];
        Assert.HasCount(342*3,part.Indices);
        Assert.IsNotNull(part.FirstTexelFaces);
        CollectionAssert.AreEqual(raw.FirstTexelFaces.Concat(raw.FirstTexelFaces.Select(face=>face+171)).ToArray(),part.FirstTexelFaces);
        Assert.IsTrue(appearance.Warnings.Any(warning=>warning.Contains("First-texel texture fallback on 94 faces")));
    }

    [TestMethod]
    [DataRow(33858, 36668, 0, 0, 84, 130)]
    [DataRow(33858, 36668, 0, 1, 84, 130)]
    [DataRow(33858, 36668, 0, 2, 84, 130)]
    [DataRow(33858, 36668, 0, 3, 84, 130)]
    [DataRow(45092, 48778, 0, 0, 277, 362)]
    [DataRow(45092, 48778, 0, 1, 277, 362)]
    [DataRow(45092, 48778, 0, 2, 277, 362)]
    [DataRow(45092, 48778, 0, 3, 277, 362)]
    [DataRow(45152, 48859, 22, 0, 29, 38)]
    [DataRow(45152, 48859, 22, 1, 29, 38)]
    [DataRow(45152, 48859, 22, 2, 29, 38)]
    [DataRow(45152, 48859, 22, 3, 29, 38)]
    [DataRow(45291, 48743, 10, 0, 111, 171)]
    [DataRow(45291, 48743, 10, 1, 111, 171)]
    [DataRow(45291, 48743, 10, 2, 111, 171)]
    [DataRow(45291, 48743, 10, 3, 111, 171)]
    public async Task ObjectRenderAppearance_ReportedProjectionIsFiniteInSingleAndBatch(int objectId, int modelId, int shape, int rotation, int vertices, int faces)
    {
        var store = new MemoryFileStore();
        // Real failing model, with a minimal object definition decoded by the production provider.
        SeedDefinition(store, 16, [1, 1, (byte)shape, 1, 0x80, (byte)(modelId >> 16), (byte)(modelId >> 8), (byte)modelId, 0], id: objectId);
        store.Set(7, modelId, ContainerBytes(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "models", $"model-{modelId}.dat"))));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        using var singleResponse = await client.GetAsync($"/api/v1/cache/types/objects/{objectId}/render-appearance?shape={shape}&rotation={rotation}");
        Assert.AreEqual(System.Net.HttpStatusCode.OK, singleResponse.StatusCode, await singleResponse.Content.ReadAsStringAsync());
        var single = await singleResponse.Content.ReadFromJsonAsync<GetObjectRenderAppearanceDto>();
        Assert.IsNotNull(single);
        Assert.AreEqual(objectId, single.Id);
        Assert.AreEqual(shape, single.Shape);
        Assert.AreEqual(rotation, single.Rotation);
        Assert.HasCount(1, single.Parts);
        var part = single.Parts[0];
        Assert.HasCount(vertices * 3, part.Positions);
        Assert.HasCount(faces * 3, part.Indices);
        Assert.IsNotNull(part.FaceTextureIds);
        var affectedFace = modelId == 36668 ? 10 : modelId == 48778 ? 40 : 31;
        Assert.IsTrue(part.FaceTextureIds[affectedFace] >= 0);
        Assert.IsNotNull(part.FaceTextureUvs);
        Assert.HasCount(part.Indices.Length * 2, part.FaceTextureUvs);
        Assert.IsTrue(part.FaceTextureUvs.All(float.IsFinite));
        if (modelId == 48743)
        {
            Assert.IsNotNull(part.FirstTexelFaces);
            Assert.HasCount(47, part.FirstTexelFaces);
            CollectionAssert.Contains(part.FirstTexelFaces, 31);
            Assert.IsTrue(single.Warnings.Any(warning => warning.Contains("First-texel texture fallback on 47 faces")));
            Assert.IsTrue(part.FaceTextureUvs.Take(6).Distinct().Count() > 1, "Valid faces must retain real UVs.");
        }
        else Assert.IsTrue(part.FaceTextureUvs.Skip(affectedFace * 6).Take(6).Distinct().Count() > 1, "The affected wall face must retain real UVs.");

        using var batchResponse = await client.PostAsJsonAsync("/api/v1/cache/types/objects/render-appearances", new[] { new ObjectRenderAppearanceKeyDto(objectId, shape, rotation) });
        Assert.AreEqual(System.Net.HttpStatusCode.OK, batchResponse.StatusCode, await batchResponse.Content.ReadAsStringAsync());
        var rows = await batchResponse.Content.ReadFromJsonAsync<ObjectRenderAppearanceResultDto[]>();
        Assert.IsNotNull(rows);
        Assert.HasCount(1, rows);
        Assert.AreEqual(200, rows[0].Status);
        Assert.IsNull(rows[0].Problem);
        var batchAppearance = rows[0].Appearance;
        Assert.IsNotNull(batchAppearance);
        Assert.AreEqual(objectId, batchAppearance.Id);
        Assert.AreEqual(shape, batchAppearance.Shape);
        Assert.AreEqual(rotation, batchAppearance.Rotation);
        Assert.HasCount(1, batchAppearance.Parts);
        var batchPart = batchAppearance.Parts[0];
        CollectionAssert.AreEqual(part.Positions, batchPart.Positions);
        CollectionAssert.AreEqual(part.Indices, batchPart.Indices);
        CollectionAssert.AreEqual(part.FaceTextureIds, batchPart.FaceTextureIds);
        CollectionAssert.AreEqual(part.FaceTextureUvs, batchPart.FaceTextureUvs);
        CollectionAssert.AreEqual(part.FirstTexelFaces, batchPart.FirstTexelFaces);
        CollectionAssert.AreEqual(single.Warnings, batchAppearance.Warnings);
    }

    [TestMethod]
    [DataRow(10, 10, 1)]
    [DataRow(2, 2, 0)]
    [DataRow(8, 4, 2)]
    [DataRow(6, 4, 3)]
    [DataRow(11, 10, 2)]
    public async Task AppearanceBatch_MatchesSingleMirroredWallsDecorationsAndContourMetadata(int shape, int modelShape, int rotation)
    {
        var store = new MemoryFileStore();
        var definition = Definition("objects").SkipLast(1).Concat(new byte[] { 62, 21, 2, 70, 101, 110, 99, 101, 0, 0 }).ToArray();
        definition[2] = (byte)modelShape;
        SeedDefinition(store, 16, definition);
        store.Set(7, 0, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/objects/render-appearances", new[] { new ObjectRenderAppearanceKeyDto(0, shape, rotation) });
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<ObjectRenderAppearanceResultDto[]>();
        Assert.IsNotNull(rows); Assert.HasCount(1, rows);
        Assert.AreEqual(200, rows[0].Status);
        Assert.AreEqual(await client.GetStringAsync($"/api/v1/cache/types/objects/0/render-appearance?shape={shape}&rotation={rotation}"),
            System.Text.Json.JsonSerializer.Serialize(rows[0].Appearance, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
        Assert.AreEqual(1, rows[0].Appearance?.ContourMode);
        Assert.AreEqual("Fence", rows[0].Appearance?.Name);
    }

    [TestMethod]
    public async Task AppearanceBatch_RequiresAuthenticationAndInheritsCachePolicy()
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapGroup("").RequireAuthorization(CacheAuthorization.PolicyName).MapModelEndpoints(), services => {
            services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme).AddBearerToken();
            services.AddAuthorization(options => options.AddPolicy(CacheAuthorization.PolicyName, policy => policy.RequireAuthenticatedUser()));
        });
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/objects/render-appearances", new[] { new ObjectRenderAppearanceKeyDto(0, 10, 0) });
        Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        var endpoint = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().Single(endpoint => endpoint.RoutePattern.RawText!.EndsWith("/render-appearances", StringComparison.Ordinal));
        Assert.IsTrue(endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(policy => policy.Policy == CacheAuthorization.PolicyName));
    }

    [TestMethod]
    public async Task AppearanceBatch_CancellationStopsBeforeTheNextKey()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new CancellingObjects(cancellation);
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapModelEndpoints());
        var endpoint = app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().Single(endpoint => endpoint.RoutePattern.RawText!.EndsWith("/render-appearances", StringComparison.Ordinal));
        var handler = endpoint.Metadata.GetMetadata<MethodInfo>();
        Assert.IsNotNull(handler);
        var error = Assert.ThrowsExactly<TargetInvocationException>(() => handler.Invoke(null, new object[] {
            new[] { new ObjectRenderAppearanceKeyDto(0, 10, 0), new ObjectRenderAppearanceKeyDto(1, 10, 0) },
            app.Services.GetRequiredService<ICacheAPI>(), provider, cancellation.Token }));
        Assert.IsInstanceOfType<OperationCanceledException>(error.InnerException);
        Assert.AreEqual(1, provider.Reads);
    }

    private sealed class CancellingObjects(CancellationTokenSource cancellation) : ITypeProvider<IObjectType>
    {
        public int Reads { get; private set; }
        public int ArchiveSize => 2;
        public IObjectType Get(int id) { Reads++; cancellation.Cancel(); return new ObjectType(id); }
        public IObjectType[] GetAll() => throw new NotSupportedException();
        public IObjectType[] GetRange(int start, int end) => throw new NotSupportedException();
    }

    [TestMethod]
    public async Task AppearanceBatch_MapsPerKeyExceptionsWithoutDiscardingOtherResults()
    {
        var store = new MemoryFileStore();
        store.Set(7, 0, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints(), services => services.AddSingleton<ITypeProvider<IObjectType>, FaultingObjects>());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/objects/render-appearances", new[] {
            new ObjectRenderAppearanceKeyDto(0, 10, 0), new ObjectRenderAppearanceKeyDto(1, 10, 0), new ObjectRenderAppearanceKeyDto(2, 10, 0) });
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<ObjectRenderAppearanceResultDto[]>();
        Assert.IsNotNull(rows);
        CollectionAssert.AreEqual(new[] { 404, 200, 422 }, rows.Select(row => row.Status).ToArray());
        Assert.IsNotNull(rows[1].Appearance);
        Assert.AreEqual("Cache file not found.", rows[0].Problem?.Title);
        Assert.AreEqual("Unsupported cache data.", rows[2].Problem?.Title);
    }

    private sealed class FaultingObjects : ITypeProvider<IObjectType>
    {
        public int ArchiveSize => 3;
        public IObjectType Get(int id)
        {
            if (id == 0) throw new FileNotFoundException("Missing definition");
            if (id == 2) throw new NotSupportedException("Unsupported definition");
            using var bytes = new MemoryStream(Definition("objects"));
            return new ObjectTypeCodec().Decode(id, bytes);
        }
        public IObjectType[] GetAll() => throw new NotSupportedException();
        public IObjectType[] GetRange(int start, int end) => throw new NotSupportedException();
    }

    [TestMethod]
    public async Task AppearanceBatch_PreservesSingleResultsDeduplicatesAndKeepsPartialFailures()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, 16, Definition("objects"));
        store.Set(7, 0, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var keys = new[] { new ObjectRenderAppearanceKeyDto(0, 10, 1), new ObjectRenderAppearanceKeyDto(0, 2, 0), new ObjectRenderAppearanceKeyDto(0, 10, 1) };
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/objects/render-appearances", keys);
        response.EnsureSuccessStatusCode();
        var rows = await response.Content.ReadFromJsonAsync<ObjectRenderAppearanceResultDto[]>();
        Assert.IsNotNull(rows);
        Assert.HasCount(2, rows);
        Assert.AreEqual(200, rows[0].Status);
        var single = await client.GetStringAsync("/api/v1/cache/types/objects/0/render-appearance?shape=10&rotation=1");
        Assert.AreEqual(single, System.Text.Json.JsonSerializer.Serialize(rows[0].Appearance, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
        Assert.AreEqual(404, rows[1].Status);
        Assert.IsNull(rows[1].Appearance);
        Assert.AreEqual(404, rows[1].Problem?.Status);
        Assert.AreEqual(2, rows[1].Shape);
        Assert.AreEqual(2, store.GetReadCount(7, 0)); // one deduplicated batch read plus the comparison read
    }

    [TestMethod]
    [DataRow("[]")]
    [DataRow("[null]")]
    [DataRow("[{\"id\":-1,\"shape\":10,\"rotation\":0}]")]
    [DataRow("[{\"id\":0,\"shape\":23,\"rotation\":0}]")]
    [DataRow("[{\"id\":0,\"shape\":10,\"rotation\":4}]")]
    public async Task AppearanceBatch_RejectsInvalidInputBeforeDecoding(string json)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsync("/api/v1/cache/types/objects/render-appearances", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task AppearanceBatch_RejectsOversizedInputBeforeDecoding()
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.PostAsJsonAsync("/api/v1/cache/types/objects/render-appearances", Enumerable.Repeat(new ObjectRenderAppearanceKeyDto(0, 10, 0), 33).ToArray());
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static readonly byte[] LegacyModel = [0, 1, 3, 1, 2, 64, 65, 65, 0, 42, 0, 0, 0, 1, 0, 2, 74, 54, 84,
            0, 3, 0, 1, 1, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
    private static readonly byte[] ModernSkinnedModel = [0, 1, 3, 1, 1, 2, 3, 64, 65, 65, 0x12, 0x34, 74, 54, 84, 13,
        0, 3, 0, 1, 0, 8, 0, 0, 0, 0, 1, 0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255];

    [TestMethod]
    [DataRow("items", 19, 113, 114, 738)]
    [DataRow("npcs", 18, 100, 101, 820)]
    [DataRow("objects", 16, 29, 39, 820)]
    public async Task DefinitionModel_PreservesEffectiveClientLighting(string kind, int index, int ambientOpcode, int contrastOpcode, int contrast)
    {
        var store = new MemoryFileStore();
        var definition = Definition(kind);
        SeedDefinition(store, index, definition.Take(definition.Length - 1).Concat(new byte[] {(byte)ambientOpcode, 12, (byte)contrastOpcode, 250, 0}).ToArray());
        var geometry = (byte[])LegacyModel.Clone();
        geometry[4] = 3; // legacy flat shading plus existing texture flag
        store.Set(7, 0, ContainerBytes(geometry));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var result = await client.GetFromJsonAsync<GetModelDto>($"/api/v1/cache/types/{kind}/0/model");
        Assert.IsNotNull(result);
        Assert.AreEqual(76, result.Ambient);
        Assert.AreEqual(contrast, result.Contrast);
        CollectionAssert.AreEqual(new[] { 1 }, result.FaceShadingTypes);
        CollectionAssert.AreEqual(new[] { 60000 }, result.FaceTextureIds);
        StringAssert.Contains(result.Warnings[0], "fixed inspection light");
    }

    [TestMethod]
    [DataRow("items", 19)]
    [DataRow("npcs", 18)]
    [DataRow("objects", 16)]
    public async Task DefinitionModel_AppliesScaleAndOrderedUnsignedAppearancePairs(string kind, int index)
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, index, Definition(kind));
        store.Set(7, 0, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var result = await client.GetFromJsonAsync<GetModelDto>($"/api/v1/cache/types/{kind}/0/model");

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 60000 }, result.FaceTextureIds);
        CollectionAssert.AreEqual(new[] { 50000 }, result.FaceColors);
        CollectionAssert.AreEqual(kind == "npcs" ? new[] { 0, 0, 0, 40, 0, 0, 0, 80, 0 } : new[] { 0, 0, 0, 80, 0, 0, 0, 40, 0 }, result.Positions);
        if (kind == "npcs") CollectionAssert.AreEqual(new float[] { 2, 0.5f, 2 }, result.Scale);
        else Assert.IsNull(result.Scale);
        Assert.AreEqual(1f, result.CoordinateScale);
        Assert.IsNull(result.VertexSkins, "A definition with no skinned vertices must not offer manual animation posing.");
        CollectionAssert.AreEqual(new float[] { 0, 0, 1, 0, 0, 1 }, result.FaceTextureUvs);

        var raw = await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/models/0");
        Assert.IsNotNull(raw);
        Assert.AreEqual(0.25f, raw.CoordinateScale);
        CollectionAssert.AreEqual(new[] { 42 }, raw.FaceTextureIds);
        CollectionAssert.AreEqual(new[] { 0 }, raw.FacePriorities);
        Assert.AreEqual(64, raw.Ambient); Assert.AreEqual(850, raw.Contrast);
        StringAssert.Contains(raw.Warnings[0], "client priorities");
        CollectionAssert.AreEqual(new[] { 127 }, raw.FaceColors);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 10, 0, 0, 0, 20, 0 }, raw.Positions);
    }

    [TestMethod]
    public async Task NpcModel_MixedVersionsNormalizeCoordinatesAndPreserveSkinLabels()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, 18, new byte[] { 1, 2, 0, 0, 0, 1 }.Concat(Definition("npcs").Skip(4)).ToArray());
        store.Set(7, 0, ContainerBytes(LegacyModel));
        var first = (byte[])LegacyModel.Clone();
        var second = (byte[])ModernSkinnedModel.Clone();
        first[first.Length - 18 + 6] = 10;
        second[second.Length - 23 + 6] = 11;
        store.Set(7, 0, ContainerBytes(first));
        store.Set(7, 1, ContainerBytes(second));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var result = await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/types/npcs/0/model");

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 40, 0, 0, 0, 80, 0, 0, 0, 0, 10, 0, 0, 0, 20, 0 }, result.Positions);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5 }, result.Indices);
        CollectionAssert.AreEqual(new[] { -1, -1, -1, 1, 2, 3 }, result.VertexSkins);
        CollectionAssert.AreEqual(new[] { 60000, -1 }, result.FaceTextureIds);
        CollectionAssert.AreEqual(new[] { 0, 1, 2, -1, -1, -1 }, result.FaceTextureVertices);
        Assert.HasCount(12, result.FaceTextureUvs!);
        CollectionAssert.AreEqual(new[] { 10, 11 }, result.FacePriorities);
    }

    [TestMethod]
    public async Task ObjectModel_UsesRequestedShapeGroupAndRejectsAbsentShapes()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, 16, new byte[] { 1, 2, 10, 1, 0, 0, 20, 1, 0, 1 }.Concat(Definition("objects").Skip(6)).ToArray());
        store.Set(7, 0, ContainerBytes(LegacyModel));
        store.Set(7, 1, ContainerBytes(ModernSkinnedModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);

        var result = await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/types/objects/0/model?shape=20");

        Assert.IsNotNull(result);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 20, 0, 0, 0, 10, 0 }, result.Positions);
        CollectionAssert.AreEqual(new[] { 0x1234 }, result.FaceColors);
        using var absent = await client.GetAsync("/api/v1/cache/types/objects/0/model?shape=22");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, absent.StatusCode);
    }

    [TestMethod]
    [DataRow("male-equipped", 3, 4, -8, 12, 40)]
    [DataRow("female-equipped", 3, -4, 8, -12, 10)]
    [DataRow("male-head", 2, 0, 0, 0, 40)]
    [DataRow("female-head", 2, 0, 0, 0, 10)]
    public async Task ItemAppearance_AssemblesSelectedPartsWithoutInventoryScale(string appearance, int parts, int x, int y, int z, int firstWidth)
    {
        var store = new MemoryFileStore();
        byte[] equipment = [23,0,0, 24,0,1, 78,0,2, 25,0,1, 26,0,0, 79,0,2,
            90,0,0, 92,0,1, 91,0,1, 93,0,0, 125,1,254,3, 126,255,2,253];
        SeedDefinition(store, 19, equipment.Concat(Definition("items")).ToArray());
        store.Set(7, 0, ContainerBytes(LegacyModel));
        store.Set(7, 1, ContainerBytes(ModernSkinnedModel));
        store.Set(7, 2, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var metadata = await client.GetFromJsonAsync<GetItemModelAppearanceDto[]>("/api/v1/cache/types/items/0/model-appearances");
        Assert.IsNotNull(metadata); Assert.HasCount(5, metadata);
        var selected = metadata.Single(value => value.Key == appearance);
        Assert.IsTrue(selected.Available); Assert.HasCount(parts, selected.ModelIds);
        CollectionAssert.AreEqual(appearance switch {
            "male-equipped" => new[] {0,1,2}, "female-equipped" => new[] {1,0,2},
            "male-head" => new[] {0,1}, _ => new[] {1,0}}, selected.ModelIds);
        var result = await client.GetFromJsonAsync<GetModelDto>($"/api/v1/cache/types/items/0/model?appearance={appearance}");
        Assert.IsNotNull(result); Assert.HasCount(parts * 9, result.Positions);
        CollectionAssert.AreEqual(new[] {x,y,z, x+firstWidth,y,z}, result.Positions.Take(6).ToArray());
        for (var part = 0; part < parts; part++)
            CollectionAssert.AreEqual(new[] {x,y,z}, result.Positions.Skip(part * 9).Take(3).ToArray(), "Wear translation applies once to every assembled part.");
        CollectionAssert.AreEqual(Enumerable.Range(0, parts*3).ToArray(), result.Indices);
        Assert.AreEqual(parts, result.FaceColors.Length);
        Assert.IsTrue(result.FaceColors.Contains(50000));
        Assert.IsTrue(result.FaceTextureIds!.Contains(60000));
        Assert.AreEqual(1f, result.CoordinateScale);
        Assert.AreEqual(x, selected.OffsetX); Assert.AreEqual(y, selected.OffsetY); Assert.AreEqual(z, selected.OffsetZ);
    }

    [TestMethod]
    public async Task ItemAppearances_RejectAbsentPrimaryAndIgnoreOrphanTertiary()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, 19, new byte[] {23,0,0, 78,0,2, 26,0,1}.Concat(Definition("items")).ToArray());
        store.Set(7,0,ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var metadata = await client.GetFromJsonAsync<GetItemModelAppearanceDto[]>("/api/v1/cache/types/items/0/model-appearances");
        Assert.IsNotNull(metadata);
        CollectionAssert.AreEqual(new[] {0}, metadata.Single(value => value.Key == "male-equipped").ModelIds);
        Assert.IsFalse(metadata.Single(value => value.Key == "female-equipped").Available);
        var male = await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/types/items/0/model?appearance=male-equipped");
        Assert.IsNotNull(male); Assert.HasCount(9, male.Positions);
        using var absent = await client.GetAsync("/api/v1/cache/types/items/0/model?appearance=female-equipped");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, absent.StatusCode);
        using var invalid = await client.GetAsync("/api/v1/cache/types/items/0/model?appearance=other");
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [TestMethod]
    public async Task ObjectRenderAppearance_RotatesBeforeScaleAndAppliesDecodedOffsetsOnce()
    {
        var store = new MemoryFileStore();
        SeedDefinition(store, 16, Definition("objects").SkipLast(1).Concat(new byte[] { 70, 0, 3, 21, 0 }).ToArray());
        store.Set(7, 0, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var appearance = await client.GetFromJsonAsync<GetObjectRenderAppearanceDto>("/api/v1/cache/types/objects/0/render-appearance?shape=10&rotation=1");
        Assert.IsNotNull(appearance);
        Assert.AreEqual(1, appearance.ContourMode);
        Assert.AreEqual(64, appearance.DecorDisplacement);
        Assert.HasCount(1, appearance.Parts);
        CollectionAssert.AreEqual(new[] { 12, 0, 0, 12, 0, -40, 12, 40, 0 }, appearance.Parts[0].Positions);
        CollectionAssert.AreEqual(new[] { 50000 }, appearance.Parts[0].FaceColors);
        // A later raw read must remain untransformed.
        var raw = await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/models/0");
        Assert.IsNotNull(raw);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 10, 0, 0, 0, 20, 0 }, raw.Positions);
    }

    [TestMethod]
    public async Task ObjectRenderAppearance_CombinesCornerWallPartsAndPreservesMirroredUvs()
    {
        var store = new MemoryFileStore();
        var definition = Definition("objects");
        definition[2] = 2;
        SeedDefinition(store, 16, definition);
        store.Set(7, 0, ContainerBytes(LegacyModel));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var appearance = await client.GetFromJsonAsync<GetObjectRenderAppearanceDto>("/api/v1/cache/types/objects/0/render-appearance?shape=2&rotation=0");
        Assert.IsNotNull(appearance);
        Assert.HasCount(2, appearance.Parts);
        CollectionAssert.AreEqual(new[] { 0, 2, 1 }, appearance.Parts[0].Indices);
        CollectionAssert.AreEqual(new float[] { 0, 0, 0, 1, 1, 0 }, appearance.Parts[0].FaceTextureUvs);
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 0, 0, -40, 0, 40, 0 }, appearance.Parts[1].Positions);
    }

    [TestMethod]
    [DataRow(-1, 10, 0)]
    [DataRow(0, 23, 0)]
    [DataRow(0, 10, 4)]
    public async Task ObjectRenderAppearance_RejectsInvalidPlacementBeforeReadingAssets(int id, int shape, int rotation)
    {
        await using var app = await CacheServiceTestHost.StartAsync(new MemoryFileStore(), group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        using var response = await client.GetAsync($"/api/v1/cache/types/objects/{id}/render-appearance?shape={shape}&rotation={rotation}");
        Assert.AreEqual(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static byte[] Definition(string kind)
    {
        using var stream = new MemoryStream();
        if (kind == "items") stream.Write(new byte[] { 1, 0, 0 });
        else if (kind == "npcs") stream.Write(new byte[] { 1, 1, 0, 0 });
        else stream.Write(new byte[] { 1, 1, 10, 1, 0, 0 });
        stream.WriteByte(40); stream.WriteByte(2);
        Short(127); Short(300); Short(300); Short(50000);
        stream.WriteByte(41); stream.WriteByte(2);
        Short(42); Short(50000); Short(50000); Short(60000);
        stream.WriteByte((byte)(kind == "items" ? 110 : kind == "npcs" ? 97 : 65)); Short(256);
        stream.WriteByte((byte)(kind == "items" ? 111 : kind == "npcs" ? 98 : 66)); Short(64);
        stream.WriteByte(0);
        return stream.ToArray();

        void Short(int value) { stream.WriteByte((byte)(value >> 8)); stream.WriteByte((byte)value); }
    }

    private static void SeedDefinition(MemoryFileStore store, int index, byte[] data, int id = 0)
        => SeedDefinitions(store,index,(id,data));

    private static void SeedDefinitions(MemoryFileStore store, int index, params (int Id,byte[] Data)[] definitions)
    {
        var table = new ReferenceTable(7, 1, (ReferenceTableFlags)0, 1);
        foreach(var group in definitions.GroupBy(definition=>definition.Id>>8))
        {
            var members=group.OrderBy(definition=>definition.Id).ToArray();
            var archive=new ReferenceTableEntry(group.Key);archive.InitializeEntries(members.Length);
            foreach(var member in members)archive.AddEntry(member.Id&255,new ReferenceTableChildEntry(member.Id&255));
            table.AddEntry(group.Key,archive);
            using var bytes=new MemoryStream();
            foreach(var member in members)bytes.Write(member.Data);
            if(members.Length>1)
            {
                var previous=0;
                foreach(var member in members) {var delta=new byte[4];System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(delta,member.Data.Length-previous);bytes.Write(delta);previous=member.Data.Length;}
                bytes.WriteByte(1);
            }
            store.Set(index,group.Key,ContainerBytes(bytes.ToArray()));
        }
        using var encoded = new ReferenceTableCodec().Encode(table);
        store.Set(255, index, ContainerBytes(encoded.ToArray()));
    }

    private static byte[] ContainerBytes(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }
}
