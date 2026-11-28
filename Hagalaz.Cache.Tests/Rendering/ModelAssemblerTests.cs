using Hagalaz.Cache.Logic.Rendering;
using Xunit;

namespace Hagalaz.Cache.Tests.Rendering;

public class ModelAssemblerTests
{
    // Version-12 triangle (0,0,0), (10,0,0), (0,20,0) with face colour 127 and one projected texture face.
    private static readonly byte[] TexturedTriangleModel = [0, 0, 1, 3, 1, 64, 65, 65, 0, 43, 1, 0, 127, 74, 54, 84, 0, 0, 0, 1, 0, 2,
        0, 3, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 2, 0, 1, 0, 0, 0, 3, 0, 1, 255, 255];

    private const int ItemIndex = 19, NpcIndex = 18, ObjectIndex = 16;

    // Ordered colour pairs 127 -> 300 -> 50000, texture pairs, and an x/y scale of 256/64, with each kind's model opcode first.
    private static byte[] Definition(byte[] models, byte scaleXOpcode, params byte[] extra)
    {
        using var stream = new MemoryStream();
        stream.Write(models);
        stream.WriteByte(40); stream.WriteByte(2);
        Short(127); Short(300); Short(300); Short(50000);
        stream.WriteByte(41); stream.WriteByte(2);
        Short(42); Short(50000); Short(50000); Short(60000);
        stream.WriteByte(scaleXOpcode); Short(256);
        stream.WriteByte((byte)(scaleXOpcode + 1)); Short(64);
        stream.Write(extra);
        stream.WriteByte(0);
        return stream.ToArray();

        void Short(int value) { stream.WriteByte((byte)(value >> 8)); stream.WriteByte((byte)value); }
    }

    private static readonly byte[] ObjectShape10 = [1, 1, 10, 1, 0, 0];

    private static RenderingTestCache Cache(int index, params (int Id, byte[] Data)[] definitions) =>
        new RenderingTestCache().Container(ModelAssembler.ModelIndex, 0, TexturedTriangleModel).Archive(index, 0, definitions);

    [Fact]
    public void AddGameCache_RegistersTheRenderingModules()
    {
        using var cache = new RenderingTestCache();

        Assert.NotNull(cache.Get<ModelAssembler>());
        Assert.NotNull(cache.Get<ItemIconComposer>());
    }

    [Fact]
    public void Model_KeepsStoredUnitsWhileClientUnitsScaleOldModels()
    {
        using var cache = new RenderingTestCache().Container(ModelAssembler.ModelIndex, 0, TexturedTriangleModel);
        var assembler = cache.Get<ModelAssembler>();

        var stored = assembler.Model(0);
        var client = assembler.ModelInClientUnits(0);

        Assert.Equal([0, 0, 0, 10, 0, 0, 0, 20, 0], stored.Positions);
        Assert.Equal(0.25f, stored.CoordinateScale);
        Assert.Null(stored.AnimationIds);
        Assert.Equal([0, 0, 0, 40, 0, 0, 0, 80, 0], client.Positions);
        Assert.Equal(1, client.CoordinateScale);
    }

    [Fact]
    public void Item_AppliesInventoryScaleAndOrderedRecolours()
    {
        using var cache = Cache(ItemIndex, (0, Definition([1, 0, 0], 110)));

        var item = cache.Get<ModelAssembler>().Item(0, ModelAssembler.InventoryAppearance);

        Assert.True(item.IsSuccess);
        Assert.Equal("Inventory", item.Value.Appearance.Label);
        Assert.Equal([0, 0, 0, 80, 0, 0, 0, 40, 0], item.Value.Model.Positions);
        Assert.Equal([50000], item.Value.Model.FaceColors);
        Assert.Empty(item.Value.Model.Notes);
    }

    [Fact]
    public void Item_ReportsUnknownAndUnavailableAppearances()
    {
        using var cache = Cache(ItemIndex, (0, Definition([1, 0, 0], 110)));
        var assembler = cache.Get<ModelAssembler>();

        Assert.True(assembler.Item(0, "bogus").HasError<UnknownAppearanceError>());
        Assert.True(assembler.Item(0, "male-equipped").HasError<ModelSubjectAbsentError>());
    }

    [Fact]
    public void Npc_ReportsMissingModelsAndUndecodableRenderAnimationsAsNote()
    {
        using var cache = Cache(NpcIndex, (0, Definition([1, 1, 0, 0], 97, 127, 0, 5)), (1, [0]));
        var assembler = cache.Get<ModelAssembler>();

        var npc = assembler.Npc(0);

        Assert.True(npc.IsSuccess);
        Assert.Single(npc.Value.Notes);
        Assert.StartsWith("Render-animation associations could not be decoded: ", npc.Value.Notes[0]);
        Assert.Equal([2f, 0.5f, 2f], npc.Value.Scale);
        Assert.True(assembler.Npc(1).HasError<ModelSubjectAbsentError>());
    }

