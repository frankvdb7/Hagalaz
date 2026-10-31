using System.IO;
using System.Linq;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests;
public class ItemDefinitionRoundTripTests
{
    [Fact]
    public void FullDefinitionEncode_RetainsUnmodeledPayloadsAndExtendedHeadIds()
    {
        var codec=new ItemTypeCodec();
        byte[] legacy=[43,0,1,2,3,44,0,7,45,0,9,250,3,251,1,0,8,0,9,252,1,0,10,0,11,253,0,1,0,2,0,3,0,4];
        using var bytes=new MemoryStream(legacy.Concat(new byte[]{0}).ToArray());
        var source=codec.Decode(7,bytes);
        var updated=new ItemType(7) {Name="Full definition",Value=123,MembersOnly=true,StackableType=1,MaleHeadModel=60000,FemaleHeadModel=60001,MaleHeadModel2=60002,FemaleHeadModel2=60003,ScaleX=256};
        updated.PreservedOpcodes = ((ItemType)source).PreservedOpcodes;
        using var encoded=codec.Encode(updated);
        Assert.True(encoded.ToArray().AsSpan().IndexOf(legacy)>=0);
        encoded.Position=0;
        var result=(ItemType)codec.Decode(7,encoded);
        Assert.Equal("Full definition",result.Name);Assert.Equal(123,result.Value);Assert.True(result.Stackable);Assert.True(result.MembersOnly);
        Assert.Equal(60000,result.MaleHeadModel);Assert.Equal(60001,result.FemaleHeadModel);Assert.Equal(60002,result.MaleHeadModel2);Assert.Equal(60003,result.FemaleHeadModel2);Assert.Equal(256,result.ScaleX);
    }
    [Fact]
    public void FullDefinitionEncode_RetainsStoredFieldsWithTemplateAndCounterpartLinks()
    {
        var codec=new ItemTypeCodec();
        var item=new ItemType(20139) {Name="Stored name",NoteId=20140,NoteTemplateId=799,LendTemplateId=900,MembersOnly=true,StackableType=1,Value=50,UnknownInt18=0,UnknownInt19=20};
        using var encoded=codec.Encode(item);encoded.Position=0;
        var result=(ItemType)codec.Decode(item.Id,encoded);
        Assert.Equal(item.Name,result.Name);Assert.Equal(item.Value,result.Value);Assert.True(result.MembersOnly);Assert.Equal(1,result.StackableType);
        Assert.Equal(20140,result.NoteId);Assert.Equal(799,result.NoteTemplateId);Assert.Equal(900,result.LendTemplateId);Assert.Equal(20,result.UnknownInt19);
    }

}
