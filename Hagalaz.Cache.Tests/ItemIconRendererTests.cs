using Hagalaz.Cache.Logic.Rendering;
using Xunit;
using static Hagalaz.Cache.Logic.Rendering.ItemIconRenderer;

namespace Hagalaz.Cache.Tests;

public sealed class ItemIconRendererTests
{
    private static readonly Dictionary<int, Texture> NoTextures = [];
    // The icon light from ItemDefinition.method6279 with its random intensity fixed at 1.0.
    private static readonly ClientModelShading.Light IconLight = new(1f, 1f, 1f, -50, -10, -50);

    [Fact]
    public void LightModel_MatchesTheClientLightingPortUsedByTheModelViewer()
    {
        // Expected values come from shared/model-viewer/model-lighting.ts lightModel with its default atmosphere light.
        int[] positions = [0, 0, 0, 100, 0, 0, 0, 100, 0, 0, 0, 100, 37, -80, 12];
        int[] indices = [0, 1, 2, 0, 3, 1, 1, 3, 2, 2, 3, 0, 4, 1, 2];
        int[] shading = [0, 1, 0, 0, 1];
        var shades = ClientModelShading.LightModel(positions, indices, shading, 850, new ClientModelShading.Light(1.1523438f, 0.69921875f, 1.2f, -50, -60, -50));
        Assert.Equal([71, 177, 142, 57, 57, 57, 177, 177, 142, 142, 177, 71, 59, 59, 59], shades);
    }

