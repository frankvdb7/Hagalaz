namespace Hagalaz.Services.Cache.Features.Mutations.Dtos;

public sealed record WriteRawFileResultDto(string Action, int IndexId, int FileId, int? SourceIndexId = null, int? SourceFileId = null);
