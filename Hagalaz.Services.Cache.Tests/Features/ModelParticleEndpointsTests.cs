using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Services.Cache.Features;
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
    public async Task RawCape_RetainsAttachmentsAndReportsEmitterAvailability(string state)
    {
        var store=new MemoryFileStore();
        store.Set(7,65297,ContainerBytes(Fixture("model-65297.dat")));
        if(state!="missing") Seed(store,27,403,state=="valid"?Fixture("emitter-403.dat"):[99,0]);
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapModelEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        var result=await client.GetFromJsonAsync<ModelEndpoints.ModelResponse>("/api/v1/cache/models/65297");
        Assert.IsNotNull(result);Assert.HasCount(7,result.Particles!);
        CollectionAssert.AreEqual(Enumerable.Range(785,7).ToArray(),result.Particles!.Select(p=>p.Face).ToArray());
        foreach(var particle in result.Particles)
        {
            Assert.AreEqual(403,particle.EmitterId);
            if(state=="valid") {Assert.IsNotNull(particle.Definition,particle.Warning);Assert.AreEqual(744,particle.Definition.TextureId);Assert.IsNull(particle.Warning);}
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
        var result=await client.GetFromJsonAsync<ModelEndpoints.ModelResponse>("/api/v1/cache/types/items/0/model?appearance=male-equipped");
        Assert.IsNotNull(result);Assert.HasCount(14,result.Particles!);
        var faces=ModelGeometryDecoder.Decode(bytes).FaceColors.Length;
        CollectionAssert.AreEqual(Enumerable.Range(785,7).Concat(Enumerable.Range(faces+785,7)).ToArray(),result.Particles!.Select(p=>p.Face).ToArray());
    }
    private static byte[] Fixture(string name)=>File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures/particles",name));
    private static byte[] ContainerBytes(byte[] data) {using var container=new Container(new MemoryStream(data));return container.Encode();}
    private static void Seed(MemoryFileStore store,int index,int child,byte[] data)
    {
        var table=new ReferenceTable(7,1,(ReferenceTableFlags)0,1);var archive=new ReferenceTableEntry(0);
        archive.InitializeEntries(1);archive.AddEntry(child,new ReferenceTableChildEntry(0));table.AddEntry(0,archive);
        using var encoded=new ReferenceTableCodec().Encode(table);
        store.Set(255,index,ContainerBytes(encoded.ToArray()));store.Set(index,0,ContainerBytes(data));
    }
}
