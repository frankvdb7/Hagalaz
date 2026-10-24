using System;
using System.IO;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Models;

internal sealed class ArchiveSnapshot
{
    private const long EntryMetadataWeight = 128;

    private readonly byte[] _data;
    private readonly MemberSlice[] _members;

    private ArchiveSnapshot(byte[] data, MemberSlice[] members)
    {
        _data = data;
        _members = members;
        RetentionWeight = checked((long)data.Length + ((long)members.Length * sizeof(int) * 2) + EntryMetadataWeight);
    }

    public long RetentionWeight { get; }

    public static ArchiveSnapshot Create(IArchive archive)
    {
        var entries = archive.Entries ?? throw new InvalidOperationException("Archive is not decoded.");
        var lengths = new int[entries.Length];
        var totalLength = 0;

        for (var memberIndex = 0; memberIndex < entries.Length; memberIndex++)
        {
            var entry = entries[memberIndex] ?? throw new InvalidDataException($"Archive member {memberIndex} is missing.");
            var length = checked((int)entry.Length);
            lengths[memberIndex] = length;
            totalLength = checked(totalLength + length);
        }

        var data = new byte[totalLength];
        var members = new MemberSlice[entries.Length];
        var offset = 0;

        for (var memberIndex = 0; memberIndex < entries.Length; memberIndex++)
        {
            var entry = entries[memberIndex];
            var length = lengths[memberIndex];
            members[memberIndex] = new MemberSlice(offset, length);
            CopyContents(entry, data, offset, length);
            offset = checked(offset + length);
        }

        if (offset != data.Length)
        {
            throw new InvalidDataException("Packed archive member lengths do not match the snapshot data.");
        }

        return new ArchiveSnapshot(data, members);
    }

    public MemoryStream OpenMember(int physicalMemberIndex)
    {
        if ((uint)physicalMemberIndex >= (uint)_members.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(physicalMemberIndex));
        }

        var member = _members[physicalMemberIndex];
        return new MemoryStream(_data, member.Offset, member.Length, writable: false, publiclyVisible: false);
    }

    private static void CopyContents(MemoryStream entry, byte[] destination, int offset, int length)
    {
        if (entry.TryGetBuffer(out var segment) && segment.Array is not null && segment.Count >= length)
        {
            Buffer.BlockCopy(segment.Array, segment.Offset, destination, offset, length);
            return;
        }

        var contents = entry.ToArray();
        if (contents.Length != length)
        {
            throw new InvalidDataException("Archive member length changed while creating a snapshot.");
        }

        Buffer.BlockCopy(contents, 0, destination, offset, length);
    }

    private readonly record struct MemberSlice(int Offset, int Length);
}
