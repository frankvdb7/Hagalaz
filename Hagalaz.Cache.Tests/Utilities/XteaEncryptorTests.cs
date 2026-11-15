using System;
using System.Linq;
using Hagalaz.Cache.Utilities;
using Xunit;

namespace Hagalaz.Cache.Tests.Utilities;

public sealed class XteaEncryptorTests
{
    [Theory]
    [InlineData(5, 5)]
    [InlineData(21, 5)]
    [InlineData(22, 5)]
    [InlineData(64, 0)]
    public void EncryptXtea_IsInvertedByDecryptXteaAndLeavesTheHeaderAndTailPlain(int length, int offset)
    {
        int[] keys = [0x1234567, -1, int.MinValue, 7];
        var plain = Enumerable.Range(0, length).Select(i => (byte)(i * 31 + 5)).ToArray();

        var encrypted = XteaEncryptor.EncryptXtea(keys, (byte[])plain.Clone(), offset, length);

        Assert.Equal(plain[..offset], encrypted[..offset]);
        var blocks = (length - offset) / 8;
        Assert.Equal(plain[(offset + blocks * 8)..], encrypted[(offset + blocks * 8)..]);
        if (blocks > 0) Assert.NotEqual(plain, encrypted);
        Assert.Equal(plain, XteaDecryptor.DecryptXtea(keys, encrypted, offset, length));
    }

    [Fact]
    public void EncryptXtea_RequiresFourKeys() =>
        Assert.Throws<ArgumentException>(() => XteaEncryptor.EncryptXtea([1, 2, 3], new byte[16], 0, 16));
}
