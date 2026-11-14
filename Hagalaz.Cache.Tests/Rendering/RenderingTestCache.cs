using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Extensions;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Models;
using Hagalaz.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Cache.Tests.Rendering;

/// <summary>An in-memory cache wired through <c>AddGameCache</c>, with helpers to store containers and archives.</summary>
internal sealed class RenderingTestCache : IDisposable
{
    private readonly Dictionary<int, ReferenceTable> _tables = [];
    private ServiceProvider? _services;

    public MemoryFileStore Store { get; } = new();

    public IServiceProvider Services => _services ??= new ServiceCollection()
        .AddSingleton<Hagalaz.Cache.Abstractions.IFileStore>(Store)
        .AddGameCache()
        .BuildServiceProvider();

    public T Get<T>() where T : notnull => Services.GetRequiredService<T>();

    /// <summary>Stores <paramref name="data"/> as the container of <paramref name="fileId"/>, listing it in the index's reference table.</summary>
    public RenderingTestCache Container(int indexId, int fileId, byte[] data, string? name = null)
    {
        var table = Table(indexId);
        var entry = new ReferenceTableEntry(fileId);
        if (name is not null) entry.Id = ReferenceTable.CalculateId(name);
        entry.InitializeEntries(1);
        entry.AddEntry(0, new ReferenceTableChildEntry(0));
        table.AddEntry(fileId, entry);
        SaveTable(indexId);
        Store.Set(indexId, fileId, Encode(data));
        return this;
    }

    /// <summary>Stores an archive whose members are keyed by member ID.</summary>
    public RenderingTestCache Archive(int indexId, int fileId, params (int Id, byte[] Data)[] members)
    {
        var table = Table(indexId);
        var entry = new ReferenceTableEntry(fileId);
        var ordered = members.OrderBy(member => member.Id).ToArray();
        entry.InitializeEntries(ordered.Length);
        for (var index = 0; index < ordered.Length; index++) entry.AddEntry(ordered[index].Id, new ReferenceTableChildEntry(index));
        table.AddEntry(fileId, entry);
        SaveTable(indexId);
        using var bytes = new MemoryStream();
        foreach (var member in ordered) bytes.Write(member.Data);
        if (ordered.Length > 1)
        {
            var previous = 0;
            foreach (var member in ordered)
            {
                var delta = new byte[4];
                System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(delta, member.Data.Length - previous);
                bytes.Write(delta);
                previous = member.Data.Length;
            }
            bytes.WriteByte(1);
        }
        Store.Set(indexId, fileId, Encode(bytes.ToArray()));
        return this;
    }

    /// <summary>Stores the bundled <c>p11_full</c> font fixture in the font metrics and sprite indexes.</summary>
    public RenderingTestCache Font()
    {
        var fixtures = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fonts");
        Container(13, 0, File.ReadAllBytes(Path.Combine(fixtures, "p11_full.metrics")), "p11_full");
        Container(8, 0, File.ReadAllBytes(Path.Combine(fixtures, "p11_full.sprite")), "p11_full");
        return this;
    }

    public void Dispose()
    {
        _services?.Dispose();
        Store.Dispose();
    }

    private ReferenceTable Table(int indexId)
    {
        if (!_tables.TryGetValue(indexId, out var table))
            _tables[indexId] = table = new ReferenceTable(7, 1, ReferenceTableFlags.Identifiers, 1);
        return table;
    }

    private void SaveTable(int indexId)
    {
        using var encoded = new ReferenceTableCodec().Encode(_tables[indexId]);
        Store.Set(255, indexId, Encode(encoded.ToArray()));
    }

    private static byte[] Encode(byte[] data)
    {
        using var container = new Container(new MemoryStream(data));
        return container.Encode();
    }
}
