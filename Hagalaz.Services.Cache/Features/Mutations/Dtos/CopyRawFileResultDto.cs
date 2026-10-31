namespace Hagalaz.Services.Cache.Features.Mutations.Dtos;

public sealed record CopyRawFileResultDto(string Action, int IndexId, int FileId, int SourceIndexId, int SourceFileId);
