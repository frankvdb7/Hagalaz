using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;

namespace Hagalaz.Cache.Logic.Codecs;

/// <summary>
/// Revision-742 models in the newer layout: streams from the start of the file, then an optional version byte and a 23-byte
/// footer ending in FF FF. Encoding reproduces the stored bytes for canonically stored models (no zero delta behind a set
/// vertex flag, smallest smart forms), which every model in the cache is. The old layout and extended skin tables are rejected.
/// </summary>
public sealed class ModelTypeCodec : ITypeCodec<IModelType>
{
    private const int FooterLength = 23;
    private const int ShadingFlag = 0x01, ParticleFlag = 0x02, BillboardFlag = 0x04, VersionFlag = 0x08, SmartGroupFlag = 0x40;

    public IModelType Decode(int id, MemoryStream stream)
    {
        var data = stream.ToArray();
        if (data.Length < 2 || data[^1] != 255 || data[^2] != 255) throw new NotSupportedException("Old-layout models are not supported.");
        if (data.Length < FooterLength) throw new InvalidDataException("Model header is truncated.");
        var footer = new Reader(data, data.Length - FooterLength, data.Length);
        var vertices = footer.UShort();
        var faces = footer.UShort();
        var textureCount = footer.Byte();
        var flags = footer.Byte();
        var priority = footer.Byte();
        var alpha = Switch(footer.Byte());
        var faceSkins = Switch(footer.Byte());
        var textures = Switch(footer.Byte());
        var vertexSkins = Switch(footer.Byte());
        var xLength = footer.UShort();
        var yLength = footer.UShort();
        var zLength = footer.UShort();
        var indexLength = footer.UShort();
        var mappingLength = footer.UShort();
        if ((flags & 0x30) != 0) throw new NotSupportedException("Extended model skin tables are not supported.");
        if ((flags & 0x80) != 0) throw new NotSupportedException("Model flag 0x80 is not supported.");
        var streamEnd = (flags & VersionFlag) != 0 ? data.Length - FooterLength - 1 : data.Length - FooterLength;
        if (streamEnd < 0) throw new InvalidDataException("Model header is truncated.");
        var version = (flags & VersionFlag) != 0 ? data[streamEnd] : 12;
        if ((flags & VersionFlag) != 0 && version == 12) throw new NotSupportedException("A stored version byte of 12 is not supported.");

        var model = new ModelType(id) { Version = version, GlobalPriority = priority, UsesSmartBillboardGroups = (flags & SmartGroupFlag) != 0 };
        var vertexFlagsOffset = textureCount;
        var shadingOffset = vertexFlagsOffset + vertices;
        var codesOffset = shadingOffset + ((flags & ShadingFlag) != 0 ? faces : 0);
        var priorityOffset = codesOffset + faces;
        var faceSkinOffset = priorityOffset + (priority == 255 ? faces : 0);
        var vertexSkinOffset = faceSkinOffset + (faceSkins ? faces : 0);
        var alphaOffset = vertexSkinOffset + (vertexSkins ? vertices : 0);
        var indicesOffset = alphaOffset + (alpha ? faces : 0);
        var textureIdOffset = indicesOffset + indexLength;
        var pointerOffset = textureIdOffset + (textures ? faces * 2 : 0);
        var colorOffset = pointerOffset + mappingLength;
        var xOffset = colorOffset + faces * 2;
        var yOffset = xOffset + xLength;
        var zOffset = yOffset + yLength;
        var geometryEnd = zOffset + zLength;
        if (geometryEnd > streamEnd) throw new InvalidDataException("Model geometry streams overlap the header.");

        model.Positions = ReadPositions(data, vertices, vertexFlagsOffset, xOffset, yOffset, zOffset, geometryEnd);
        (model.Indices, model.StripCodes) = ReadFaces(data, faces, vertices, codesOffset, indicesOffset, textureIdOffset);
        model.FaceColors = Shorts(new Reader(data, colorOffset, xOffset), faces);
        if ((flags & ShadingFlag) != 0) model.ShadingTypes = Bytes(new Reader(data, shadingOffset, codesOffset), faces);
        if (priority == 255) model.FacePriorities = Bytes(new Reader(data, priorityOffset, faceSkinOffset), faces);
        if (faceSkins) model.FaceSkins = Bytes(new Reader(data, faceSkinOffset, vertexSkinOffset), faces);
        if (vertexSkins) model.VertexSkins = Bytes(new Reader(data, vertexSkinOffset, alphaOffset), vertices);
        if (alpha) model.FaceAlpha = Bytes(new Reader(data, alphaOffset, indicesOffset), faces);
        if (textures) model.TextureIds = Shorts(new Reader(data, textureIdOffset, pointerOffset), faces);

        var types = Bytes(new Reader(data, 0, vertexFlagsOffset), textureCount);
        if (types.FirstOrDefault(type => type > 3) is var unsupported and > 3) throw new NotSupportedException($"Unsupported texture projection {unsupported}.");
        ReadPointers(model, data, textureCount, pointerOffset, colorOffset);
        var attachmentOffset = ReadMappings(model, data, types, geometryEnd, streamEnd);
        ReadAttachments(model, new Reader(data, attachmentOffset, streamEnd), flags, faces, vertices);
        return model;
    }

