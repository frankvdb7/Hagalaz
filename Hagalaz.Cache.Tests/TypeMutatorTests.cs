using System;
using System.Collections.Generic;
using System.IO;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Moq;
using Xunit;

namespace Hagalaz.Cache.Tests;

public sealed class TypeMutatorTests
{
    private const int Npc = 300;

    [Fact]
    public void Read_DecodesTheStoredEntry()
    {
        var cache = new FakeTypeCache(new NpcTypeData());
        cache.Store(Npc, Encode(new NpcTypeCodec(), new NpcType(Npc) { Name = "Guard", CombatLevel = 21 }));

        var read = Mutator(cache).Read(Npc);

        Assert.True(read.IsSuccess);
        Assert.Equal("Guard", read.Value.Name);
    }

    [Fact]
    public void Read_ReportsAMissingEntryAsAbsent()
    {
        var read = Mutator(new FakeTypeCache(new NpcTypeData())).Read(Npc);

        Assert.True(read.HasError<TypeAbsentError>());
    }

    [Fact]
    public void Read_ReportsAnUndecodableEntryAsUnreadable()
    {
        var cache = new FakeTypeCache(new NpcTypeData());
        cache.Store(Npc, []);

        var read = ByteMutator(cache).Read(Npc);

        Assert.True(read.HasError<TypeUnreadableError>());
    }

