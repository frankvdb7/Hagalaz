namespace Hagalaz.Cache.Abstractions.Types;

/// <summary>A model billboard (index 29, archive 0). Nullable members are absent opcodes; values are as stored.</summary>
public interface IBillboardType : IType
{
    /// <summary>Opcode 1: texture ID, 65535 meaning none.</summary>
    int? TextureId { get; set; }
    /// <summary>Opcode 2: half width and half height, each stored minus one.</summary>
    BillboardSize? Size { get; set; }
    /// <summary>Opcode 3: a byte the client reads but whose meaning is unknown.</summary>
    int? Opcode3 { get; set; }
    /// <summary>Opcode 4: blend mode.</summary>
    int? BlendMode { get; set; }
    /// <summary>Opcode 5: a byte only the software renderer uses.</summary>
    int? Opcode5 { get; set; }
    /// <summary>Opcode 6.</summary>
    bool HideInSpecialPass { get; set; }
    /// <summary>Opcode 7.</summary>
    bool HideFace { get; set; }

    /// <summary>The texture the client draws, or -1.</summary>
    int RenderTextureId { get; }
    int HalfWidth { get; }
    int HalfHeight { get; }
    /// <summary>The blend mode the client uses, 2 when absent.</summary>
    int RenderBlendMode { get; }
}

/// <summary>Billboard half sizes as stored, one less than the size used.</summary>
public readonly record struct BillboardSize(int Width, int Height);