    public MemoryStream Encode(IModelType model)
    {
        var vertices = model.Positions.Length / 3;
        var faces = model.FaceColors.Length;
        var output = new MemoryStream();
        foreach (var mapping in model.TextureMappings) output.Byte(mapping.Type);

        using var xs = new MemoryStream();
        using var ys = new MemoryStream();
        using var zs = new MemoryStream();
        int x = 0, y = 0, z = 0;
        for (var vertex = 0; vertex < vertices; vertex++)
        {
            var (dx, dy, dz) = (model.Positions[vertex * 3] - x, model.Positions[vertex * 3 + 1] - y, model.Positions[vertex * 3 + 2] - z);
            (x, y, z) = (model.Positions[vertex * 3], model.Positions[vertex * 3 + 1], model.Positions[vertex * 3 + 2]);
            output.Byte((dx != 0 ? 1 : 0) | (dy != 0 ? 2 : 0) | (dz != 0 ? 4 : 0));
            if (dx != 0) Smart(xs, dx);
            if (dy != 0) Smart(ys, dy);
            if (dz != 0) Smart(zs, dz);
        }
        if (model.ShadingTypes is { } shading) foreach (var value in shading) output.Byte(value);
        foreach (var code in model.StripCodes) output.Byte(code);
        if (model.GlobalPriority == 255) foreach (var value in Required(model.FacePriorities, "face priorities")) output.Byte(value);
        if (model.FaceSkins is { } faceSkins) foreach (var value in faceSkins) output.Byte(value);
        if (model.VertexSkins is { } vertexSkins) foreach (var value in vertexSkins) output.Byte(value);
        if (model.FaceAlpha is { } alpha) foreach (var value in alpha) output.Byte(value);
        using var indices = EncodeFaces(model, faces);
        indices.WriteTo(output);
        if (model.TextureIds is { } textureIds) foreach (var value in textureIds) output.UShort(value);
        var pointerStart = output.Length;
        if (model.TextureIds is { } textured && model.TextureMappings.Count > 0)
        {
            var pointers = Required(model.MappingPointers, "mapping pointers");
            for (var face = 0; face < faces; face++)
                if (textured[face] != 0) output.Byte(pointers[face]);
        }
        var mappingLength = (int)(output.Length - pointerStart);
        foreach (var color in model.FaceColors) output.UShort(color);
        xs.WriteTo(output);
        ys.WriteTo(output);
        zs.WriteTo(output);
        WriteMappings(output, model);
        WriteAttachments(output, model);
        if (model.Version != 12) output.Byte(model.Version);

        var flags = (model.ShadingTypes is not null ? ShadingFlag : 0) | (model.Emitters is not null || model.Effectors is not null ? ParticleFlag : 0)
            | (model.Billboards is not null ? BillboardFlag : 0) | (model.Version != 12 ? VersionFlag : 0) | (model.UsesSmartBillboardGroups ? SmartGroupFlag : 0);
        output.UShort(vertices);
        output.UShort(faces);
        output.Byte(model.TextureMappings.Count);
        output.Byte(flags);
        output.Byte(model.GlobalPriority);
        output.Byte(model.FaceAlpha is not null ? 1 : 0);
        output.Byte(model.FaceSkins is not null ? 1 : 0);
        output.Byte(model.TextureIds is not null ? 1 : 0);
        output.Byte(model.VertexSkins is not null ? 1 : 0);
        output.UShort(Length(xs));
        output.UShort(Length(ys));
        output.UShort(Length(zs));
        output.UShort(Length(indices));
        output.UShort(mappingLength);
        output.Byte(255);
        output.Byte(255);
        return output;
    }

