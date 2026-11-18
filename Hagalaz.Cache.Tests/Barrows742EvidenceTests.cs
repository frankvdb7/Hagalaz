using System.Text;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hagalaz.Cache.Tests;

/// <summary>
///     Read-only characterization of the pinned revision-742 cache for the Barrows minigame (issue #315).
///     Set <c>HAGALAZ_CACHE742_PATH</c> to a directory holding the extracted
///     <c>tests/fixtures/cache742/cache.zip</c> of Hagalaz.GameClient (SHA-256 2674f30b...c891b1).
///     The tests pass trivially when the variable is not set. Set <c>BARROWS_EVIDENCE_REPORT</c> to also write the raw report.
/// </summary>
public sealed class Barrows742EvidenceTests : IDisposable
{
    private readonly ServiceProvider? _services;
    private readonly StringBuilder _report = new();

    public Barrows742EvidenceTests()
    {
        var path = Environment.GetEnvironmentVariable("HAGALAZ_CACHE742_PATH");
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddGameCache(o => o.Path = path);
        _services = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        var target = Environment.GetEnvironmentVariable("BARROWS_EVIDENCE_REPORT");
        if (_services != null && !string.IsNullOrEmpty(target))
        {
            File.AppendAllText(target, _report.ToString());
        }

        _services?.Dispose();
    }

    [Fact]
    public void Varbits_Config452_453_464()
    {
        if (_services == null) return;
        var provider = _services.GetRequiredService<IVarpBitDefinitionProvider>();
        foreach (var id in Enumerable.Range(440, 50))
        {
            var d = provider.Get(id);
            _report.AppendLine($"VARBIT {id}: config={d.ConfigID} lsb={d.BitOffset} len={d.BitLength}");
        }

        // every varbit that lives in the Barrows configs
        for (var id = 0; id < 12000; id++)
        {
            var d = provider.Get(id);
            if (d.ConfigID is 452 or 453 && id is < 440 or >= 490)
            {
                _report.AppendLine($"VARBIT-IN-CONFIG {id}: config={d.ConfigID} lsb={d.BitOffset} len={d.BitLength}");
            }
        }

        // NOTE: VarpBitDefinition.BitLength holds the definition's most-significant bit index (msb), not a width.
        // Config 453 (HUD): brother flags, reward potential, chest open (also object 10284's varbit), creatures slain.
        for (var i = 0; i < 6; i++) AssertBits(provider, 457 + i, 453, i, i);
        AssertBits(provider, 463, 453, 6, 15);
        AssertBits(provider, 1394, 453, 16, 16);
        AssertBits(provider, 464, 453, 17, 31);
        // Config 452 (tunnel): one bit per entry rope (6..9) and one bit per door slot (10..25).
        for (var i = 0; i < 4; i++) AssertBits(provider, 465 + i, 452, 6 + i, 6 + i);
        for (var i = 0; i < 16; i++) AssertBits(provider, 469 + i, 452, 10 + i, 10 + i);
    }

    private static void AssertBits(IVarpBitDefinitionProvider provider, int id, int config, int lsb, int msb)
    {
        var d = provider.Get(id);
        Assert.Equal((config, lsb, msb), (d.ConfigID, (int)d.BitOffset, (int)d.BitLength));
    }

    [Fact]
    public void Objects()
    {
        if (_services == null) return;
        var provider = _services.GetRequiredService<ITypeProvider<IObjectType>>();
        var ids = new List<int>();
        ids.AddRange(Enumerable.Range(6700, 70));
        ids.AddRange([10284, 61189, 63177, 66016, 66017, 66018, 66019, 66020, 66115, 66116]);
        ids.AddRange(Enumerable.Range(6702, 6));
        foreach (var id in ids.Distinct())
        {
            var o = (ObjectType)provider.Get(id);
            if (id is >= 6709 and <= 6712)
            {
                Assert.Equal(465 + (id - 6709), o.VarpBitFileId);
                Assert.Equal([-1, 6708, -1], o.TransformToIDs);
            }

            if (id is >= 6716 and <= 6731) Assert.Equal(469 + (id - 6716), o.VarpBitFileId);
            if (id is >= 6735 and <= 6750) Assert.Equal(469 + (id - 6735), o.VarpBitFileId);
            if (id == 10284)
            {
                Assert.Equal(1394, o.VarpBitFileId);
                Assert.Equal([6774, 6775, -1], o.TransformToIDs);
            }

            _report.AppendLine(
                $"OBJECT {id}: name='{o.Name}' size={o.SizeX}x{o.SizeY} varbit={o.VarpBitFileId} transforms=[{string.Join(",", o.TransformToIDs ?? [])}] actions=[{string.Join("|", o.Actions.Select(a => a ?? ""))}]");
        }
    }

