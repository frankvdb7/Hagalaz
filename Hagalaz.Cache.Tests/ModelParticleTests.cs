using System;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests;
public class ModelParticleTests
{
    private static IParticleEmitterType Emitter(byte[] data) => new ParticleEmitterTypeCodec().Decode(0, new MemoryStream(data));

    [Theory]
    [InlineData(1, 250, -6)]
    [InlineData(2, 6, 6)]
    public void Decode_SpeedDragReadsSignedStrengthWithoutConsumingNextOpcode(byte mode, byte strength, int signedStrength)
    {
        var emitter = Emitter([4, mode, strength, 15, 0, 44, 0]);
        Assert.Equal(new ParticleDrag(mode, signedStrength), emitter.SpeedDrag);
        Assert.Equal(44, emitter.RenderTextureId);
        Assert.Empty(emitter.UnsupportedOpcodes);
    }

    [Fact]
    public void Decode_EmitterReadsLocalEffectorIdsAndKeepsFollowingOpcodesAligned()
    {
        var emitter = Emitter([9, 2, 0, 17, 1, 2, 15, 0, 44, 10, 1, 0, 6, 0]);
        Assert.Equal(new[] { 17, 258 }, emitter.LocalEffectorIds);
        Assert.Equal(44, emitter.RenderTextureId);
        Assert.Equal(new[] { 6 }, emitter.ConstantEffectorIds);
        Assert.Empty(emitter.UnsupportedOpcodes);
        Assert.Throws<InvalidDataException>(() => Emitter([9, 2, 0, 17, 0]));
    }

    [Fact]
    public void Decode_EffectorRetainsSignedVectorAndModeFlags()
    {
        using var bytes = new MemoryStream([1, 3, 255, 2, 123, 3, 0, 1, 64, 0, 255, 255, 255, 254, 0, 0, 0, 7, 4, 2, 0, 0, 2, 106, 6, 1, 8, 9, 10, 0]);
        var effector = new ParticleEffectorTypeCodec().Decode(0, bytes);
        Assert.Equal(new ParticleVector(81920, -2, 7), effector.Position);
        Assert.Equal(1023, effector.ConeAngle);
        Assert.Equal(new ParticleFalloff(2, 618), effector.Falloff);
        Assert.Equal(618, effector.EffectiveAttenuation);
        Assert.Equal(1, effector.Scope);
        Assert.Equal(123, effector.Opcode2);
        Assert.True(effector.DirectDisplacement && effector.Radial && effector.Reverse);
        Assert.Equal(bytes.Length, bytes.Position);
    }

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
        var d=Emitter(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures/particles/emitter-403.dat")));
        Assert.Equal(744,d.RenderTextureId); Assert.Equal(0x1effffffu,d.ColorMin); Assert.Equal(0x5accccccu,d.ColorMax);
        Assert.Equal(0.05f,d.LifeMin); Assert.Equal(0.06f,d.LifeMax);
        Assert.Equal(296.875f,d.RateMin); Assert.Equal(500f,d.RateMax);
        Assert.Equal(48,d.SizeMin); Assert.Equal(40,d.SizeMax);
        Assert.Equal(new ParticleSizeCurve(4, 100), d.SizeOverLife);
        Assert.Equal(new ParticleSpeedCurve(734003, 100), d.SpeedOverLife);
        Assert.DoesNotContain(22, d.UnsupportedOpcodes);
        Assert.DoesNotContain(23, d.UnsupportedOpcodes);
        Assert.DoesNotContain(27, d.UnsupportedOpcodes);
        Assert.DoesNotContain(28, d.UnsupportedOpcodes);
        Assert.InRange(d.SpeedMin,799,801); Assert.InRange(d.SpeedMax,1199,1201);
        Assert.NotEmpty(d.ConstantEffectorIds); Assert.DoesNotContain(10,d.UnsupportedOpcodes);
        Assert.Equal(new ParticleColorCurve(0x00ffffff, 100, 100), d.ColorOverLife);
        Assert.DoesNotContain(18, d.UnsupportedOpcodes);
        Assert.DoesNotContain(20, d.UnsupportedOpcodes);
        Assert.DoesNotContain(21, d.UnsupportedOpcodes);
        Assert.Equal(new[]{0,64,4080,3920},d.RenderAngles);
    }
    [Fact]
    public void Decode_UnknownOrTruncatedEmitterFailsExplicitly()
    {
        Assert.Throws<NotSupportedException>(()=>Emitter([99,0]));
        Assert.Throws<InvalidDataException>(()=>Emitter([7,0]));
    }

    [Fact]
    public void Encode_EmitterPreservesUnusedPayloadsAndIsStable()
    {
        var codec = new ParticleEmitterTypeCodec();
        var stored = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures/particles/emitter-403.dat"));
        using var first = codec.Encode(codec.Decode(403, new MemoryStream(stored)));
        using var second = codec.Encode(codec.Decode(403, new MemoryStream(first.ToArray())));
        Assert.Equal(first.ToArray(), second.ToArray());
        var unused = Emitter([24, 16, 1, 2, 3, 4, 5, 6, 22, 255, 255, 255, 255, 24, 0]);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6 }, unused.Opcode16);
        Assert.Null(unused.SpeedOverLife);
        Assert.Equal(-1, unused.TargetSpeed);
        Assert.Equal(new[] { 16, 24 }, unused.UnsupportedOpcodes);
        using var encoded = codec.Encode(unused);
        Assert.Equal(new byte[] { 16, 1, 2, 3, 4, 5, 6, 22, 255, 255, 255, 255, 24, 0 }, encoded.ToArray());
    }

    [Fact]
    public void Encode_EffectorKeepsStoredZeroAttenuation()
    {
        var codec = new ParticleEffectorTypeCodec();
        var effector = codec.Decode(0, new MemoryStream([4, 3, 0, 0, 0, 0, 0]));
        Assert.Equal(1, effector.EffectiveAttenuation);
        using var encoded = codec.Encode(effector);
        Assert.Equal(new byte[] { 4, 3, 0, 0, 0, 0, 0 }, encoded.ToArray());
        Assert.Throws<InvalidDataException>(() => codec.Decode(0, new MemoryStream([5, 0])));
    }
}