    [Fact]
    public void SelectObjectShape_ReportsAbsentShapesAndInvertedObjects()
    {
        using var cache = Cache(ObjectIndex, (0, Definition(ObjectShape10, 65)), (1, [0]), (2, Definition(ObjectShape10, 65, 62)));
        var assembler = cache.Get<ModelAssembler>();

        var selection = assembler.SelectObjectShape(0, null);
        Assert.True(selection.IsSuccess);
        Assert.Equal(10, selection.Value.Shape);
        Assert.Equal([0, 0, 0, 80, 0, 0, 0, 40, 0], assembler.ObjectShape(selection.Value).Positions);
        Assert.True(assembler.SelectObjectShape(0, 22).HasError<ModelSubjectAbsentError>());
        Assert.True(assembler.SelectObjectShape(1, null).HasError<ModelSubjectAbsentError>());
        var inverted = assembler.SelectObjectShape(2, null);
        Assert.Equal("Inverted object models are not yet supported.", Assert.IsType<UnsupportedAppearanceError>(inverted.Errors.Single()).Message);
    }

    [Theory]
    [InlineData(2, 2, 2)]
    [InlineData(8, 4, 2)]
    [InlineData(6, 4, 1)]
    [InlineData(5, 4, 1)]
    [InlineData(10, 10, 1)]
    public void ObjectPlacement_MapsShapesToModelShapesAndOrientations(int shape, int modelShape, int parts)
    {
        using var cache = Cache(ObjectIndex, (0, [1, 1, (byte)modelShape, 1, 0, 0, 0]));

        var placement = cache.Get<ModelAssembler>().ObjectPlacement(0, shape, 1);

        Assert.True(placement.IsSuccess);
        Assert.Equal(parts, placement.Value.Parts.Length);
    }

    [Fact]
    public void ObjectPlacement_MirrorsTheFirstCornerWallPart()
    {
        using var cache = Cache(ObjectIndex, (0, [1, 1, 2, 1, 0, 0, 0]));

        var placement = cache.Get<ModelAssembler>().ObjectPlacement(0, 2, 0).Value;

        Assert.Equal([0, 2, 1], placement.Parts[0].Indices);
        Assert.Equal([0, 1, 2], placement.Parts[1].Indices);
    }

    [Fact]
    public void ObjectPlacement_ReportsAbsentModelShape()
    {
        using var cache = Cache(ObjectIndex, (0, [.. ObjectShape10, 0]));

        Assert.True(cache.Get<ModelAssembler>().ObjectPlacement(0, 0, 0).HasError<ModelSubjectAbsentError>());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ObjectPlacement_ChoosesTheZeroStateTransformVariant(bool selector, bool hidden)
    {
        var fallback = hidden ? 32767 : 2;
        byte[] root = [92, 255, 255, selector ? (byte)0 : (byte)255, selector ? (byte)1 : (byte)255, (byte)(fallback >> 8), (byte)fallback, 0, 0, 1, 0];
        using var cache = Cache(ObjectIndex, (0, root), (1, [.. ObjectShape10, 14, 2, 0]), (2, [.. ObjectShape10, 14, 3, 0]));

        var placement = cache.Get<ModelAssembler>().ObjectPlacement(0, 10, 0).Value;

        if (hidden)
        {
            Assert.Empty(placement.Parts);
            Assert.Equal(["Hidden for the definition's default transform fallback; live game state is unavailable."], placement.Warnings);
        }
        else
        {
            Assert.Single(placement.Parts);
            Assert.Equal(2, placement.SizeX);
            Assert.Equal(["Preview uses object 1 for assumed zero-valued varp/varbit state; live game state is unavailable."], placement.Warnings);
        }
    }

    [Fact]
    public void ObjectPlacement_ReportsTheClientFloorShadowStrength()
    {
        using var cache = Cache(ObjectIndex, (0, [.. ObjectShape10, 0]), (1, [.. ObjectShape10, 64, 0]), (2, [.. ObjectShape10, 98, 0]));
        var assembler = cache.Get<ModelAssembler>();

        var casting = assembler.ObjectPlacement(0, 10, 0).Value;
        var positions = casting.Parts[0].Positions;
        var radius = (int)(Math.Sqrt(Enumerable.Range(0, positions.Length / 3).Max(vertex => (long)positions[vertex * 3] * positions[vertex * 3] + (long)positions[vertex * 3 + 2] * positions[vertex * 3 + 2])) + 0.99);

        Assert.Equal(Math.Max(1, Math.Min(30, radius / 4)), casting.ShadowStrength);
        Assert.Equal(0, assembler.ObjectPlacement(1, 10, 0).Value.ShadowStrength);
        Assert.Equal(15, assembler.ObjectPlacement(2, 10, 0).Value.ShadowStrength);
    }
}
