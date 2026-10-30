using System;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests;
public class ModelParticleTests
{
    [Theory]
    [InlineData(65297,785)]
    [InlineData(65316,782)]
    public void Decode_RealCompletionistCapeRetainsSevenFaceAttachments(int model, int firstFace)
    {
        var geometry=ModelGeometryDecoder.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,$"Fixtures/particles/model-{model}.dat")));
        Assert.Equal(7,geometry.Emitters!.Length);
        Assert.All(geometry.Emitters, emitter=>Assert.Equal(403,emitter.EmitterId));
        Assert.Equal(Enumerable.Range(firstFace,7),geometry.Emitters.Select(emitter=>emitter.Face));
        Assert.All(geometry.Emitters, emitter=>Assert.True(emitter.Face*3+2<geometry.Indices.Length));
        Assert.All(geometry.Emitters, emitter=> {
            Assert.Equal(255,geometry.FaceAlpha[emitter.Face]);
            Assert.Equal(20287,geometry.FaceColors[emitter.Face]);
        });
    }
    [Fact]
    public void Decode_InventoryCapeDoesNotInventEquippedParticles()
    {
        var geometry=ModelGeometryDecoder.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures/particles/model-65270.dat")));
        Assert.Null(geometry.Emitters);
    }
    [Fact]
    public void Decode_RealEmitter403RetainsSourceLifecycleTextureAndUnsupportedFields()
    {
        var d=ParticleEmitterDecoder.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures/particles/emitter-403.dat")));
        Assert.Equal(744,d.TextureId); Assert.Equal(0x1effffffu,d.ColorMin); Assert.Equal(0x5accccccu,d.ColorMax);
        Assert.Equal(0.05f,d.LifeMin); Assert.Equal(0.06f,d.LifeMax);
        Assert.Equal(296.875f,d.RateMin); Assert.Equal(500f,d.RateMax);
        Assert.Equal(48,d.SizeMin); Assert.Equal(40,d.SizeMax);
        Assert.InRange(d.SpeedMin,799,801); Assert.InRange(d.SpeedMax,1199,1201);
        Assert.Contains(10,d.UnsupportedOpcodes); Assert.Contains(18,d.UnsupportedOpcodes);
        Assert.Equal(new[]{0,64,4080,3920},d.Angles);
    }
    [Fact]
    public void Decode_UnknownOrTruncatedEmitterFailsExplicitly()
    {
        Assert.Throws<NotSupportedException>(()=>ParticleEmitterDecoder.Decode([99,0]));
        Assert.Throws<InvalidDataException>(()=>ParticleEmitterDecoder.Decode([7,0]));
    }
}