    [Fact]
    public void MapPlacements_BarrowsTunnelRegion()
    {
        if (_services == null) return;
        var maps = _services.GetRequiredService<IMapProvider>();
        // Barrows tunnel spawn coordinates (3534..3569, 9677..9712) fall in region x=55, y=151.
        foreach (var regionId in new[] { (55 << 8) | 151, (55 << 8) | 150 })
        {
            try
            {
                var map = maps.Get(regionId, [0, 0, 0, 0]);
                var interesting = map.Objects
                    .Where(o => o.Id is (>= 6709 and <= 6712) or (>= 6716 and <= 6731) or (>= 6735 and <= 6750) or 10284 or 6708)
                    .OrderBy(o => o.Id);
                _report.AppendLine($"MAP region {regionId}: objects={map.Objects.Count}");
                if (regionId == ((55 << 8) | 151))
                {
                    var placed = interesting.Select(o => o.Id).Distinct().ToHashSet();
                    // all four entry ropes are placed, including 6712 which CryptRope does not register
                    Assert.All(new[] { 6709, 6710, 6711, 6712 }, id => Assert.Contains(id, placed));
                    // CryptCharacterSpawns / entry ropes: NW, NE, SW, SE
                    Assert.Equal(
                        [(3534, 9712), (3568, 9712), (3534, 9678), (3568, 9678)],
                        interesting.Where(o => o.Id is >= 6709 and <= 6712).Select(o => (3520 + o.X, 9664 + o.Y)).ToArray());
                    Assert.Contains(interesting, o => o.Id == 10284 && 3520 + o.X == 3551 && 9664 + o.Y == 9695);
                    // every door id (inner and perimeter, both variants) has two placements
                    foreach (var id in Enumerable.Range(6716, 16).Concat(Enumerable.Range(6735, 16)))
                    {
                        Assert.Equal(2, interesting.Count(o => o.Id == id));
                    }
                }

                foreach (var o in interesting)
                {
                    _report.AppendLine($"MAP-OBJECT region={regionId} id={o.Id} shape={o.ShapeType} rot={o.Rotation} at=({(regionId >> 8) * 64 + o.X},{(regionId & 0xff) * 64 + o.Y},{o.Z})");
                }
            }
            catch (Exception e)
            {
                _report.AppendLine($"MAP region {regionId}: FAILED {e.GetType().Name} {e.Message}");
            }
        }
    }

    [Fact]
    public void Npcs()
    {
        if (_services == null) return;
        var provider = _services.GetRequiredService<ITypeProvider<INpcType>>();
        foreach (var id in new[] { 2025, 2026, 2027, 2028, 2029, 2030, 14297, 2031, 2032, 2033, 2034, 2035, 2036, 2037 })
        {
            var n = (NpcType)provider.Get(id);
            if (id == 14297) Assert.Equal("Akrisae the Doomed", n.Name);
            _report.AppendLine($"NPC {id}: name='{n.Name}' combat={n.CombatLevel} size={n.Size} actions=[{string.Join("|", n.Actions.Select(a => a ?? ""))}]");
        }
    }

