using System;
using System.Buffers.Binary;
using System.IO;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>Decodes model topology used by the revision-742 client.</summary>
public static class ModelGeometryDecoder
{
    public sealed record EmitterAttachment(int EmitterId, int Face);
    public sealed record Geometry(int[] Positions, int[] Indices, int[] FaceColors, int[] FaceAlpha, int[]? VertexSkins, int Version = 12, int[]? FaceSkins = null, int[]? FaceTextureIds = null, int[]? FaceTextureVertices = null, float[]? FaceTextureUvs = null, EmitterAttachment[]? Emitters = null);

    public static Geometry Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 2 || data[^1] != 255 || data[^2] != 255) return DecodeLegacy(data);
        if (data.Length < 23) throw new InvalidDataException("Model header is truncated.");
        var header = new Reader(data, data.Length - 23);
        var vertices = header.Short();
        var faces = header.Short();
        var textureCount = header.Byte();
        var flags = header.Byte();
        var version = (flags & 8) != 0 ? data[data.Length - 24] : 12;
        // Extended skin tables alter the footer layout and need their own decoder.
        if ((flags & 0x30) != 0) throw new NotSupportedException("Extended model skin tables are not yet supported.");
        var priority = header.Byte();
        var alpha = header.Byte();
        var faceSkins = header.Byte();
        var textures = header.Byte();
        var vertexSkins = header.Byte();
        var xLength = header.Short();
        var yLength = header.Short();
        var zLength = header.Short();
        var indexLength = header.Short();
        var mappingLength = header.Short();
        var vertexFlagsOffset = textureCount;
        var typeOffset = vertexFlagsOffset + vertices;
        var codesOffset = typeOffset + ((flags & 1) != 0 ? faces : 0);
        var priorityOffset = codesOffset + faces;
        var faceSkinOffset = priorityOffset + (priority == 255 ? faces : 0);
        var vertexSkinOffset = faceSkinOffset + (faceSkins == 1 ? faces : 0);
        var alphaOffset = vertexSkinOffset + (vertexSkins == 1 ? vertices : 0);
        var indicesOffset = alphaOffset + (alpha == 1 ? faces : 0);
        var colorOffset = indicesOffset + indexLength + (textures == 1 ? faces * 2 : 0) + mappingLength;
        var xOffset = colorOffset + faces * 2;
        var geometryEnd = xOffset + xLength + yLength + zLength;
        if (geometryEnd > data.Length - 23) throw new InvalidDataException("Model geometry streams overlap the header.");

        // Both formats share coordinate and topology encoding. Reuse that decoder
        // after projecting their stream layout, rather than duplicating its rules.
        using var legacy = new MemoryStream();
        Copy(vertexFlagsOffset, vertices);
        Copy(codesOffset, faces);
        if (faceSkins == 1) Copy(faceSkinOffset, faces);
        if (vertexSkins == 1) Copy(vertexSkinOffset, vertices);
        if (alpha == 1) Copy(alphaOffset, faces);
        Copy(indicesOffset, indexLength);
        Copy(colorOffset, faces * 2);
        Copy(xOffset, xLength + yLength + zLength);
        WriteShort(vertices);
        WriteShort(faces);
        legacy.Write(new byte[] { 0, 0, 0, (byte)alpha, (byte)faceSkins, (byte)vertexSkins });
        WriteShort(xLength);
        WriteShort(yLength);
        WriteShort(zLength);
        WriteShort(indexLength);
        var result = DecodeLegacy(legacy.ToArray());
        if (result.VertexSkins is { } skins)
            for (var vertex = 0; vertex < skins.Length; vertex++)
                if (skins[vertex] == 255) skins[vertex] = -1;
        int[]? textureIds = null;
        var textureVertices = new int[faces * 3];
        float[]? textureUvs = null;
        Array.Fill(textureVertices, -1);
        var simpleCount = 0; var complexCount = 0; var boxCount = 0;
        for (var mapping = 0; mapping < textureCount; mapping++)
        {
            if (data[mapping] == 0) simpleCount++;
            else if (data[mapping] <= 3) { complexCount++; if (data[mapping] == 2) boxCount++; }
            else throw new NotSupportedException($"Unsupported texture projection {data[mapping]}.");
        }
        if (textures == 1)
        {
            textureIds = new int[faces];
            var textureReader = new Reader(data, indicesOffset + indexLength);
            for (var face = 0; face < faces; face++) textureIds[face] = textureReader.Short() - 1;
            var mappings = new ModelTextureCoordinates.Mapping[textureCount];
            var simpleReader = new Reader(data, geometryEnd);
            var complexReader = new Reader(data, geometryEnd + simpleCount * 6);
            var scaleOffset = geometryEnd + simpleCount * 6 + complexCount * 6;
            var scaleReader = new Reader(data, scaleOffset);
            var rotationOffset = scaleOffset + complexCount * (version >= 15 ? 9 : version == 14 ? 7 : 6);
            var rotationReader = new Reader(data, rotationOffset);
            var directionReader = new Reader(data, rotationOffset + complexCount);
            var offsetReader = new Reader(data, rotationOffset + complexCount * 2);
            if (rotationOffset + complexCount * 3 + boxCount * 2 > data.Length - 23)
                throw new InvalidDataException("Texture mapping streams overlap the header.");
            for (var mapping = 0; mapping < textureCount; mapping++)
            {
                var type = data[mapping];
                var reader = type == 0 ? simpleReader : complexReader;
                var a = reader.Short(); var b = reader.Short(); var c = reader.Short();
                if (type == 0) { mappings[mapping] = new(type, a, b, c); continue; }
                var sx = version >= 15 ? scaleReader.Medium() : scaleReader.Short();
                var sy = version >= 14 ? scaleReader.Medium() : scaleReader.Short();
                var sz = version >= 15 ? scaleReader.Medium() : scaleReader.Short();
                // ModelDefinition.method1196 normalizes old positions and projection scales together.
                if (version < 13) { sx *= 4; sy *= 4; if (type != 1) sz *= 4; }
                var rotation = rotationReader.Byte();
                var direction = directionReader.SignedByte();
                var offset = offsetReader.SignedByte();
                var oy = type == 2 ? offsetReader.SignedByte() : 0;
                var oz = type == 2 ? offsetReader.SignedByte() : 0;
                mappings[mapping] = new(type, (short)a, (short)b, (short)c, sx, sy, sz, rotation, direction, offset, oy, oz);
            }
            var mappingReader = new Reader(data, indicesOffset + indexLength + faces * 2);
            var pointers = new int[faces];
            Array.Fill(pointers, -1);
            for (var face = 0; face < faces; face++)
            {
                if (textureIds[face] < 0) continue;
                var mapping = textureCount > 0 ? mappingReader.Byte() - 1 : -1;
                pointers[face] = mapping;
                if (mapping >= textureCount) throw new InvalidDataException("Texture mapping references a missing triangle.");
                if (mapping < 0) Array.Copy(result.Indices, face * 3, textureVertices, face * 3, 3);
                else if (mappings[mapping] is { Type: 0 } triangle)
                {
                    textureVertices[face * 3] = triangle.A;
                    textureVertices[face * 3 + 1] = triangle.B;
                    textureVertices[face * 3 + 2] = triangle.C;
                }
            }
            var projectionPositions = result.Positions;
            if (version < 13)
            {
                projectionPositions = (int[])result.Positions.Clone();
                for (var coordinate = 0; coordinate < projectionPositions.Length; coordinate++) projectionPositions[coordinate] *= 4;
            }
            textureUvs = ModelTextureCoordinates.Generate(projectionPositions, result.Indices, textureIds, pointers, mappings);
        }
        var attachmentOffset = geometryEnd + simpleCount * 6 + complexCount * (6 + (version >= 15 ? 9 : version == 14 ? 7 : 6) + 3) + boxCount * 2;
        EmitterAttachment[]? emitters = null;
        if ((flags & 2) != 0)
        {
            var attachments = new Reader(data, attachmentOffset);
            emitters = new EmitterAttachment[attachments.Byte()];
            for (var emitter = 0; emitter < emitters.Length; emitter++)
            {
                var emitterId = attachments.Short(); var face = attachments.Short();
                if (face >= faces) throw new InvalidDataException("Particle emitter references a missing face.");
                emitters[emitter] = new(emitterId, face);
            }
            var effectors = attachments.Byte();
            for (var effector = 0; effector < effectors; effector++) { attachments.Short(); attachments.Short(); }
        }
        return result with { Version = version, FaceTextureIds = textureIds, FaceTextureVertices = textureVertices, FaceTextureUvs = textureUvs, Emitters = emitters };

        void Copy(int offset, int length)
        {
            if (offset < 0 || length < 0 || offset > data.Length - length)
                throw new InvalidDataException("Model stream is truncated.");
            legacy.Write(data, offset, length);
        }
        void WriteShort(int value)
        {
            legacy.WriteByte((byte)(value >> 8));
            legacy.WriteByte((byte)value);
        }
    }

    public static Geometry DecodeLegacy(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 18) throw new InvalidDataException("Model header is truncated.");
        if (data[^1] == 255 && data[^2] == 255)
            throw new NotSupportedException("This decoder accepts legacy model geometry only.");
        var header = new Reader(data, data.Length - 18);
        var vertices = header.Short();
        var faces = header.Short();
        var textures = header.Byte();
        var hasTypes = header.Byte();
        var priority = header.Byte();
        var hasAlpha = header.Byte();
        var hasFaceSkins = header.Byte();
        var hasVertexSkins = header.Byte();
        var xLength = header.Short();
        var yLength = header.Short();
        var zLength = header.Short();
        var indexLength = header.Short();
        var offset = vertices + faces;
        if (priority == 255) offset += faces;
        var faceSkinOffset = offset;
        if (hasFaceSkins == 1) offset += faces;
        var typeOffset = offset;
        if (hasTypes == 1) offset += faces;
        var vertexSkinOffset = offset;
        if (hasVertexSkins == 1) offset += vertices;
        var alphaOffset = offset;
        if (hasAlpha == 1) offset += faces;
        var indexOffset = offset;
        offset += indexLength;
        var colorOffset = offset;
        offset += faces * 2 + textures * 6;
        var xOffset = offset;
        var yOffset = xOffset + xLength;
        var zOffset = yOffset + yLength;
        if (zOffset + zLength != data.Length - 18)
            throw new InvalidDataException("Model stream lengths do not match its header.");
        var flags = new Reader(data, 0);
        var xs = new Reader(data, xOffset);
        var ys = new Reader(data, yOffset);
        var zs = new Reader(data, zOffset);
        var positions = new int[vertices * 3];
        int x = 0, y = 0, z = 0;
        for (var v = 0; v < vertices; v++)
        {
            var bits = flags.Byte();
            if ((bits & 1) != 0) x += xs.Smart();
            if ((bits & 2) != 0) y += ys.Smart();
            if ((bits & 4) != 0) z += zs.Smart();
            positions[v * 3] = x;
            positions[v * 3 + 1] = y;
            positions[v * 3 + 2] = z;
        }
        var codes = new Reader(data, vertices);
        var deltas = new Reader(data, indexOffset);
        var colors = new Reader(data, colorOffset);
        var types = new Reader(data, typeOffset);
        var indices = new int[faces * 3];
        var faceColors = new int[faces];
        var alphas = new Reader(data, alphaOffset);
        var faceAlpha = new int[faces];
        var faceTextureIds = new int[faces];
        var faceTextureVertices = new int[faces * 3];
        var texturePointers = new int[faces];
        var textureMappings = new ModelTextureCoordinates.Mapping[textures];
        var mappingTriangles = new Reader(data, colorOffset + faces * 2);
        for (var mapping = 0; mapping < textures; mapping++)
            textureMappings[mapping] = new(0, mappingTriangles.Short(), mappingTriangles.Short(), mappingTriangles.Short());
        Array.Fill(faceTextureVertices, -1);
        Array.Fill(faceTextureIds, -1);
        int a = 0, b = 0, c = 0, last = 0;
        for (var f = 0; f < faces; f++)
        {
            switch (codes.Byte())
            {
                case 1: a = last + deltas.Smart(); b = a + deltas.Smart(); c = b + deltas.Smart(); break;
                case 2: b = c; c = last + deltas.Smart(); break;
                case 3: a = c; c = last + deltas.Smart(); break;
                case 4: (a, b) = (b, a); c = last + deltas.Smart(); break;
                default: throw new InvalidDataException("Unknown triangle topology opcode.");
            }
            last = c;
            if ((uint)a >= vertices || (uint)b >= vertices || (uint)c >= vertices)
                throw new InvalidDataException("Triangle references a missing vertex.");
            indices[f * 3] = a;
            indices[f * 3 + 1] = b;
            indices[f * 3 + 2] = c;
            faceColors[f] = colors.Short();
            if (hasAlpha == 1) faceAlpha[f] = alphas.Byte();
            var faceType = hasTypes == 1 ? types.Byte() : 0;
            if ((faceType & 2) != 0)
            {
                faceTextureIds[f] = faceColors[f] == 65535 ? -1 : faceColors[f];
                faceColors[f] = 127;
                var mapping = faceType >> 2;
                texturePointers[f] = mapping;
                if (mapping >= textures) throw new InvalidDataException("Texture mapping references a missing triangle.");
                var triangle = new Reader(data, colorOffset + faces * 2 + mapping * 6);
                for (var corner = 0; corner < 3; corner++) faceTextureVertices[f * 3 + corner] = triangle.Short();
            }
        }
        int[]? vertexSkins = null;
        if (hasVertexSkins == 1)
        {
            vertexSkins = new int[vertices];
            var skins = new Reader(data, vertexSkinOffset);
            for (var vertex = 0; vertex < vertices; vertex++) vertexSkins[vertex] = skins.Byte();
        }
        int[]? faceSkins = null;
        if (hasFaceSkins == 1)
        {
            faceSkins = new int[faces];
            var groups = new Reader(data, faceSkinOffset);
            for (var face = 0; face < faces; face++) faceSkins[face] = groups.Byte();
        }
        return new Geometry(positions, indices, faceColors, faceAlpha, vertexSkins, FaceSkins: faceSkins, FaceTextureIds: faceTextureIds, FaceTextureVertices: faceTextureVertices,
            FaceTextureUvs: textures == 0 ? null : ModelTextureCoordinates.Generate(positions, indices, faceTextureIds, texturePointers, textureMappings));
    }

    private sealed class Reader(byte[] data, int position)
    {
        public int Byte()
        {
            if ((uint)position >= data.Length) throw new InvalidDataException("Model stream is truncated.");
            return data[position++];
        }
        public int Short()
        {
            if (position < 0 || position > data.Length - 2) throw new InvalidDataException("Model stream is truncated.");
            var value = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(position, 2));
            position += 2;
            return value;
        }
        public int Smart()
        {
            if ((uint)position >= data.Length) throw new InvalidDataException("Model stream is truncated.");
            return data[position] < 128 ? Byte() - 64 : Short() - 49152;
        }
        public int SignedByte() => (sbyte)Byte();
        public int Medium() => (Byte() << 16) | (Byte() << 8) | Byte();
    }
}