    [Fact]
    public void LitFaceColor_MatchesTheClientPalettePortUsedByTheModelViewer()
    {
        // Expected values come from model-lighting.ts litFaceColor with ambient 80.
        int[] expected = [855309, 920845, 855309, 920845, 855309, 920845, 855309, 920845, 855309, 920845, 7301738, 10655124, 9998737, 14210003, 11774891, 16579579,
            656397, 918549, 5645431, 7807915, 7680674, 10629097, 9124800, 12283122, 527623, 529159, 725514, 925196, 989710, 1255696, 1187344, 1387794,
            852482, 1442562, 7017999, 10098957, 9641493, 13771539, 11281432, 16197910, 855309, 855310, 4342341, 6052965, 5921374, 8289930, 6974063, 9737634];
        var actual = new List<int>();
        foreach (var packed in new[] { 0, 127, 50000, 21000, 960, 43072 })
        foreach (var intensity in new[] { 2, 64, 100, 126 })
        foreach (var textured in new[] { false, true })
            actual.Add(ClientModelShading.LitFaceColor(packed, intensity, textured, 80));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RenderFitted_CentresAndFillsTheImageRegardlessOfModelPositionAndSize()
    {
        static (int MinX, int MinY, int MaxX, int MaxY) Covered(int[] pixels, int width)
        {
            var points = pixels.Select((pixel, index) => (pixel, index)).Where(entry => entry.pixel != 0).Select(entry => (X: entry.index % width, Y: entry.index / width)).ToArray();
            return (points.Min(point => point.X), points.Min(point => point.Y), points.Max(point => point.X), points.Max(point => point.Y));
        }
        // A closed box, so faces toward the fixed preview angle are always drawn.
        static Model Box(int size, int shift)
        {
            int[] positions = [0, 0, 0, size, 0, 0, size, size, 0, 0, size, 0, 0, 0, size, size, 0, size, size, size, size, 0, size, size];
            for (var offset = 0; offset < positions.Length; offset++) positions[offset] += shift;
            int[] indices = [0, 1, 2, 0, 2, 3, 5, 4, 7, 5, 7, 6, 4, 0, 3, 4, 3, 7, 1, 5, 6, 1, 6, 2, 4, 5, 1, 4, 1, 0, 3, 2, 6, 3, 6, 7, 2, 1, 0, 3, 2, 0, 7, 4, 5, 6, 7, 5, 3, 0, 4, 7, 3, 4, 6, 5, 1, 2, 6, 1, 1, 5, 4, 0, 1, 4, 6, 2, 3, 7, 6, 3];
            var faces = indices.Length / 3;
            return new Model(positions, indices, Enumerable.Repeat(0x7f7f & 0xffff, faces).ToArray(), new int[faces], null, null, null, 64, 850);
        }
        var near = Covered(RenderFitted(Box(100, 0), NoTextures, 64, 64), 64);
        var large = Covered(RenderFitted(Box(40000, 9000), NoTextures, 64, 64), 64);
        foreach (var bounds in new[] { near, large })
        {
            Assert.InRange(bounds.MaxX - bounds.MinX + 1, 50, 62);
            Assert.InRange(bounds.MaxY - bounds.MinY + 1, 40, 62);
            Assert.InRange(bounds.MinX + bounds.MaxX, 60, 66);
            Assert.InRange(bounds.MinY + bounds.MaxY, 60, 66);
        }
        Assert.All(RenderFitted(new Model([], [], [], [], null, null, null, 64, 850), NoTextures, 8, 8), pixel => Assert.Equal(0, pixel));
        // A far-away vertex that only hidden faces use does not affect the framing.
        var box = Box(100, 0);
        var stray = box with { Positions = [.. box.Positions, 90000, 90000, 90000], Indices = [.. box.Indices, 0, 1, 8], FaceColors = [.. box.FaceColors, 0], FaceAlpha = [.. box.FaceAlpha, 255] };
        Assert.Equal(near, Covered(RenderFitted(stray, NoTextures, 64, 64), 64));
    }

    [Fact]
    public void Project_UsesTheClientIconCameraAndPreservesFramingWhenScaled()
    {
        var model = Triangle([0, 1, 2]);
        Assert.Equal([16f, 16f, 3200f, 32f, 16f, 3200f, 16f, 32f, 3200f], Project(model, new Camera(800, 0, 0, 0, 0, 0), ZoomMode.Normal, 36, 32));
        // Twice the client size doubles the focal length and centre.
        Assert.Equal([32f, 32f, 3200f, 64f, 32f, 3200f, 32f, 64f, 3200f], Project(model, new Camera(800, 0, 0, 0, 0, 0), ZoomMode.Normal, 72, 64));
        // Extra width centres the client frame.
        Assert.Equal([34f, 16f, 3200f, 50f, 16f, 3200f, 34f, 32f, 3200f], Project(model, new Camera(800, 0, 0, 0, 0, 0), ZoomMode.Normal, 72, 32));
        // The note's inner item uses zoom ×1.5 and outline level 2 uses ×1.04.
        Assert.Equal(16 + 512 * 100 / 4800f, Project(model, new Camera(800, 0, 0, 0, 0, 0), ZoomMode.NoteContent, 36, 32)[3], 3);
        Assert.Equal(16 + 512 * 100 / 3328f, Project(model, new Camera(800, 0, 0, 0, 0, 0), ZoomMode.Outlined, 36, 32)[3], 3);
        // Offsets translate by four units each; offset2 moves both down and away, as in the client.
        var offset = Project(model, new Camera(800, 0, 0, 0, 25, 25), ZoomMode.Normal, 36, 32);
        Assert.Equal(16 + 512 * 100 / 3300f, offset[0], 3);
        Assert.Equal(16 + 512 * 100 / 3300f, offset[1], 3);
        Assert.Equal(3300f, offset[2], 3);
    }

    [Fact]
    public void Project_RotatesAboutZThenYBeforeTranslatingAndTiltingAboutX()
    {
        var model = Triangle([0, 1, 2]);
        // A quarter pitch moves the camera above the item; the origin stays at the projection centre at the same distance.
        var pitched = Project(model, new Camera(800, 512, 0, 0, 0, 0), ZoomMode.Normal, 36, 32);
        Assert.Equal(16f, pitched[0], 3);
        Assert.Equal(16f, pitched[1], 3);
        Assert.Equal(3200f, pitched[2], 0);
        // A quarter yaw turns +X away from the camera.
        var yawed = Project(model, new Camera(800, 0, 512, 0, 0, 0), ZoomMode.Normal, 36, 32);
        Assert.Equal(16f, yawed[3], 3);
        Assert.Equal(3100f, yawed[5], 0);
        // A quarter roll (negative zan2d in client angle units) turns +X into screen +Y.
        var rolled = Project(model, new Camera(800, 0, 0, -512, 0, 0), ZoomMode.Normal, 36, 32);
        Assert.Equal(16f, rolled[3], 3);
        Assert.Equal(32f, rolled[4], 3);
    }

    [Fact]
    public void RenderModel_DrawsFrontFacesWithLitColourAndCullsBackFaces()
    {
        var front = RenderModel(Triangle([0, 2, 1]), new Camera(800, 0, 0, 0, 0, 0), NoTextures, ZoomMode.Normal, 36, 32);
        var back = RenderModel(Triangle([0, 1, 2]), new Camera(800, 0, 0, 0, 0, 0), NoTextures, ZoomMode.Normal, 36, 32);
        Assert.All(back, pixel => Assert.Equal(0, pixel));
        Assert.Equal(0, front[0]);
        var inside = front[20 * 36 + 18];
        Assert.Equal(0xFF, (inside >>> 24));
        var shade = ClientModelShading.LightModel([0, 0, 0, 100, 0, 0, 0, 100, 0], [0, 2, 1], [], 768, IconLight)[0];
        Assert.Equal(unchecked((int)0xFF000000) | ClientModelShading.LitFaceColor(960, shade, textured: false, 64), inside);
        // Pixel centres inside or on the edges of the projected right triangle with 16-pixel legs: 1 + 2 + ... + 16.
        Assert.Equal(136, front.Count(pixel => pixel != 0));
    }

    [Fact]
    public void RenderModel_HidesFlaggedFacesAndBlendsTranslucentFacesOverOpaqueOnes()
    {
        var camera = new Camera(800, 0, 0, 0, 0, 0);
        Assert.All(RenderModel(Triangle([0, 2, 1], alpha: 255), camera, NoTextures, ZoomMode.Normal, 36, 32), pixel => Assert.Equal(0, pixel));
        var opaque = RenderModel(Triangle([0, 2, 1]), camera, NoTextures, ZoomMode.Normal, 36, 32)[20 * 36 + 18];
        var model = new Model([0, 0, 0, 100, 0, 0, 0, 100, 0, 0, 0, -50, 100, 0, -50, 0, 100, -50], [0, 2, 1, 3, 5, 4], [960, 43072], [0, 128], null, null, null, 64, 768);
        var blended = RenderModel(model, camera, NoTextures, ZoomMode.Normal, 36, 32)[20 * 36 + 18];
        var shade = ClientModelShading.LightModel(model.Positions, model.Indices, [], 768, IconLight)[3];
        var front = unchecked((int)0xFF000000) | ClientModelShading.LitFaceColor(43072, shade, textured: false, 64);
        for (var shift = 0; shift <= 16; shift += 8)
        {
            var expected = (front >> shift & 255) * (127 / 255f) + (opaque >> shift & 255) * (128 / 255f);
            Assert.InRange(blended >> shift & 255, (int)expected - 1, (int)expected + 1);
        }
    }

    [Fact]
    public void RenderModel_SamplesTexturesPerspectiveCorrectlyAndSkipsCutOutTexels()
    {
        // Left half red, right half transparent.
        int[] texels = [unchecked((int)0xFFFF0000), 0, unchecked((int)0xFFFF0000), 0];
        var textures = new Dictionary<int, Texture> { [5] = new Texture(2, 2, texels, 0, 0) };
        var model = new Model([0, 0, 0, 100, 0, 0, 0, 100, 0], [0, 2, 1], [127], [0], null, [5], [0, 0, 0, 1, 1, 0], 64, 768);
        var pixels = RenderModel(model, new Camera(800, 0, 0, 0, 0, 0), textures, ZoomMode.Normal, 36, 32);
        var left = pixels[18 * 36 + 17];
        Assert.NotEqual(0, left);
        Assert.Equal(0, left & 0xFFFF);
        Assert.True((left >> 16 & 255) > 0);
        Assert.Equal(0, pixels[17 * 36 + 28]);
    }

    [Fact]
    public void OutlineAndShadow_ReproduceTheClientPixelPasses()
    {
        const int o = DarkOutline, s = 0x302020, p = 0x123456;
        int[] source =
        [
            0, 0, 0, 0,
            0, p, 0, 0,
            0, 0, 0, 0,
        ];
        var outlined = Outline(source, 4, 3, o);
        Assert.Equal([0, o, 0, 0, o, p, o, 0, 0, o, 0, 0], outlined);
        Shadow(outlined, 4, 3, s);
        Assert.Equal([0, o, 0, 0, o, p, o, 0, 0, o, s, s], outlined);

        int[] composed = [0, 0, 1, 0];
        Draw(composed, [2, 0, 0, 0x000000]);
        Assert.Equal([2, 0, 1, 0], composed);
        int[] normalized = [unchecked((int)0xFF000000), s, 0];
        Normalize(normalized);
        Assert.Equal([0, unchecked((int)0xFF302020), 0], normalized);
    }

    [Theory]
    [InlineData(1, 0, true, null)]
    [InlineData(5, 0, true, null)]
    [InlineData(1, 1, false, "<col=ffff00>1</col>")]
    [InlineData(-1, 1, false, "<col=ffff00>-1</col>")]
    [InlineData(1, 2, false, null)]
    [InlineData(1, 2, true, "<col=ffff00>1</col>")]
    [InlineData(2, 2, false, "<col=ffff00>2</col>")]
    [InlineData(-1, 2, true, null)]
    [InlineData(99999, 2, true, "<col=ffff00>99999</col>")]
    [InlineData(100000, 2, true, "<col=ffffff>100K</col>")]
    [InlineData(9999999, 2, true, "<col=ffffff>9999K</col>")]
    [InlineData(10000000, 2, true, "<col=00ff80>10M</col>")]
    public void QuantityLabel_FollowsTheClientModesAndThresholds(int quantity, int mode, bool stackable, string? expected) =>
        Assert.Equal(expected, QuantityLabel(quantity, mode, stackable));

    [Theory]
    [InlineData(1, true, "<col=ffff00>1</col>")]
    [InlineData(500, true, "<col=ffff00>500</col>")]
    [InlineData(0, true, null)]
    [InlineData(-1, true, null)]
    [InlineData(1, false, null)]
    [InlineData(500, false, null)]
    public void QuantityLabel_WithoutAModeLabelsOnlyStackableItemsWithAPositiveQuantity(int quantity, bool stackable, string? expected) =>
        Assert.Equal(expected, QuantityLabel(quantity, null, stackable));

    [Fact]
    public void StackVariant_PicksTheHighestReachedAmountLikeTheClient()
    {
        int[] ids = [11, 12, 13, 0, 0, 0, 0, 0, 0, 0];
        int[] amounts = [2, 25, 1000, 0, 0, 0, 0, 0, 0, 0];
        Assert.Equal(7, StackVariant(7, 1, ids, amounts));
        Assert.Equal(11, StackVariant(7, 2, ids, amounts));
        Assert.Equal(12, StackVariant(7, 999, ids, amounts));
        Assert.Equal(13, StackVariant(7, 5000, ids, amounts));
        Assert.Equal(7, StackVariant(7, 5000, null, null));
    }

    [Fact]
    public void ClientFont_DrawsGlyphMasksWithColourTagsAndShadowFromTheBaseline()
    {
        var metrics = new byte[263];
        metrics[2 + '1'] = 3;
        metrics[258] = 2;
        var empty = new ClientFont.Glyph(2, 2, new bool[4]);
        var glyphs = Enumerable.Repeat(empty, 256).ToArray();
        glyphs['1'] = new ClientFont.Glyph(2, 2, [true, false, false, true]);
        var font = new ClientFont(metrics, glyphs);
        var pixels = new int[8 * 6];
        font.Draw(pixels, 8, 6, "<col=00ff80>1</col>1", 0, 3, QuantityColor, QuantityShadow);
        // Baseline 3 minus ascent 2 puts the glyph canvas top at row 1; the shadow is drawn first at (+1, +1).
        Assert.Equal(unchecked((int)0xFF00FF80), pixels[1 * 8 + 0]);
        Assert.Equal(unchecked((int)0xFF00FF80), pixels[2 * 8 + 1]);
        Assert.Equal(QuantityShadow, pixels[3 * 8 + 2]);
        // The second glyph advances by three pixels and returns to the default colour.
        Assert.Equal(QuantityColor, pixels[1 * 8 + 3]);
        Assert.Equal(QuantityColor, pixels[2 * 8 + 4]);
        Assert.Throws<NotSupportedException>(() => new ClientFont([0, 1, .. new byte[261]], glyphs));
    }

    [Fact]
    public void ClientFont_ReadsThePinnedP11FullMetrics()
    {
        var metrics = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "fonts", "p11_full.metrics"));
        var glyphs = Enumerable.Repeat(new ClientFont.Glyph(1, 1, [false]), 256).ToArray();
        Assert.Equal(10, new ClientFont(metrics, glyphs).Ascent);
    }

    private static Model Triangle(int[] indices, int alpha = 0) =>
        new([0, 0, 0, 100, 0, 0, 0, 100, 0], indices, [960], [alpha], null, null, null, 64, 768);
}
