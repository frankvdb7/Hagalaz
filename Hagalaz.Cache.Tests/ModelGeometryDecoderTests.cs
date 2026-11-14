using Hagalaz.Cache.Logic.Codecs;
using System.Security.Cryptography;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class ModelGeometryDecoderTests
{
    [Theory]
    [InlineData(false, 255, -1)]
    [InlineData(false, 7, 7)]
    [InlineData(true, 0, -1)]
    [InlineData(true, 0x80, 256)]
    public void Decode_PreservesBillboardAfterParticleAttachments(bool smartGroup, byte group, int expectedGroup)
    {
        byte[] geometry = [0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84];
        byte[] attachments = [1, 0, 9, 0, 0, 1, 0, 3, 0, 2, 1, 0, 42, 0, 0, group];
        byte[] footer = [0, 3, 0, 1, 0, (byte)(smartGroup ? 0x46 : 6), 0, 0, 0, 0, 0,
            0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255];
        byte[] data = smartGroup && group == 0x80
            ? [.. geometry, .. attachments.AsSpan(0, attachments.Length - 1), 0x81, 1, 253, .. footer]
            : [.. geometry, .. attachments, 253, .. footer];

        var decoded = ModelGeometryDecoder.Decode(data);

        Assert.Equal(new ModelGeometryDecoder.EmitterAttachment(9, 0), Assert.Single(decoded.Emitters!));
        Assert.Equal(new ModelGeometryDecoder.EffectorAttachment(3, 2), Assert.Single(decoded.Effectors!));
        Assert.Equal(new ModelGeometryDecoder.BillboardAttachment(42, 0, expectedGroup, -3), Assert.Single(decoded.Billboards!));
        Assert.Equal(new[] { 0, 1, 2 }, decoded.Indices);
    }

    [Fact]
    public void DecodeBillboard_PreservesDimensionsTextureBlendAndFlags()
    {
        using var data = new MemoryStream([3, 255, 1, 1, 7, 2, 0, 10, 0, 20, 5, 31, 4, 1, 6, 7, 0]);
        var codec = new BillboardTypeCodec();
        var billboard = codec.Decode(0, data);
        Assert.Equal((263, 11, 21, 1, true, true), (billboard.RenderTextureId, billboard.HalfWidth, billboard.HalfHeight, billboard.RenderBlendMode, billboard.HideFace, billboard.HideInSpecialPass));
        Assert.Equal((255, 31), (billboard.Opcode3, billboard.Opcode5));
        Assert.Equal(data.Length, data.Position);
        using var encoded = codec.Encode(billboard);
        Assert.Equal(new byte[] { 1, 1, 7, 2, 0, 10, 0, 20, 3, 255, 4, 1, 5, 31, 6, 7, 0 }, encoded.ToArray());
        Assert.Equal(-1, codec.Decode(1, new MemoryStream([1, 255, 255, 0])).RenderTextureId);
        Assert.Throws<InvalidDataException>(() => codec.Decode(2, new MemoryStream([8, 0])));
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void Decode_PreservesFaceShadingWithoutChangingTopology(bool modern, byte shading)
    {
        byte[] legacy = [0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
        byte[] newer = [0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255];
        var data = modern ? newer : legacy;
        data[data.Length - (modern ? 23 : 18) + 5] = 1;
        var offset = modern ? 3 : 4;
        data = data.Take(offset).Append(shading).Concat(data.Skip(offset)).ToArray();
        var result = ModelGeometryDecoder.Decode(data);
        Assert.Equal(new[] { (int)shading }, result.FaceShadingTypes);
        Assert.Equal(new[] { 0, 1, 2 }, result.Indices);
        Assert.Equal(new[] { 0x1234 }, result.FaceColors);
        Assert.Equal(new[] { 0 }, result.FacePriorities);
    }

    [Theory]
    [InlineData(false, false, 10)]
    [InlineData(false, true, 11)]
    [InlineData(true, false, 11)]
    [InlineData(true, true, 10)]
    public void Decode_PreservesUniformAndPerFacePriorities(bool modern, bool perFace, byte priority)
    {
        byte[] legacy = [0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3];
        byte[] newer = [0, 1, 3, 1, 64, 65, 65, 0x12, 0x34, 74, 54, 84,
            0, 3, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 3, 0, 0, 255, 255];
        var data = modern ? newer : legacy;
        var header = data.Length - (modern ? 23 : 18);
        data[header + 6] = perFace ? (byte)255 : priority;
        if (perFace) data = data.Take(4).Append(priority).Concat(data.Skip(4)).ToArray();
        var result = ModelGeometryDecoder.Decode(data);
        Assert.Equal(new[] { (int)priority }, result.FacePriorities);
        Assert.Equal(new[] { 0, 1, 2 }, result.Indices);
        Assert.Equal(new[] { 0x1234 }, result.FaceColors);
    }

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

    [Theory]
    [InlineData(36668, 84, 130, "522caf7881d2dfbcfebf8dfde37bfebc5d275514e4e69c25b760f731ec5e0077")]
    [InlineData(48778, 277, 362, "a930adc149845e157cdaa773350eb481209d1773622869ab726a14aadc825205")]
    [InlineData(48859, 29, 38, "b2b4400faba82a2eae8d7699c82a24d864c399ccc23914c9262ca2e96e251165")]
    public void Decode_ReportedWallModelHasFiniteTextureProjection(int modelId, int vertices, int faces, string hash)
    {
        var data = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "models", $"model-{modelId}.dat"));
        Assert.Equal(hash, Convert.ToHexStringLower(SHA256.HashData(data)));
        var geometry = ModelGeometryDecoder.Decode(data);
        Assert.Equal(vertices * 3, geometry.Positions.Length);
        Assert.Equal(faces * 3, geometry.Indices.Length);
        Assert.NotNull(geometry.FaceTextureIds);
        Assert.Contains(geometry.FaceTextureIds, id => id >= 0);
        Assert.NotNull(geometry.FaceTextureUvs);
        Assert.Equal(geometry.Indices.Length * 2, geometry.FaceTextureUvs.Length);
        Assert.All(geometry.FaceTextureUvs, coordinate => Assert.True(float.IsFinite(coordinate)));
        var permissive = ModelGeometryDecoder.Decode(data, allowFirstTexelFallback: true);
        Assert.Empty(permissive.FirstTexelFaces!);
        Assert.Equal(geometry.FaceTextureUvs, permissive.FaceTextureUvs);
    }

    [Fact]
    public void Decode_RenderOptInMarksExactlyTheClientNonfiniteFacesAndPreservesOtherUvs()
    {
        var data = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "models", "model-48743.dat"));
        Assert.Equal("437f44670ef9de324c2cbaa8fc6a872646745ce366ef2e4473509502ca9b09ca", Convert.ToHexStringLower(SHA256.HashData(data)));
        Assert.Throws<NotSupportedException>(() => ModelGeometryDecoder.Decode(data));
        var geometry = ModelGeometryDecoder.Decode(data, allowFirstTexelFallback: true);
        // Captured by the original GameClient projection helpers, not by this decoder.
        int[] expected = [12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,31,32,35,50,51,52,55,56,57,58,59,60,64,65,66,112,113,120,121,122,123,124,126,157,158,162,164,165,166,169,170];
        Assert.Equal(expected, geometry.FirstTexelFaces);
        Assert.Equal(111 * 3, geometry.Positions.Length);
        Assert.Equal(171 * 3, geometry.Indices.Length);
        Assert.NotNull(geometry.FaceTextureUvs);
        Assert.All(geometry.FaceTextureUvs, value => Assert.True(float.IsFinite(value)));
        Assert.All(expected, face => Assert.All(geometry.FaceTextureUvs.Skip(face * 6).Take(6), value => Assert.Equal(0, value)));
        float[] clientValidFace = [0.5f, 3.3405423E-4f, 0.5f, 1.0000176f, 0.44999024f, 0.9999298f];
        for (var coordinate = 0; coordinate < clientValidFace.Length; coordinate++)
            Assert.InRange(geometry.FaceTextureUvs[coordinate], clientValidFace[coordinate] - 0.00001f, clientValidFace[coordinate] + 0.00001f);
        Assert.Equal(132, geometry.FaceTextureIds![31]);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(15)]
    public void Decode_BoxProjectionIgnoresZeroScaleOnUnusedNormalAxis(int version)
    {
        // Rotate the XY triangle onto the box's positive-Y side. Its UVs use X/Z,
        // so a zero Y scale must not poison the normal used to select that side.
        var geometry = ModelGeometryDecoder.Decode(ProjectionModel(2, version, 0, 0, orientationY: 0, orientationZ: 32767, scaleY: 0));

        Assert.Equal(new[] { 42 }, geometry.FaceTextureIds);
        Assert.Equal(new[] { 0, 1, 2 }, geometry.Indices);
        Assert.Equal(new float[] { -4.5f, -9.5f, 5.5f, -9.5f, -4.5f, 10.5f }, geometry.FaceTextureUvs);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(15)]
    public void Decode_BoxProjectionPreservesClientNegativeXSideForCollapsedX(int version)
    {
        var geometry = ModelGeometryDecoder.Decode(ProjectionModel(2, version, 0, 0, orientationY: 0, orientationZ: 32767, scaleX: 0));
        // This orientation maps Z = -Y, so the client's -X side has U = Y + 0.5.
        Assert.Equal(new float[] { -9.5f, 0.5f, -9.5f, 0.5f, 10.5f, 0.5f }, geometry.FaceTextureUvs);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(15)]
    public void Decode_BoxProjectionUsesCollapsedYPlaneForSlopedFaces(int version)
    {
        // Tilt leaves a nonzero Y normal smaller than Z. A zero Y scale still projects onto X/Z.
        var geometry = ModelGeometryDecoder.Decode(ProjectionModel(2, version, 0, 0, orientationY: 30000, orientationZ: 13177, scaleY: 0));
        var cosine = 30000f / 32767f;
        var slope = (float)Math.Sqrt(1 - cosine * cosine);
        var expected = new float[] { -4.5f, 0.5f - 10 * slope, 5.5f, 0.5f - 10 * slope, -4.5f, 0.5f + 10 * slope };
        Assert.NotNull(geometry.FaceTextureUvs);
        for (var index = 0; index < expected.Length; index++)
            Assert.InRange(geometry.FaceTextureUvs[index], expected[index] - 0.00001f, expected[index] + 0.00001f);
    }

    [Theory]
    [InlineData(12)]
    [InlineData(14)]
    [InlineData(15)]
    public void Decode_BoxProjectionStillRejectsZeroScaleOnUsedUvAxis(int version)
    {
        // The default orientation selects the Z side, which uses Y in its UVs.
        var error = Assert.Throws<NotSupportedException>(() => ModelGeometryDecoder.Decode(ProjectionModel(2, version, 0, 0, scaleY: 0)));
        Assert.Equal("Degenerate texture projection cannot produce finite coordinates.", error.Message);
    }

    private static byte[] ProjectionModel(int type, int version, int rotation, int direction, int orientationY = 32767, int orientationZ = 0, int? scaleY = null, int? scaleX = null)
    {
        using var stream = new MemoryStream();
        // One texture projection, three vertices and one triangle.
        stream.Write(new byte[] { (byte)type, 0, 1, 3, 1, 64, 65, 65, 0, 43, 1, 0, 127, 74, 54, 84 });
        Short(0); Short(orientationY); Short(orientationZ); // Projection orientation: positive Y by default.
        var scales = type == 1 ? new[] { 0, 64, 1024 } : type == 2 ? new[] { 64, 64, 64 } : new[] { 1024, 1024, 1024 };
        if (scaleY.HasValue) scales[1] = scaleY.Value;
        if (scaleX.HasValue) scales[0] = scaleX.Value;
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
