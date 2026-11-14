namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetHuffmanCodingDto(byte[] BitSizes, int[] Masks, int[] DecryptKeys);
