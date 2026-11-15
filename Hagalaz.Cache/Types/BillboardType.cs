using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types;

public sealed class BillboardType(int id) : IBillboardType
{
    public int Id { get; } = id;
    public int? TextureId { get; set; }
    public BillboardSize? Size { get; set; }
    public int? Opcode3 { get; set; }
    public int? BlendMode { get; set; }
    public int? Opcode5 { get; set; }
    public bool HideInSpecialPass { get; set; }
    public bool HideFace { get; set; }

    public int RenderTextureId => TextureId is null or 65535 ? -1 : TextureId.Value;
    public int HalfWidth => (Size?.Width ?? 63) + 1;
    public int HalfHeight => (Size?.Height ?? 63) + 1;
    public int RenderBlendMode => BlendMode ?? 2;
}
