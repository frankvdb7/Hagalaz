using Hagalaz.Cache.Logic.Codecs;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class ModelGeometryDecoderTests
{
    [Fact]
    public void DecodeLegacy_PreservesTextureReferenceSeparatelyFromFaceColor()
    {
        byte[] data = [0, 1, 3, 1, 2, 64, 65, 65, 0, 42, 0, 0, 0, 1, 0, 2, 74, 54, 84,
            0, 3, 0, 1, 1, 1, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
        var result = ModelGeometryDecoder.Decode(data);
        Assert.Equal(new[] { 42 }, result.FaceTextureIds);
        Assert.Equal(new[] { 0, 1, 2 }, result.FaceTextureVertices);
        Assert.Equal(new[] { 127 }, result.FaceColors);
        Assert.Equal(new[] { 0, 1, 2 }, result.Indices);
        Assert.Equal(new float[] { 0, 0, 1, 0, 0, 1 }, result.FaceTextureUvs);
    }

    [Fact]
    public void Decode_PreservesVersionUsedForMixedModelAssembly()
    {
        byte[] data = [0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84, 13,
            0, 3, 0, 1, 0, 8, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255];
        var result = ModelGeometryDecoder.Decode(data);
        Assert.Equal(13, result.Version);
        Assert.Equal(new[] { 0, 0, 0, 10, 0, 0, 0, 20, 0 }, result.Positions);
    }

    [Fact]
    public void Decode_TextureWithoutMappingUsesClientDefaultCorners()
    {
        byte[] data = [0, 1, 3, 1, 64, 65, 65, 0, 43, 0, 127, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255];
        var result = ModelGeometryDecoder.Decode(data);
        Assert.Equal(new[] { 42 }, result.FaceTextureIds);
        Assert.Equal(new float[] { 0, 1, 1, 1, 0, 0 }, result.FaceTextureUvs);
    }

    [Fact]
    public void DecodeLegacy_PreservesVertexSkinGroupsForAnimation()
    {
        byte[] data = [0, 1, 3, 1, 2, 2, 7, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 0, 0, 1, 0, 2, 0, 1, 0, 0, 0, 3];
        var result = ModelGeometryDecoder.Decode(data);
        Assert.Equal(new[] { 2, 2, 7 }, result.VertexSkins);
        Assert.Equal(new[] { 0, 1, 2 }, result.Indices);
    }

    [Fact]
    public void DecodeLegacy_PreservesRawFaceTransparency()
    {
        byte[] data = [0, 1, 3, 1, 128, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 1, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
        var result = ModelGeometryDecoder.Decode(data);
        Assert.Equal(new[] { 128 }, result.FaceAlpha);
        Assert.Equal(new[] { 0, 1, 2 }, result.Indices);
    }

    [Fact]
    public void Decode_ModernFormatReconstructsGeometry()
    {
        byte[] data = [0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255];
        var result = ModelGeometryDecoder.Decode(data);
        Assert.Equal(new[] { 0, 0, 0, 10, 0, 0, 0, 20, 0 }, result.Positions);
        Assert.Equal(new[] { 0, 1, 2 }, result.Indices);
        Assert.Equal(new[] { 0x1234 }, result.FaceColors);
    }

    [Fact]
    public void DecodeLegacy_ReconstructsSignedCoordinateDeltasAndTriangle()
    {
        // Three vertices, one triangle: (0,0,0), (10,0,0), (0,20,0).
        byte[] data = [0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
        var result = ModelGeometryDecoder.DecodeLegacy(data);
        Assert.Equal(new[] { 0, 0, 0, 10, 0, 0, 0, 20, 0 }, result.Positions);
        Assert.Equal(new[] { 0, 1, 2 }, result.Indices);
        Assert.Equal(new[] { 0x1234 }, result.FaceColors);
    }

    [Fact]
    public void DecodeLegacy_RejectsTruncatedHeader()
    {
        Assert.Throws<InvalidDataException>(() => ModelGeometryDecoder.DecodeLegacy([0]));
    }

    // Golden coordinates produced by the unmodified GameClient Model projection helpers.
    public static IEnumerable<object[]> ClientProjectionUvs()
    {
        yield return new object[] { 1, 0, 0, new float[] { 0.25f, -9.5f, 0.75f, -9.5f, 0.25f, 10.5f } };
        yield return new object[] { 1, 0, 1, new float[] { 9.5f, 0.25f, 9.5f, 0.75f, -10.5f, 0.25f } };
        yield return new object[] { 1, 0, 2, new float[] { -0.25f, 9.5f, -0.75f, 9.5f, -0.25f, -10.5f } };
        yield return new object[] { 1, 0, 3, new float[] { -9.5f, -0.25f, -9.5f, -0.75f, 10.5f, -0.25f } };
        yield return new object[] { 1, 64, 0, new float[] { 0.5f, -9.5f, 2.9802322E-8f, -9.5f, 0.5f, 10.5f } };
        yield return new object[] { 1, 64, 1, new float[] { 9.5f, 0.5f, 9.5f, 2.9802322E-8f, -10.5f, 0.5f } };
        yield return new object[] { 1, 64, 2, new float[] { -0.5f, 9.5f, -2.9802322E-8f, 9.5f, -0.5f, -10.5f } };
        yield return new object[] { 1, 64, 3, new float[] { -9.5f, -0.5f, -9.5f, -2.9802322E-8f, 10.5f, -0.5f } };
        yield return new object[] { 2, 0, 0, new float[] { 5.5f, 10.5f, -4.5f, 10.5f, 5.5f, -9.5f } };
        yield return new object[] { 2, 0, 1, new float[] { -10.5f, 5.5f, -10.5f, -4.5f, 9.5f, 5.5f } };
        yield return new object[] { 2, 0, 2, new float[] { -5.5f, -10.5f, 4.5f, -10.5f, -5.5f, 9.5f } };
        yield return new object[] { 2, 0, 3, new float[] { 10.5f, -5.5f, 10.5f, 4.5f, -9.5f, -5.5f } };
        yield return new object[] { 2, 64, 0, new float[] { 5.5f, 10.5f, -4.5f, 10.5f, 5.5f, -9.5f } };
        yield return new object[] { 2, 64, 1, new float[] { -10.5f, 5.5f, -10.5f, -4.5f, 9.5f, 5.5f } };
        yield return new object[] { 2, 64, 2, new float[] { -5.5f, -10.5f, 4.5f, -10.5f, -5.5f, 9.5f } };
        yield return new object[] { 2, 64, 3, new float[] { 10.5f, -5.5f, 10.5f, 4.5f, -9.5f, -5.5f } };
        yield return new object[] { 3, 0, 0, new float[] { 0.25f, 0.14758366f, 0.75f, 0.14758366f, 0.25f, 0.85241634f } };
        yield return new object[] { 3, 0, 1, new float[] { -0.14758366f, 0.25f, -0.14758366f, 0.75f, -0.85241634f, 0.25f } };
        yield return new object[] { 3, 0, 2, new float[] { -0.25f, -0.14758366f, -0.75f, -0.14758366f, -0.25f, -0.85241634f } };
        yield return new object[] { 3, 0, 3, new float[] { 0.14758366f, -0.25f, 0.14758366f, -0.75f, 0.85241634f, -0.25f } };
        yield return new object[] { 3, 64, 0, new float[] { 0.5f, 0.14758366f, 2.9802322E-8f, 0.14758366f, 0.5f, 0.85241634f } };
        yield return new object[] { 3, 64, 1, new float[] { -0.14758366f, 0.5f, -0.14758366f, 2.9802322E-8f, -0.85241634f, 0.5f } };
        yield return new object[] { 3, 64, 2, new float[] { -0.5f, -0.14758366f, -2.9802322E-8f, -0.14758366f, -0.5f, -0.85241634f } };
        yield return new object[] { 3, 64, 3, new float[] { 0.14758366f, -0.5f, 0.14758366f, -2.9802322E-8f, 0.85241634f, -0.5f } };
    }

    [Theory]
    [MemberData(nameof(ClientProjectionUvs))]
    public void Decode_ProjectionUvsMatchGameClient(int type, int rotation, int direction, float[] expected)
    {
        foreach (var version in new[] { 12, 14, 15 })
        {
            var geometry = ModelGeometryDecoder.Decode(ProjectionModel(type, version, rotation, direction));
            Assert.Equal(new[] { 42 }, geometry.FaceTextureIds);
            Assert.NotNull(geometry.FaceTextureUvs);
            for (var index = 0; index < expected.Length; index++)
                Assert.InRange(geometry.FaceTextureUvs[index], expected[index] - 0.00001f, expected[index] + 0.00001f);
        }
    }

    private static byte[] ProjectionModel(int type, int version, int rotation, int direction)
    {
        using var stream = new MemoryStream();
        // One texture projection, three vertices and one triangle.
        stream.Write(new byte[] { (byte)type, 0, 1, 3, 1, 64, 65, 65, 0, 43, 1, 0, 127, 74, 54, 84 });
        Short(0); Short(32767); Short(0); // Projection orientation: positive Y.
        var scales = type == 1 ? new[] { 0, 64, 1024 } : type == 2 ? new[] { 64, 64, 64 } : new[] { 1024, 1024, 1024 };
        for (var axis = 0; axis < 3; axis++)
        {
            if (version >= 15 || (version == 14 && axis == 1)) stream.WriteByte((byte)(scales[axis] >> 16));
            Short(scales[axis]);
        }
        stream.WriteByte((byte)rotation); stream.WriteByte((byte)direction); stream.WriteByte(0);
        if (type == 2) { stream.WriteByte(0); stream.WriteByte(0); }
        if (version > 12) stream.WriteByte((byte)version);
        Short(3); Short(1);
        stream.Write(new byte[] { 1, (byte)(version > 12 ? 8 : 0), 0, 0, 0, 1, 0 });
        Short(2); Short(1); Short(0); Short(3); Short(1);
        stream.WriteByte(255); stream.WriteByte(255);
        return stream.ToArray();

        void Short(int value) { stream.WriteByte((byte)(value >> 8)); stream.WriteByte((byte)value); }
    }
}
