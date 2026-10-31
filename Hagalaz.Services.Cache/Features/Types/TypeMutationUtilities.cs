using System;
using System.IO;
using System.Threading.Tasks;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic.Codecs;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Data;
using Hagalaz.Services.Cache.Features.Shared;
using Microsoft.AspNetCore.Http;

namespace Hagalaz.Services.Cache.Features.Types;

internal static class TypeMutationUtilities
{
    internal static async Task<MemoryStream> ReadImageRequestStreamAsync(HttpRequest request)
    {
        var stream = new MemoryStream();

        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync(request.HttpContext.RequestAborted);
            var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
            if (file is not null)
            {
                await file.CopyToAsync(stream, request.HttpContext.RequestAborted);
                stream.Position = 0;
                return stream;
            }
        }

        await request.Body.CopyToAsync(stream, request.HttpContext.RequestAborted);
        stream.Position = 0;
        return stream;
    }

    internal static IResult? TryGetType<TType>(int typeId, ITypeProvider<TType> provider, out TType type)
        where TType : IType
    {
        type = default!;
        if (typeId < 0)
        {
            return TypedResults.BadRequest(EndpointErrors.Validation("id must be greater than or equal to 0."));
        }

        type = provider.Get(typeId);
        return null;
    }

    internal static TType DecodeRawTypeEntry<TType>(int typeId, ITypeCodec<TType> codec, ITypeData typeData, ICacheAPI cacheApi)
        where TType : IType
    {
        var archiveId = typeData.GetArchiveId(typeId);
        var archiveEntryId = typeData.GetArchiveEntryId(typeId);
        var entry = cacheApi.ReadReferenceTable(typeData.IndexId).GetEntry(archiveId, archiveEntryId)
            ?? throw new FileNotFoundException($"No cache entry exists for type {typeId}.");

        using var member = cacheApi.Read(typeData.IndexId, archiveId, entry.Index);
        return codec.Decode(typeId, member);
    }

    internal static void PersistTypeEntry<TType>(
        int typeId,
        TType type,
        ITypeCodec<TType> codec,
        ITypeData typeData,
        ICacheAPI cacheApi)
        where TType : IType
    {
        using var encodedType = codec.Encode(type);
        var encodedBytes = encodedType.ToArray();
        using (var validationStream = new MemoryStream(encodedBytes, writable: false))
        {
            _ = codec.Decode(typeId, validationStream);
        }

        PersistTypeEntry(cacheApi, typeData, typeId, encodedBytes);
    }

    internal static void PersistTypeEntry(ICacheAPI cacheApi, ITypeData typeData, int typeId, ReadOnlyMemory<byte> encodedType)
    {
        var indexId = typeData.IndexId;
        var archiveId = typeData.GetArchiveId(typeId);
        var archiveEntryId = typeData.GetArchiveEntryId(typeId);

        cacheApi.ReplaceArchiveMember(indexId, archiveId, archiveEntryId, encodedType);
    }
}
