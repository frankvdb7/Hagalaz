namespace Hagalaz.Services.Cache.Features.Types.Dtos;

public sealed record GetCs2ScriptDto : GetCs2DefinitionDto
{
    public Cs2InstructionDto[] Instructions { get; }
    public Cs2SwitchCaseDto[][] SwitchTables { get; }
    public Cs2SourceLineDto[] Source { get; }
    public string[] ExecutableSource { get; init; } = [];

    public GetCs2ScriptDto(GetCs2DefinitionDto summary, Cs2InstructionDto[] instructions, Cs2SwitchCaseDto[][] switchTables, Cs2SourceLineDto[] source)
        : base(summary)
    {
        Instructions = instructions;
        SwitchTables = switchTables;
        Source = source;
    }
}

public sealed record Cs2InstructionDto(int Index, int Opcode, string OperandKind, string Operand);

public sealed record Cs2SwitchCaseDto(int Key, int Offset);

/// <summary>A source row and its instruction index, reconstruction status, and optional widget reference.</summary>
public sealed record Cs2SourceLineDto(int Index, string Text, bool Label = false, bool Raw = false, Cs2WidgetReferenceDto? Widget = null);

/// <summary>A verified widget component referenced by a source row.</summary>
public sealed record Cs2WidgetReferenceDto(int Id, int Component);