    private static int[] ReadPositions(byte[] data, int vertices, int flagsOffset, int xOffset, int yOffset, int zOffset, int end)
    {
        var flags = new Reader(data, flagsOffset, flagsOffset + vertices);
        var (xs, ys, zs) = (new Reader(data, xOffset, yOffset), new Reader(data, yOffset, zOffset), new Reader(data, zOffset, end));
        var positions = new int[vertices * 3];
        int x = 0, y = 0, z = 0;
        for (var vertex = 0; vertex < vertices; vertex++)
        {
            var bits = flags.Byte();
            if ((bits & ~7) != 0) throw new InvalidDataException($"Vertex {vertex} has unknown flag bits.");
            if ((bits & 1) != 0) x += xs.Smart();
            if ((bits & 2) != 0) y += ys.Smart();
            if ((bits & 4) != 0) z += zs.Smart();
            (positions[vertex * 3], positions[vertex * 3 + 1], positions[vertex * 3 + 2]) = (x, y, z);
        }
        if (!xs.AtEnd || !ys.AtEnd || !zs.AtEnd) throw new InvalidDataException("Model coordinate stream lengths do not match its header.");
        return positions;
    }

    private static (int[] Indices, byte[] Codes) ReadFaces(byte[] data, int faces, int vertices, int codesOffset, int deltasOffset, int deltasEnd)
    {
        var codes = new Reader(data, codesOffset, codesOffset + faces);
        var deltas = new Reader(data, deltasOffset, deltasEnd);
        var indices = new int[faces * 3];
        var stored = new byte[faces];
        int a = 0, b = 0, c = 0, last = 0;
        for (var face = 0; face < faces; face++)
        {
            var code = codes.Byte();
            switch (code)
            {
                case 1: a = last + deltas.Smart(); b = a + deltas.Smart(); c = b + deltas.Smart(); break;
                case 2: b = c; c = last + deltas.Smart(); break;
                case 3: a = c; c = last + deltas.Smart(); break;
                case 4: (a, b) = (b, a); c = last + deltas.Smart(); break;
                default: throw new InvalidDataException("Unknown triangle topology opcode.");
            }
            last = c;
            if ((uint)a >= vertices || (uint)b >= vertices || (uint)c >= vertices) throw new InvalidDataException("Triangle references a missing vertex.");
            (indices[face * 3], indices[face * 3 + 1], indices[face * 3 + 2]) = (a, b, c);
            stored[face] = (byte)code;
        }
        if (!deltas.AtEnd) throw new InvalidDataException("Model index stream length does not match its header.");
        return (indices, stored);
    }

    private static MemoryStream EncodeFaces(IModelType model, int faces)
    {
        var deltas = new MemoryStream();
        int a = 0, b = 0, c = 0, last = 0;
        for (var face = 0; face < faces; face++)
        {
            var (na, nb, nc) = (model.Indices[face * 3], model.Indices[face * 3 + 1], model.Indices[face * 3 + 2]);
            var code = model.StripCodes[face];
            var continues = code switch
            {
                1 => true,
                2 => na == a && nb == c,
                3 => na == c && nb == b,
                4 => na == b && nb == a,
                _ => throw new InvalidOperationException($"Face {face} has strip code {code}."),
            };
            if (!continues) throw new InvalidOperationException($"Face {face} does not continue the strip as its code {code} requires.");
            if (code == 1) { Smart(deltas, na - last); Smart(deltas, nb - na); Smart(deltas, nc - nb); }
            else Smart(deltas, nc - last);
            (a, b, c, last) = (na, nb, nc, nc);
        }
        return deltas;
    }