    [Fact]
    public void Mutate_WritesTheChangedEntry()
    {
        var cache = new FakeTypeCache(new NpcTypeData());
        cache.Store(Npc, Encode(new NpcTypeCodec(), new NpcType(Npc) { Name = "Guard", CombatLevel = 21 }));

        var result = Mutator(cache).Mutate(Npc, npc => npc.CombatLevel = 40);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, cache.Writes);
        var stored = Decode(new NpcTypeCodec(), cache.Stored(Npc));
        Assert.Equal(40, stored.CombatLevel);
        Assert.Equal("Guard", stored.Name);
    }

    [Fact]
    public void Mutate_DoesNotWriteAnUnchangedEditEvenWhenTheStoredBytesAreNotCanonical()
    {
        var cache = new FakeTypeCache(new NpcTypeData());
        // Decodes to the same value as its canonical encoding [5], like a stored item whose opcodes are in another order.
        cache.Store(Npc, [5, 99]);

        var result = ByteMutator(cache).Mutate(Npc, type => type.Count = 5);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, cache.Writes);
    }

    [Fact]
    public void Mutate_PersistsNoValuesDerivedForTheGameFromNoteTemplates()
    {
        var codec = new ItemTypeCodec();
        var cache = new FakeTypeCache(new ItemTypeData());
        cache.Store(20139, Encode(codec, new ItemType(20139) { Name = "Stored name", NoteId = 20140, NoteTemplateId = 799, Value = 50 }));

        var result = new TypeMutator<IItemType>(cache.Api, codec, new ItemTypeData()).Mutate(20139, item => ((ItemType)item).Value = 75);

        Assert.True(result.IsSuccess);
        var stored = (ItemType)codec.Decode(20139, new MemoryStream(cache.Stored(20139)));
        Assert.Equal("Stored name", stored.Name);
        Assert.Equal(75, stored.Value);
        Assert.Equal(799, stored.NoteTemplateId);
    }

    [Fact]
    public void Mutate_ThrowsAndWritesNothingWhenTheEncodingIsNotStable()
    {
        var cache = new FakeTypeCache(new NpcTypeData());
        cache.Store(Npc, [1]);
        var mutator = new TypeMutator<CountedType>(cache.Api, new ByteCodec(drift: 1), new NpcTypeData());

        Assert.Throws<InvalidOperationException>(() => mutator.Mutate(Npc, type => type.Count++));
        Assert.Equal(0, cache.Writes);
    }

    [Fact]
    public void Mutate_WritesNothingForAMissingOrUndecodableEntry()
    {
        var cache = new FakeTypeCache(new NpcTypeData());
        cache.Store(Npc + 1, []);
        var mutator = ByteMutator(cache);

        Assert.True(mutator.Mutate(Npc, type => type.Count = 40).HasError<TypeAbsentError>());
        Assert.True(mutator.Mutate(Npc + 1, type => type.Count = 40).HasError<TypeUnreadableError>());
        Assert.Equal(0, cache.Writes);
    }

    [Fact]
    public void Replace_WritesTheReplacementOverAnUndecodableEntry()
    {
        var cache = new FakeTypeCache(new NpcTypeData());
        cache.Store(Npc, []);

        var result = ByteMutator(cache).Replace(Npc, new CountedType(Npc) { Count = 9 });

        Assert.True(result.IsSuccess);
        Assert.Equal(new byte[] { 9 }, cache.Stored(Npc));
    }

    [Fact]
    public void Replace_ReportsAMissingEntryAsAbsentWithoutWriting()
    {
        var cache = new FakeTypeCache(new NpcTypeData());

        var result = Mutator(cache).Replace(Npc, new NpcType(Npc) { Name = "Replacement" });

        Assert.True(result.HasError<TypeAbsentError>());
        Assert.Equal(0, cache.Writes);
    }

    [Fact]
    public void Replace_DoesNotWriteAReplacementEqualToTheStoredEntry()
    {
        var cache = new FakeTypeCache(new NpcTypeData());
        cache.Store(Npc, Encode(new NpcTypeCodec(), new NpcType(Npc) { Name = "Guard" }));

        var result = Mutator(cache).Replace(Npc, new NpcType(Npc) { Name = "Guard" });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, cache.Writes);
    }

    private static TypeMutator<INpcType> Mutator(FakeTypeCache cache) => new(cache.Api, new NpcTypeCodec(), new NpcTypeData());

    private static TypeMutator<CountedType> ByteMutator(FakeTypeCache cache) => new(cache.Api, new ByteCodec(), new NpcTypeData());

    private static byte[] Encode<T>(ITypeCodec<T> codec, T type) where T : IType
    {
        using var encoded = codec.Encode(type);
        return encoded.ToArray();
    }

    private static INpcType Decode(ITypeCodec<INpcType> codec, byte[] data) => codec.Decode(Npc, new MemoryStream(data));

    /// <summary>A definition type index whose members are stored at the position equal to their member ID.</summary>
    private sealed class FakeTypeCache
    {
        private readonly Hagalaz.Cache.Abstractions.Types.Data.ITypeData _typeData;
        private readonly Dictionary<(int Archive, int Entry), byte[]> _members = new();

        public FakeTypeCache(Hagalaz.Cache.Abstractions.Types.Data.ITypeData typeData)
        {
            _typeData = typeData;
            var api = new Mock<ICacheAPI>(MockBehavior.Strict);
            api.Setup(cache => cache.ReadReferenceTable(typeData.IndexId)).Returns(() =>
            {
                var table = new Mock<IReferenceTable>();
                table.Setup(t => t.GetEntry(It.IsAny<int>(), It.IsAny<int>())).Returns((int archive, int entry) =>
                    _members.ContainsKey((archive, entry)) ? Mock.Of<IReferenceTableChildEntry>(child => child.Index == entry) : null);
                return table.Object;
            });
            api.Setup(cache => cache.Read(typeData.IndexId, It.IsAny<int>(), It.IsAny<int>()))
                .Returns((int _, int archive, int position) => new MemoryStream(_members[(archive, position)]));
            api.Setup(cache => cache.ReplaceArchiveMember(typeData.IndexId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ReadOnlyMemory<byte>>()))
                .Callback((int _, int archive, int entry, ReadOnlyMemory<byte> data) =>
                {
                    Writes++;
                    _members[(archive, entry)] = data.ToArray();
                });
            Api = api.Object;
        }

        public ICacheAPI Api { get; }
        public int Writes { get; private set; }

        public void Store(int typeId, byte[] data) => _members[(_typeData.GetArchiveId(typeId), _typeData.GetArchiveEntryId(typeId))] = data;
        public byte[] Stored(int typeId) => _members[(_typeData.GetArchiveId(typeId), _typeData.GetArchiveEntryId(typeId))];
    }

    private sealed class CountedType(int id) : IType
    {
        public int Id { get; } = id;
        public int Count { get; set; }
    }

    /// <summary>Stores a count as its first byte and ignores the rest; <paramref name="drift"/> makes re-encoding never match.</summary>
    private sealed class ByteCodec(int drift = 0) : ITypeCodec<CountedType>
    {
        public CountedType Decode(int id, MemoryStream stream) =>
            new(id) { Count = (stream.ReadByte() is var value and >= 0 ? value : throw new EndOfStreamException()) + drift };

        public MemoryStream Encode(CountedType instance) => new([(byte)instance.Count]);
    }
}
