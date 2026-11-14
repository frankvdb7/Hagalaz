using Hagalaz.Services.Cache.Features.Models.Dtos;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Models;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;
[TestClass]
public class ModelParticleEndpointsTests
{
    [TestMethod]
    [DataRow("valid")]
    [DataRow("missing")]
    [DataRow("unsupported")]
    [DataRow("truncated")]
    public async Task RawCape_RetainsAttachmentsAndReportsEmitterAvailability(string state)
    {
        var store=new MemoryFileStore();
        store.Set(7,65297,ContainerBytes(Fixture("model-65297.dat")));
        if(state!="missing") Seed(store,27,403,state switch { "valid" => Fixture("emitter-403.dat"), "truncated" => [9, 2, 0, 17, 0], _ => [99, 0] }, includeConstant: state == "valid");
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapModelEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        var result=await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/models/65297");
        Assert.IsNotNull(result);Assert.HasCount(7,result.Particles!);
        CollectionAssert.AreEqual(Enumerable.Range(785,7).ToArray(),result.Particles!.Select(p=>p.Face).ToArray());
        foreach(var particle in result.Particles!)
        {
            Assert.AreEqual(403,particle.EmitterId);
            if (state == "valid")
            {
                Assert.IsNotNull(particle.Definition, particle.Warning);
                Assert.AreEqual(744, particle.Definition.TextureId);
                CollectionAssert.AreEqual(new[] { 6 }, particle.Definition.ConstantEffectorIds);
                Assert.AreEqual(984, particle.Definition.ConstantEffectors!.Single().Y);
                var dto = System.Text.Json.JsonSerializer.SerializeToNode(particle.Definition)!.AsObject();
                dto.Remove(nameof(ParticleEmitterDto.ConstantEffectors));
                Assert.IsTrue(System.Text.Json.Nodes.JsonNode.DeepEquals(
                    System.Text.Json.JsonSerializer.SerializeToNode(ExpectedEmitter(Fixture("emitter-403.dat"))), dto));
                Assert.IsNull(particle.Warning);
            }
            else {Assert.IsNull(particle.Definition);StringAssert.Contains(particle.Warning,"is unavailable");}
        }
    }
    [TestMethod]
    public async Task ItemAssembly_RemapsCapeAttachmentFacesAcrossParts()
    {
        var store=new MemoryFileStore();
        Seed(store,19,0,[23,0,0,24,0,1,0]);
        Seed(store,27,403,Fixture("emitter-403.dat"));
        var bytes=Fixture("model-65297.dat");
        store.Set(7,0,ContainerBytes(bytes));store.Set(7,1,ContainerBytes(bytes));
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapModelEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        var result=await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/types/items/0/model?appearance=male-equipped");
        Assert.IsNotNull(result);Assert.HasCount(14,result.Particles!);
        var faces=new ModelTypeCodec().Decode(0, new MemoryStream(bytes)).FaceColors.Length;
        CollectionAssert.AreEqual(Enumerable.Range(785,7).Concat(Enumerable.Range(faces+785,7)).ToArray(),result.Particles!.Select(p=>p.Face).ToArray());
    }

    [TestMethod]
    public async Task ItemAssembly_RemapsEffectorVerticesAndResolvesSparseDefinitions()
    {
        var store = new MemoryFileStore();
        Seed(store, 19, 0, [23, 0, 0, 24, 0, 1, 0]);
        Seed(store, 27, 17, [3, 0, 1, 64, 0, 0, 1, 64, 0, 0, 1, 64, 0, 8, 9, 10, 0], archiveId: 1);
        byte[] bytes = [
            0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 1, 0, 17, 0, 2,
            0, 3, 0, 1, 0, 2, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255,
        ];
        store.Set(7, 0, ContainerBytes(bytes));
        store.Set(7, 1, ContainerBytes(bytes));
        await using var app = await CacheServiceTestHost.StartAsync(store, group => group.MapModelEndpoints());
        using var client = CacheServiceTestHost.CreateClient(app);
        var result = await client.GetFromJsonAsync<GetModelDto>("/api/v1/cache/types/items/0/model?appearance=male-equipped");
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Effectors);
        CollectionAssert.AreEqual(new[] { 2, 5 }, result.Effectors.Select(effector => effector.Vertex).ToArray());
        foreach (var effector in result.Effectors)
        {
            Assert.AreEqual(17, effector.EffectorId);
            Assert.IsNull(effector.Warning);
            Assert.AreEqual(new ParticleEffectorDto(81920, 81920, 81920, 0, 0, 1, 0, true, true, true), effector.Definition);
        }
    }
    private static byte[] Fixture(string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures/particles",name));
    private static byte[] ContainerBytes(byte[] data) {using var container=new Container(new MemoryStream(data));return container.Encode();}
    private static void Seed(MemoryFileStore store,int index,int child,byte[] data,int archiveId=0,bool includeConstant=false)
    {
        var table=new ReferenceTable(7,1,(ReferenceTableFlags)0,(includeConstant ? 2 : archiveId+1));var archive=new ReferenceTableEntry(archiveId);
        archive.InitializeEntries(1);archive.AddEntry(child,new ReferenceTableChildEntry(0));table.AddEntry(archiveId,archive);
        if (includeConstant)
        {
            var constants = new ReferenceTableEntry(1);
            constants.InitializeEntries(1);
            constants.AddEntry(6, new ReferenceTableChildEntry(0));
            table.AddEntry(1, constants);
            store.Set(index, 1, ContainerBytes([3, 0, 0, 0, 0, 0, 0, 3, 216, 0, 0, 0, 0, 6, 2, 0]));
        }
        using var encoded=new ReferenceTableCodec().Encode(table);
        store.Set(255,index,ContainerBytes(encoded.ToArray()));store.Set(index,archiveId,ContainerBytes(data));
    }

    private static object ExpectedEmitter(byte[] data)
    {
        var e = new Hagalaz.Cache.Logic.Codecs.ParticleEmitterTypeCodec().Decode(403, new MemoryStream(data));
        return new
        {
            TextureId = e.RenderTextureId, e.ColorMin, e.ColorMax, e.LifeMin, e.LifeMax, e.RateMin, e.RateMax, e.SizeMin, e.SizeMax, e.SpeedMin, e.SpeedMax,
            Angles = e.RenderAngles, e.UnsupportedOpcodes, e.LocalEffectorIds, e.SpeedDrag, e.ConstantEffectorIds, e.SizeOverLife, e.SpeedOverLife, e.ColorOverLife,
        };
    }
}
