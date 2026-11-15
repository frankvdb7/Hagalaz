using System.Net;
using System.Net.Http.Json;
using Hagalaz.Cache.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Hagalaz.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;
[TestClass]
public class ItemDefinitionMutationTests
{
    [TestMethod]
    public async Task SaveFullDefinition_ChangesModelFieldsAndPreservesLegacyBytes()
    {
        var original=new byte[]{2,(byte)'O',(byte)'l',(byte)'d',0,250,7,0};
        var store=Seed(0,original);
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapTypeEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        var definitionJson = await client.GetStringAsync("/api/v1/cache/types/items/0/definition");
        Assert.IsFalse(definitionJson.Contains("preservedOpcodes", StringComparison.OrdinalIgnoreCase));
        var item=await client.GetFromJsonAsync<UpdateItemDefinitionDto>("/api/v1/cache/types/items/0/definition");
        Assert.IsNotNull(item);item=item with{Name="Updated",Value=789,MaleHeadModel=60000,ScaleX=256,MembersOnly=true,
            ExtraData=new Dictionary<int,object>{{451,42},{452,"text"}}};
        using var result=await client.PutAsJsonAsync("/api/v1/cache/types/items/0/definition",item);
        Assert.AreEqual(HttpStatusCode.OK,result.StatusCode,await result.Content.ReadAsStringAsync());
        var saved=await client.GetFromJsonAsync<GetItemDefinitionDto>("/api/v1/cache/types/items/0/definition");
        Assert.IsNotNull(saved);Assert.AreEqual("Updated",saved.Name);Assert.AreEqual(789,saved.Value);Assert.AreEqual(60000,saved.MaleHeadModel);Assert.AreEqual(256,saved.ScaleX);Assert.IsTrue(saved.MembersOnly);
        using var raw=app.Services.GetRequiredService<ICacheAPI>().Read(19,0,0);
        Assert.IsTrue(raw.ToArray().AsSpan().IndexOf(new byte[]{250,7})>=0);

    }

    [TestMethod]
    [DataRow(0,1)]
    [DataRow(255,256)]
    public async Task Duplicate_PreservesSourceAndCreatesExactDefinitionAcrossArchiveBoundary(int source,int target)
    {
        byte[] bytes=[2,(byte)'C',(byte)'a',(byte)'p',(byte)'e',0,250,5,0];
        var store=Seed(source,bytes);
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapTypeEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        using var response=await client.PostAsJsonAsync($"/api/v1/cache/types/items/{source}/duplicate",new{});
        Assert.AreEqual(HttpStatusCode.Created,response.StatusCode,await response.Content.ReadAsStringAsync());
        var duplicate=await response.Content.ReadFromJsonAsync<DuplicateItemDefinitionResultDto>();
        Assert.IsNotNull(duplicate);Assert.AreEqual(target,duplicate.Id);
        var old=await client.GetFromJsonAsync<GetItemDefinitionDto>($"/api/v1/cache/types/items/{source}/definition");
        var copy=await client.GetFromJsonAsync<GetItemDefinitionDto>($"/api/v1/cache/types/items/{target}/definition");
        Assert.IsNotNull(old);Assert.IsNotNull(copy);Assert.AreEqual("Cape",old.Name);Assert.AreEqual(old.Name,copy.Name);Assert.AreEqual(target,copy.Id);
        var projected=await client.GetFromJsonAsync<GetItemTypeDto>($"/api/v1/cache/types/items/{target}");
        Assert.IsNotNull(projected);Assert.AreEqual("Cape",projected.Name);
        var cache=app.Services.GetRequiredService<ICacheAPI>();
        using var sourceBytes=cache.Read(19,source>>8,cache.ReadReferenceTable(19).GetEntry(source>>8)!.GetEntry(source&255)!.Index);
        using var targetBytes=cache.Read(19,target>>8,cache.ReadReferenceTable(19).GetEntry(target>>8)!.GetEntry(target&255)!.Index);
        CollectionAssert.AreEqual(bytes,sourceBytes.ToArray());CollectionAssert.AreEqual(bytes,targetBytes.ToArray());
        using var second=await client.PostAsJsonAsync($"/api/v1/cache/types/items/{source}/duplicate",new{});
        var another=await second.Content.ReadFromJsonAsync<DuplicateItemDefinitionResultDto>();
        Assert.IsNotNull(another);Assert.AreEqual(target+1,another.Id);
    }