    private static void ReadPointers(ModelType model, byte[] data, int textureCount, int offset, int end)
    {
        if (model.TextureIds is not { } textureIds || textureCount == 0)
        {
            if (end != offset) throw new InvalidDataException("Model has texture mapping pointers without textured faces.");
            return;
        }
        var reader = new Reader(data, offset, end);
        var pointers = new int[textureIds.Length];
        for (var face = 0; face < textureIds.Length; face++)
        {
            if (textureIds[face] == 0) continue;
            pointers[face] = reader.Byte();
            if (pointers[face] > textureCount) throw new InvalidDataException("Texture mapping references a missing triangle.");
        }
        if (!reader.AtEnd) throw new InvalidDataException("Model mapping pointer stream length does not match its header.");
        model.MappingPointers = pointers;
    }

    private static int ScaleWidth(int version) => version >= 15 ? 9 : version == 14 ? 7 : 6;

    private static int ReadMappings(ModelType model, byte[] data, int[] types, int geometryEnd, int streamEnd)
    {
        var simple = types.Count(type => type == 0);
        var complex = types.Length - simple;
        var boxes = types.Count(type => type == 2);
        var version = model.Version;
        var simpleReader = new Reader(data, geometryEnd, streamEnd);
        var complexReader = new Reader(data, geometryEnd + simple * 6, streamEnd);
        var scaleOffset = geometryEnd + simple * 6 + complex * 6;
        var scales = new Reader(data, scaleOffset, streamEnd);
        var rotationOffset = scaleOffset + complex * ScaleWidth(version);
        var rotations = new Reader(data, rotationOffset, streamEnd);
        var directions = new Reader(data, rotationOffset + complex, streamEnd);
        var offsets = new Reader(data, rotationOffset + complex * 2, streamEnd);
        foreach (var type in types)
        {
            if (type == 0)
            {
                model.TextureMappings.Add(new(type, simpleReader.UShort(), simpleReader.UShort(), simpleReader.UShort()));
                continue;
            }
            var (a, b, c) = (complexReader.UShort(), complexReader.UShort(), complexReader.UShort());
            var sx = version >= 15 ? scales.Medium() : scales.UShort();
            var sy = version >= 14 ? scales.Medium() : scales.UShort();
            var sz = version >= 15 ? scales.Medium() : scales.UShort();
            var (rotation, direction, offset) = (rotations.Byte(), directions.Byte(), offsets.Byte());
            var (oy, oz) = type == 2 ? (offsets.Byte(), offsets.Byte()) : (0, 0);
            model.TextureMappings.Add(new(type, a, b, c, sx, sy, sz, rotation, direction, offset, oy, oz));
        }
        return rotationOffset + complex * 3 + boxes * 2;
    }

    private static void WriteMappings(MemoryStream output, IModelType model)
    {
        foreach (var mapping in model.TextureMappings.Where(mapping => mapping.Type == 0)) Triangle(output, mapping);
        var complex = model.TextureMappings.Where(mapping => mapping.Type != 0).ToArray();
        foreach (var mapping in complex) Triangle(output, mapping);
        foreach (var mapping in complex)
        {
            if (model.Version >= 15) output.Medium(mapping.ScaleX); else output.UShort(mapping.ScaleX);
            if (model.Version >= 14) output.Medium(mapping.ScaleY); else output.UShort(mapping.ScaleY);
            if (model.Version >= 15) output.Medium(mapping.ScaleZ); else output.UShort(mapping.ScaleZ);
        }
        foreach (var mapping in complex) output.Byte(mapping.Rotation);
        foreach (var mapping in complex) output.Byte(mapping.Direction);
        foreach (var mapping in complex)
        {
            output.Byte(mapping.Offset);
            if (mapping.Type == 2) { output.Byte(mapping.OffsetY); output.Byte(mapping.OffsetZ); }
        }

        static void Triangle(MemoryStream output, ModelTextureMapping mapping)
        {
            output.UShort(mapping.A);
            output.UShort(mapping.B);
            output.UShort(mapping.C);
        }
    }

