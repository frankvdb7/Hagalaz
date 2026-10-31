using System.Collections.Generic;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetClientMapDefinitionDto(
        int Id,
        string DefaultStringValue,
        int DefaultIntValue,
        char KeyType,
        char ValueType,
        int Count,
        IReadOnlyList<ClientMapValueMapEntryDto>? ValueMap,
        IReadOnlyList<DynamicValueDto>? Values);
