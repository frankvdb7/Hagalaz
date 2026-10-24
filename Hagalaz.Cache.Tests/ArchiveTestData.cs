using System;
using System.IO;
using Hagalaz.Cache.Extensions;

namespace Hagalaz.Cache.Tests;

internal static class ArchiveTestData
{
    public static byte[] EncodeArchive(params byte[][] members)
    {
        if (members.Length == 0)
        {
            throw new ArgumentException("An archive must contain at least one member.", nameof(members));
        }

        if (members.Length == 1)
        {
            return members[0];
        }

        using var archiveData = new MemoryStream();
        foreach (var member in members)
        {
            archiveData.Write(member);
        }

        var previousMemberLength = 0;
        foreach (var member in members)
        {
            archiveData.WriteInt(member.Length - previousMemberLength);
            previousMemberLength = member.Length;
        }

        archiveData.WriteByte(1);
        return archiveData.ToArray();
    }
}
