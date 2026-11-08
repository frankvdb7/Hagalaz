using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Types;

public static class HuffmanDefinitionsEndpoints
{
    public static IEndpointRouteBuilder MapHuffmanDefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/huffman/coding", GetHuffmanCoding)
            .WithSummary("Get the chat Huffman coding").Produces<GetHuffmanCodingDto>();
        return types;
    }

    private static IResult GetHuffmanCoding(IHuffmanCodeProvider provider)
    {
        var coding = provider.GetCoding();
        return TypedResults.Ok(new GetHuffmanCodingDto(coding.BitSizes, coding.Masks, coding.DecryptKeys));
    }
}