    [TestMethod]
    public async Task SaveFullDefinition_RejectsInvalidInputWithoutMutatingSource()
    {
        var store=Seed(0,[2,(byte)'A',0,0]);
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapTypeEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        foreach(var item in new[]{new UpdateItemDefinitionDto{Id=0,Name="",Value=0},new UpdateItemDefinitionDto{Id=0,Name="Valid",Value=-1},new UpdateItemDefinitionDto{Id=1,Name="Wrong ID"},new UpdateItemDefinitionDto{Id=0,Name="Invalid ☃"}})
        {
            using var response=await client.PutAsJsonAsync("/api/v1/cache/types/items/0/definition",item);
            Assert.AreEqual(HttpStatusCode.BadRequest,response.StatusCode);
        }
        var saved=await client.GetFromJsonAsync<GetItemDefinitionDto>("/api/v1/cache/types/items/0/definition");Assert.IsNotNull(saved);Assert.AreEqual("A",saved.Name);
    }

    [TestMethod]
    public async Task SaveFullDefinition_RejectsFieldsTheCodecCannotEncodeWithoutMutatingSource()
    {
        var store=Seed(0,[2,(byte)'A',0,0]);
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapTypeEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        byte[] Stored(){using var stream=store.Read(19,0);return stream.ToArray();}
        var source=Stored();
        var cases=new (string Name, Func<UpdateItemDefinitionDto,UpdateItemDefinitionDto> Change)[]
        {
            ("sixth ground option overlaps inventory opcodes", item=>item with{GroundOptions=[null,null,"Take",null,null,"Extra"]}),
            ("missing inventory options", item=>item with{InventoryOptions=null!}),
            ("non-Latin-1 option", item=>item with{InventoryOptions=["Wield ☃",null,null,null,"Drop"]}),
            ("unpaired model recolors", item=>item with{OriginalModelColors=[1],ModifiedModelColors=null}),
            ("mismatched texture recolors", item=>item with{OriginalTextureColors=[1,2],ModifiedTextureColors=[3]}),
            ("recolor count exceeds a byte", item=>item with{OriginalModelColors=new int[256],ModifiedModelColors=new int[256]}),
            ("stack variants exceed opcodes 100-109", item=>item with{StackIds=new int[11],StackAmounts=new int[11]}),
            ("quest ID count exceeds a byte", item=>item with{QuestIDs=new int[256]}),
            ("boolean extra data", item=>item with{ExtraData=new Dictionary<int,object>{{1,true}}}),
            ("extra data outside int32", item=>item with{ExtraData=new Dictionary<int,object>{{1,5_000_000_000L}}}),
            ("extra data key wider than 24 bits", item=>item with{ExtraData=new Dictionary<int,object>{{0x1000001,1}}}),
            ("negative extra data key", item=>item with{ExtraData=new Dictionary<int,object>{{-1,1}}}),
            ("extra data count exceeds a byte", item=>item with{ExtraData=Enumerable.Range(0,256).ToDictionary(key=>key,key=>(object)key)}),
        };
        foreach(var (name,change) in cases)
        {
            var item=await client.GetFromJsonAsync<UpdateItemDefinitionDto>("/api/v1/cache/types/items/0/definition");
            Assert.IsNotNull(item);
            using var response=await client.PutAsJsonAsync("/api/v1/cache/types/items/0/definition",change(item));
            Assert.AreEqual(HttpStatusCode.BadRequest,response.StatusCode,name);
            CollectionAssert.AreEqual(source,Stored(),name);
        }
    }

    [TestMethod]
    public async Task ConcurrentDuplicate_AllocatesUniqueDefinitions()
    {
        var store=Seed(0,[2,(byte)'A',0,0]);
        await using var app=await CacheServiceTestHost.StartAsync(store,group=>group.MapTypeEndpoints());
        using var client=CacheServiceTestHost.CreateClient(app);
        var responses=await Task.WhenAll(Enumerable.Range(0,4).Select(_=>client.PostAsJsonAsync("/api/v1/cache/types/items/0/duplicate",new{})));
        var ids=new List<int>();
        foreach(var response in responses) using(response) {
            Assert.AreEqual(HttpStatusCode.Created,response.StatusCode);
            var result=await response.Content.ReadFromJsonAsync<DuplicateItemDefinitionResultDto>();Assert.IsNotNull(result);ids.Add(result.Id);
        }
        CollectionAssert.AreEquivalent(new[]{1,2,3,4},ids.ToArray());
    }

    private static MemoryFileStore Seed(int id,byte[] bytes)
    {
        var store=new MemoryFileStore();var table=new ReferenceTable(7,1,(ReferenceTableFlags)0,1);var entry=new ReferenceTableEntry(id>>8);
        entry.InitializeEntries(1);entry.AddEntry(id&255,new ReferenceTableChildEntry(0));table.AddEntry(id>>8,entry);
        using var encoded=new ReferenceTableCodec().Encode(table);using var reference=new Container(new MemoryStream(encoded.ToArray()));using var data=new Container(new MemoryStream(bytes));
        store.Set(255,19,reference.Encode());store.Set(19,id>>8,data.Encode());return store;
    }
}