    private static void ReadAttachments(ModelType model, Reader reader, int flags, int faces, int vertices)
    {
        if ((flags & ParticleFlag) != 0)
        {
            var emitters = new List<ModelEmitterAttachment>();
            for (var count = reader.Byte(); count > 0; count--)
            {
                var (emitter, face) = (reader.UShort(), reader.UShort());
                if (face >= faces) throw new InvalidDataException("Particle emitter references a missing face.");
                emitters.Add(new(emitter, face));
            }
            var effectors = new List<ModelEffectorAttachment>();
            for (var count = reader.Byte(); count > 0; count--)
            {
                var (effector, vertex) = (reader.UShort(), reader.UShort());
                if (vertex >= vertices) throw new InvalidDataException("Particle effector references a missing vertex.");
                effectors.Add(new(effector, vertex));
            }
            (model.Emitters, model.Effectors) = (emitters, effectors);
        }
        if ((flags & BillboardFlag) != 0)
        {
            var billboards = new List<ModelBillboardAttachment>();
            for (var count = reader.Byte(); count > 0; count--)
            {
                var (definition, face) = (reader.UShort(), reader.UShort());
                if (face >= faces) throw new InvalidDataException("Model billboard references a missing face.");
                var group = model.UsesSmartBillboardGroups ? reader.NullableSmart() : reader.Byte();
                billboards.Add(new(definition, face, group, (sbyte)reader.Byte()));
            }
            model.Billboards = billboards;
        }
        if (!reader.AtEnd) throw new InvalidDataException("Model has data after its attachments.");
    }

    private static void WriteAttachments(MemoryStream output, IModelType model)
    {
        if (model.Emitters is not null || model.Effectors is not null)
        {
            var emitters = model.Emitters ?? [];
            output.Byte(emitters.Count);
            foreach (var emitter in emitters) { output.UShort(emitter.EmitterId); output.UShort(emitter.Face); }
            var effectors = model.Effectors ?? [];
            output.Byte(effectors.Count);
            foreach (var effector in effectors) { output.UShort(effector.EffectorId); output.UShort(effector.Vertex); }
        }
        if (model.Billboards is { } billboards)
        {
            output.Byte(billboards.Count);
            foreach (var billboard in billboards)
            {
                output.UShort(billboard.DefinitionId);
                output.UShort(billboard.Face);
                if (model.UsesSmartBillboardGroups) NullableSmart(output, billboard.Group);
                else output.Byte(billboard.Group);
                output.Byte(billboard.DepthOffset);
            }
        }
    }

    private static bool Switch(int value) => value switch
    {
        0 => false,
        1 => true,
        _ => throw new NotSupportedException($"Model stream switch value {value} is not supported."),
    };

    private static int[] Bytes(Reader reader, int count)
    {
        var values = new int[count];
        for (var index = 0; index < count; index++) values[index] = reader.Byte();
        return values;
    }

    private static int[] Shorts(Reader reader, int count)
    {
        var values = new int[count];
        for (var index = 0; index < count; index++) values[index] = reader.UShort();
        return values;
    }

    private static T Required<T>(T? value, string name) where T : class =>
        value ?? throw new InvalidOperationException($"The model's {name} are required by its switches.");

    private static int Length(MemoryStream stream) =>
        stream.Length <= ushort.MaxValue ? (int)stream.Length : throw new InvalidOperationException("A model stream exceeds 65535 bytes.");

    private static void Smart(MemoryStream stream, int value)
    {
        if (value is >= -64 and < 64) stream.Byte(value + 64);
        else if (value is >= -16384 and < 16384) stream.UShort(value + 49152);
        else throw new InvalidOperationException($"Model delta {value} does not fit a smart value.");
    }

    private static void NullableSmart(MemoryStream stream, int value)
    {
        if (value is >= -1 and < 127) stream.Byte(value + 1);
        else if (value is >= 127 and < 32767) stream.UShort(value + 32769);
        else throw new InvalidOperationException($"Billboard group {value} does not fit a smart value.");
    }

    private sealed class Reader(byte[] data, int position, int end)
    {
        public bool AtEnd => position == end;

        public int Byte() => position < end && position < data.Length ? data[position++] : throw new InvalidDataException("Model stream is truncated.");

        public int UShort() => (Byte() << 8) | Byte();

        public int Medium() => (Byte() << 16) | (Byte() << 8) | Byte();

        public int Smart()
        {
            if (position >= end || position >= data.Length) throw new InvalidDataException("Model stream is truncated.");
            return data[position] < 128 ? Byte() - 64 : UShort() - 49152;
        }

        public int NullableSmart()
        {
            var first = Byte();
            return first < 128 ? first - 1 : ((first << 8) | Byte()) - 32769;
        }
    }
}
