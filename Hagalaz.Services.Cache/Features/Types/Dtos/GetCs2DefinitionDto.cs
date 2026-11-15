namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public record GetCs2DefinitionDto(
        int Id,
        string Name,
        int IntLocalsCount,
        int StringLocalsCount,
        int LongLocalsCount,
        int IntArgsCount,
        int StringArgsCount,
        int LongArgsCount,
        int SwitchTablesCount,
        int OpcodeCount,
        int IntPoolCount,
        int StringPoolCount,
        int LongPoolCount);
