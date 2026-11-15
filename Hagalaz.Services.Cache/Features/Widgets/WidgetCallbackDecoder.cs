using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Services.Cache.Features.Widgets;

internal static class WidgetCallbackDecoder
{
    // IComponentDefinition.parseIComponentDefinition, revision 742.
    private static readonly string[] Events =
    [
        "Open", "Mouse entered", "Mouse exited", "Use on", "Use", "Config changed",
        "Item container changed", "Skill changed", "Game loop", "Context menu clicked", "Item on item",
        "Mouse hover", "Mouse pressed", "Mouse held", "Mouse released", "Mouse dragged over",
        "Mouse dragged", "Mouse drag stopped", "Mouse scroll", "CS2 integer changed", "CS2 string changed"
    ];
    private static readonly int[] TriggerSlots = [5, 6, 7, 19, 20];
    private static readonly Encoding ClientEncoding = CodePagesEncodingProvider.Instance.GetEncoding(1252)!;

    public static WidgetCallback[]? Decode(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        try
        {
            var version = stream.ReadUnsignedByte();
            var type = stream.ReadUnsignedByte();
            if ((type & 128) != 0) stream.ReadString();
            SkipComponentLayout(stream, type & 127, version);
            SkipComponentSettings(stream, version);
            return ReadCallbacks(stream, version);
        }
        catch (EndOfStreamException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static void SkipComponentLayout(MemoryStream stream, int type, int version)
    {
        stream.Position += 10; // Content type, position and dimensions.
        var widthMode = stream.ReadUnsignedByte();
        var heightMode = stream.ReadUnsignedByte();
        stream.Position += 5; // Position modes, parent and flags.
        switch (type)
        {
            case 0:
                stream.Position += version == 255 ? 5 : 4;
                break;
            case 3:
            case 9:
                stream.Position += 6;
                break;
            case 4:
                stream.ReadBigSmart();
                if (version is >= 2 and < 255) stream.Position++;
                stream.ReadString();
                stream.Position += version == 255 ? 9 : 10;
                break;
            case 5:
                stream.Position += version is >= 3 and < 255 ? 20 : 19;
                break;
            case 6:
                stream.ReadBigSmart();
                var flags = stream.ReadUnsignedByte();
                if ((flags & 1) != 0) stream.Position += 12;
                else if ((flags & 2) != 0) stream.Position += 14;
                stream.ReadBigSmart();
                if (widthMode != 0) stream.Position += 2;
                if (heightMode != 0) stream.Position += 2;
                break;
        }
    }

    private static void SkipComponentSettings(MemoryStream stream, int version)
    {
        var settings = stream.ReadMedInt();
        while (stream.ReadUnsignedByte() != 0) stream.Position += 3;
        stream.ReadString();
        var options = stream.ReadUnsignedByte();
        for (var i = 0; i < (options & 15); i++) stream.ReadString();
        stream.Position += Math.Min(options >> 4, 2) * 3;
        stream.ReadString();
        stream.Position += 3;
        stream.ReadString();
        if (((settings >> 11) & 127) != 0) stream.Position += 6;
        if (version == 255) return;

        stream.Position += 2;
        var intCount = stream.ReadUnsignedByte();
        stream.Position += intCount * 7;
        var stringCount = stream.ReadUnsignedByte();
        for (var i = 0; i < stringCount; i++)
        {
            stream.Position += 3;
            if (stream.ReadUnsignedByte() != 0) throw new InvalidDataException("Invalid widget parameter string.");
            stream.ReadString();
        }
    }

    private static WidgetCallback[] ReadCallbacks(MemoryStream stream, int version)
    {
        var callbacks = new Dictionary<int, WidgetCallback>();
        for (var slot = 0; slot < Events.Length; slot++)
        {
            if (slot == 10 && version == 255) continue;
            var args = new WidgetCallbackValue[stream.ReadUnsignedByte()];
            if (args.Length == 0) continue;
            for (var i = 0; i < args.Length; i++) args[i] = ReadValue(stream);
            // Class163 reads the numeric script target from callParams[0].
            int? script = args[0] is { Type: "int", Value: int id } && id >= 0 ? id : null;
            callbacks.Add(slot, new WidgetCallback(Events[slot], script, script.HasValue ? args[1..] : args, []));
        }
        foreach (var slot in TriggerSlots)
        {
            var triggers = new int[stream.ReadUnsignedByte()];
            for (var i = 0; i < triggers.Length; i++) triggers[i] = stream.ReadInt();
            if (callbacks.TryGetValue(slot, out var callback)) callbacks[slot] = callback with { Triggers = triggers };
        }
        return [.. callbacks.Values];
    }

    private static WidgetCallbackValue ReadValue(MemoryStream stream) => stream.ReadUnsignedByte() switch
    {
        0 => new("int", stream.ReadInt()),
        1 => new("string", ClientEncoding.GetString(Encoding.Latin1.GetBytes(stream.ReadString()))),
        var tag => new("unknown", tag)
    };
}
