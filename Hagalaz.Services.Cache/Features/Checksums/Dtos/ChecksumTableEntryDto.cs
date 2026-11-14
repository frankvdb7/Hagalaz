namespace Hagalaz.Services.Cache.Features.Checksums.Dtos;

public sealed record ChecksumTableEntryDto(int IndexId, int Crc32, int Version, string DigestBase64);