    [Fact]
    public void Quest_RitualOfTheMahjarrat()
    {
        if (_services == null) return;
        var provider = _services.GetRequiredService<ITypeProvider<IQuestType>>();
        var count = provider.ArchiveSize;
        _report.AppendLine($"QUEST-COUNT {count}");
        for (var id = 0; id < count; id++)
        {
            var q = provider.Get(id);
            if (q.Name == "Ritual of the Mahjarrat")
            {
                Assert.Equal(194, id);
                var progress = Assert.Single(q.ProgressVarBits);
                Assert.Equal((9829, 10, 400), (progress.Id, progress.StartedThreshold, progress.CompletedThreshold));
                Assert.Empty(q.ProgressVarps);
            }

            if (q.Name.Contains("Mahjarrat", StringComparison.OrdinalIgnoreCase) || q.Name.Contains("Ritual", StringComparison.OrdinalIgnoreCase))
            {
                _report.AppendLine($"QUEST {id}: '{q.Name}' points={q.QuestPoints}");
                foreach (var t in q.ProgressVarps) _report.AppendLine($"  progress varp: {System.Text.Json.JsonSerializer.Serialize(t)}");
                foreach (var t in q.ProgressVarBits) _report.AppendLine($"  progress varbit: {System.Text.Json.JsonSerializer.Serialize(t)}");
            }
        }
    }

    [Theory]
    [InlineData(24)]
    [InlineData(25)]
    public void Interfaces(int interfaceId)
    {
        if (_services == null) return;
        var cache = _services.GetRequiredService<ICacheAPI>();
        var files = cache.GetFileCount(3, interfaceId);
        _report.AppendLine($"INTERFACE {interfaceId}: components={files}");
        for (var child = 0; child < files; child++)
        {
            byte[] bytes;
            try
            {
                bytes = cache.Read(3, interfaceId, child).ToArray();
            }
            catch (Exception e)
            {
                _report.AppendLine($"  [{child}] unreadable: {e.GetType().Name}");
                continue;
            }

            var line = ComponentReader.Describe(child, bytes);
            Assert.DoesNotContain("PARSE-ERROR", line);
            Assert.DoesNotContain("TRAILING-BYTES", line);
            _report.AppendLine("  " + line);
        }

        Assert.Equal(15, files);
    }

    /// <summary>Port of the 742 client's <c>IComponentDefinition.parseIComponentDefinition</c> (read order only).</summary>
    private sealed class ComponentReader(byte[] data)
    {
        private int _p;
        private int U8() => data[_p++];
        private int S8() => (sbyte)data[_p++];
        private int U16() => (data[_p++] << 8) | data[_p++];
        private int S16() => (short)U16();
        private int Med() => (U8() << 16) | U16();
        private int I32() => (U16() << 16) | U16();

        private int BigSmart()
        {
            if ((sbyte)data[_p] < 0)
            {
                return I32() & 0x7fffffff;
            }

            var v = U16();
            return v == 32767 ? -1 : v;
        }

        private string Str()
        {
            var start = _p;
            while (data[_p++] != 0) { }
            return Encoding.Latin1.GetString(data, start, _p - start - 1);
        }

        private string SpecialStr()
        {
            if (U8() != 0) throw new InvalidDataException("special string marker");
            return Str();
        }

        private string Script()
        {
            var n = U8();
            if (n == 0) return "";
            var parts = new List<string>();
            for (var i = 0; i < n; i++)
            {
                parts.Add(U8() == 0 ? I32().ToString() : "\"" + Str() + "\"");
            }

            return "[" + string.Join(",", parts) + "]";
        }

        private string Triggers()
        {
            var n = U8();
            return n == 0 ? "" : "[" + string.Join(",", Enumerable.Range(0, n).Select(_ => I32())) + "]";
        }

        public static string Describe(int child, byte[] bytes)
        {
            try
            {
                var reader = new ComponentReader(bytes);
                var text = reader.Read();
                return $"[{child}]{text}" + (reader._p == bytes.Length ? "" : $" TRAILING-BYTES={bytes.Length - reader._p}");
            }
            catch (Exception e)
            {
                return $"[{child}] PARSE-ERROR {e.GetType().Name} {e.Message}";
            }
        }

