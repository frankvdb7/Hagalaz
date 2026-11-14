using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Logic.Rendering;
using Xunit;

namespace Hagalaz.Cache.Tests.Rendering;

public class ItemIconComposerTests
{
    // Version-12 triangle facing -Z; test items yaw half a turn (rotation2 1024) to face the icon camera.
    private static readonly byte[] TriangleModel = [0, 0, 1, 3, 1, 64, 65, 65, 0, 43, 1, 0, 127, 74, 54, 84, 0, 0, 0, 1, 0, 2,
        0, 3, 0, 1, 1, 0, 0, 0, 0, 1, 0, 0, 2, 0, 1, 0, 0, 0, 3, 0, 1, 255, 255];

    private static readonly IconOptions Plain = new(36, 32, 1, ItemIconRenderer.InventoryShadow, 1, null);

    private static byte[] Item(int zoom = 800, int modelId = 0, (int Id, int Template)? note = null, (int Id, int Template)? lend = null,
        bool stackable = false, (int Id, int Amount)? stack = null)
    {
        var bytes = new List<byte>();
        bytes.AddRange([1, (byte)(modelId >> 8), (byte)modelId]);
        bytes.AddRange([4, (byte)(zoom >> 8), (byte)zoom]);
        bytes.AddRange([6, 4, 0]);
        if (note is { } noted) bytes.AddRange([97, (byte)(noted.Id >> 8), (byte)noted.Id, 98, (byte)(noted.Template >> 8), (byte)noted.Template]);
        if (lend is { } lent) bytes.AddRange([121, (byte)(lent.Id >> 8), (byte)lent.Id, 122, (byte)(lent.Template >> 8), (byte)lent.Template]);
        if (stackable) bytes.Add(11);
        if (stack is { } variant) bytes.AddRange([100, (byte)(variant.Id >> 8), (byte)variant.Id, (byte)(variant.Amount >> 8), (byte)variant.Amount]);
        bytes.Add(0);
        return bytes.ToArray();
    }

    private static RenderingTestCache Cache(params byte[][] items) => new RenderingTestCache()
        .Container(ModelAssembler.ModelIndex, 0, TriangleModel)
        .Archive(19, 0, items.Select((data, id) => (id, data)).ToArray());

    [Fact]
    public void SourceIndexes_KeepTheValidatorOrder() =>
        Assert.Equal([19, 16, 7, 8, 13, 9, 26], ItemIconComposer.SourceIndexes);

    [Fact]
    public void Render_DrawsTheInventoryModel()
    {
        using var cache = Cache(Item());

        var pixels = cache.Get<ItemIconComposer>().Render(0, Plain);

        Assert.True(pixels.IsSuccess);
        Assert.Equal(36 * 32, pixels.Value.Length);
        Assert.Contains(pixels.Value, argb => argb != 0);
    }

    [Fact]
    public void Render_UsesTheStackVariantForTheQuantity()
    {
        using var cache = Cache(Item(stack: (1, 5)), Item(zoom: 1400));
        var composer = cache.Get<ItemIconComposer>();

        var variant = composer.Render(0, Plain with { Quantity = 5 }).Value;

        Assert.Equal(composer.Render(1, Plain with { Quantity = 5 }).Value, variant);
        Assert.NotEqual(composer.Render(0, Plain).Value, variant);
    }

    [Fact]
    public void Render_LayersNotedAndLentItems()
    {
        using var cache = Cache(Item(), Item(zoom: 1400), Item(note: (0, 1)), Item(lend: (0, 1))).Font();
        var composer = cache.Get<ItemIconComposer>();
        var plain = composer.Render(1, Plain).Value;

        Assert.NotEqual(plain, composer.Render(2, Plain).Value);
        Assert.NotEqual(plain, composer.Render(3, Plain).Value);
        Assert.NotEqual(composer.Render(2, Plain).Value, composer.Render(3, Plain).Value);
    }

    [Fact]
    public void Render_StopsAfterOneNestedTemplateLevel()
    {
        // Item 2's note content is item 1, whose own note content (item 0) would be a second nested level.
        using var cache = Cache(Item(), Item(note: (0, 0)), Item(note: (1, 0))).Font();
        var composer = cache.Get<ItemIconComposer>();

        Assert.True(composer.Render(1, Plain).IsSuccess);
        Assert.True(composer.Render(2, Plain).HasError<ModelSubjectAbsentError>());
    }

    [Fact]
    public void Render_DrawsTheQuantityLabelUnlessTheModeHidesIt()
    {
        using var cache = Cache(Item(stackable: true), Item()).Font();
        var composer = cache.Get<ItemIconComposer>();

        var unlabelled = composer.Render(1, Plain).Value;
        var labelled = composer.Render(0, Plain with { Quantity = 1000 }).Value;
        var hidden = composer.Render(0, Plain with { Quantity = 1000, QuantityMode = 0 }).Value;
        var forced = composer.Render(1, Plain with { QuantityMode = 1 }).Value;

        Assert.NotEqual(unlabelled, labelled);
        Assert.Equal(unlabelled, hidden);
        Assert.NotEqual(unlabelled, forced);
    }
}
