using System;
using System.IO;
using FluentResults;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Data;

namespace Hagalaz.Cache.Types;

/// <summary>The definition type has no stored cache entry.</summary>
public sealed class TypeAbsentError(int typeId) : Error($"No cache entry exists for type {typeId}.");

/// <summary>The definition type's stored cache entry cannot be decoded.</summary>
public sealed class TypeUnreadableError : Error
{
    public TypeUnreadableError(int typeId, Exception cause) : base($"The stored cache entry for type {typeId} could not be decoded.") => CausedBy(cause);
}

/// <summary>
/// Reads and changes the stored cache entries of one definition type. Entries are decoded as stored, without type event hooks;
/// every write is verified to encode, decode and re-encode to identical bytes, and an edit equal to the stored entry's canonical
/// encoding (<c>Encode(Decode(stored))</c>) is not written.
/// </summary>
/// <remarks>
/// Concurrent edits of the same type are last-writer-wins. An encoding that fails verification is a codec defect and throws
/// <see cref="InvalidOperationException"/> without writing.
/// </remarks>
public sealed class TypeMutator<TType>(ICacheAPI cache, ITypeCodec<TType> codec, ITypeData typeData) where TType : IType
{
    /// <summary>Decodes the stored entry, or fails with <see cref="TypeAbsentError"/> or <see cref="TypeUnreadableError"/>.</summary>
    public Result<TType> Read(int typeId) => Locate(typeId) is { } position ? Decode(typeId, position) : new TypeAbsentError(typeId);

    /// <summary>Applies <paramref name="change"/> to the stored entry and writes the result.</summary>
    public Result Mutate(int typeId, Action<TType> change)
    {
        var read = Read(typeId);
        if (read.IsFailed) return read.ToResult();

        var type = read.Value;
        try
        {
            var canonical = Encode(type);
            change(type);
            Write(typeId, type, canonical);
            return Result.Ok();
        }
        finally
        {
            (type as IDisposable)?.Dispose();
        }
    }

    /// <summary>Writes <paramref name="replacement"/> over an existing entry; the stored entry need not be decodable.</summary>
    public Result Replace(int typeId, TType replacement)
    {
        if (Locate(typeId) is not { } position) return new TypeAbsentError(typeId);

        byte[]? canonical = null;
        var stored = Decode(typeId, position);
        if (stored.IsSuccess)
        {
            try { canonical = Encode(stored.Value); }
            finally { (stored.Value as IDisposable)?.Dispose(); }
        }

        Write(typeId, replacement, canonical);
        return Result.Ok();
    }

    private int? Locate(int typeId) =>
        typeId < 0 ? null : cache.ReadReferenceTable(typeData.IndexId).GetEntry(typeData.GetArchiveId(typeId), typeData.GetArchiveEntryId(typeId))?.Index;

    private Result<TType> Decode(int typeId, int position)
    {
        using var stored = cache.Read(typeData.IndexId, typeData.GetArchiveId(typeId), position);
        try
        {
            return codec.Decode(typeId, stored);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return new TypeUnreadableError(typeId, error);
        }
    }

    private void Write(int typeId, TType type, byte[]? canonical)
    {
        var encoded = Encode(type);
        if (!IsStable(typeId, encoded)) throw new InvalidOperationException($"Type {typeId} does not encode, decode and re-encode to identical bytes.");
        if (canonical is not null && encoded.AsSpan().SequenceEqual(canonical)) return;
        cache.ReplaceArchiveMember(typeData.IndexId, typeData.GetArchiveId(typeId), typeData.GetArchiveEntryId(typeId), encoded);
    }

    private bool IsStable(int typeId, byte[] encoded)
    {
        TType decoded;
        try
        {
            using var stream = new MemoryStream(encoded, writable: false);
            decoded = codec.Decode(typeId, stream);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Type {typeId}'s encoding cannot be decoded.", error);
        }

        try { return Encode(decoded).AsSpan().SequenceEqual(encoded); }
        finally { (decoded as IDisposable)?.Dispose(); }
    }

    private byte[] Encode(TType type)
    {
        using var encoded = codec.Encode(type);
        return encoded.ToArray();
    }
}