        private string Read()
        {
            var sb = new StringBuilder();
            var version = U8();
            if (version == 255) version = -1;
            var type = U8();
            if ((type & 0x80) != 0)
            {
                type &= 0x7f;
                sb.Append($" name='{Str()}'");
            }

            U16();
            var x = S16();
            var y = S16();
            var w = U16();
            var h = U16();
            var widthLayout = S8();
            var heightLayout = S8();
            S8(); S8();
            var parent = U16();
            var flags = U8();
            sb.Append($" v={version} type={type} pos=({x},{y}) size={w}x{h} parent={(parent == 65535 ? -1 : parent)} hidden={(flags & 1) != 0}");
            switch (type)
            {
                case 0:
                    U16(); U16();
                    if (version < 0) U8();
                    break;
                case 5:
                    sb.Append($" sprite={I32()}");
                    U16(); U8(); U8(); U8(); I32(); U8(); U8(); I32();
                    if (version >= 3) U8();
                    break;
                case 6:
                {
                    var model = BigSmart();
                    var f = U8();
                    if ((f & 1) == 1) { S16(); S16(); U16(); U16(); U16(); U16(); }
                    else if ((f & 2) == 2) { S16(); S16(); S16(); U16(); U16(); U16(); S16(); }
                    var anim = BigSmart();
                    if (widthLayout != 0) U16();
                    if (heightLayout != 0) U16();
                    sb.Append($" model={model} anim={anim}");
                    break;
                }
                case 4:
                {
                    var font = BigSmart();
                    if (version >= 2) U8();
                    var text = Str();
                    U8(); U8(); U8(); U8(); I32(); U8();
                    if (version >= 0) U8();
                    sb.Append($" font={font} text='{text}'");
                    break;
                }
                case 3:
                    I32(); U8(); U8();
                    break;
                case 9:
                    U8(); I32(); U8();
                    break;
            }

            var settings = Med();
            var opt = U8();
            while (opt != 0)
            {
                opt = (opt << 8) | U8();
                opt &= 0xfff;
                S8(); S8();
                opt = U8();
            }

            var actionName = Str();
            var packed = U8();
            var menuCount = packed & 0xf;
            var extra = packed >> 4;
            var menu = Enumerable.Range(0, menuCount).Select(_ => Str()).ToList();
            if (extra > 0) { U8(); U16(); }
            if (extra > 1) { U8(); U16(); }
            var action = Str();
            U8(); U8(); U8();
            var tip = Str();
            if (((settings >> 11) & 0x7f) != 0) { U16(); U16(); U16(); }
            if (version >= 0) U16();
            sb.Append($" settings=0x{settings:x} actionName='{actionName}' action='{action}' tip='{tip}' menu=[{string.Join("|", menu)}]");
            if (version >= 0)
            {
                var ints = U8();
                for (var i = 0; i < ints; i++) { Med(); I32(); }
                var strs = U8();
                for (var i = 0; i < strs; i++) { Med(); SpecialStr(); }
            }

            string[] names =
            [
                "onOpen", "mouseEnter", "mouseExit", "useOn", "use", "configChanged", "itemContainerChanged", "skillChanged", "gameLoop",
                "clickedContextMenu"
            ];
            foreach (var n in names) Append(sb, n, Script());
            if (version >= 0) Append(sb, "itemOnItem", Script());
            string[] names2 =
            [
                "mouseHover", "mousePressed", "mouseHolding", "mouseReleased", "mouseDraggedOver", "mouseDragged", "mouseStopDrag", "mouseScroll",
                "cs2IntChanged", "cs2StringChanged"
            ];
            foreach (var n in names2) Append(sb, n, Script());
            string[] triggers = ["configTriggers", "itemContainerTriggers", "skillTriggers", "cs2IntTriggers", "cs2StringTriggers"];
            foreach (var n in triggers) Append(sb, n, Triggers());
            return sb.ToString();
        }

        private static void Append(StringBuilder sb, string name, string value)
        {
            if (value.Length > 0) sb.Append($" {name}={value}");
        }
    }
}
