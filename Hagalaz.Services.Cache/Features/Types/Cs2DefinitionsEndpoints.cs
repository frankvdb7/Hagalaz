using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Types;
using Hagalaz.Cache.Types.Data;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using static Hagalaz.Services.Cache.Features.Types.TypeQueryUtilities;

namespace Hagalaz.Services.Cache.Features.Types;

public static class Cs2DefinitionsEndpoints
{
    private static readonly Cs2DefinitionData ScriptData = new();

    public static IEndpointRouteBuilder MapCs2DefinitionsEndpoints(this IEndpointRouteBuilder types)
    {
        types.MapGet("/cs2/{id:int}", GetCs2Type).WithSummary("Get a CS2 script definition, disassembly and source").Produces<GetCs2ScriptDto>();
        types.MapGet("/cs2/range", GetCs2TypeRange).WithSummary("List CS2 script definitions in a range").Produces<GetCs2DefinitionDto[]>();
        return types;
    }

    private static IResult GetCs2Type(int id, ITypeProvider<ICs2Definition> provider, ICacheAPI cache)
    {
        var table = cache.ReadReferenceTable(ScriptData.IndexId);
        if (table.GetEntry(id, ScriptData.GetArchiveEntryId(id)) is null)
            throw new FileNotFoundException($"CS2 script {id} was not found.");

        var definition = provider.Get(id);
        var instructions = definition.Opcodes.Select((opcode, index) => MapInstruction(definition, opcode, index)).ToArray();
        var switches = definition.Switches
            .Select(cases => cases.Select(entry => new Cs2SwitchCaseDto(entry.Key, entry.Value)).ToArray())
            .ToArray();
        var signatures = new Dictionary<int, Cs2Source.CallArguments?> { [id] = ArgumentsOf(definition) };
        var source = Cs2Source.Build(instructions, switches, ResolveCall);
        return TypedResults.Ok(new GetCs2ScriptDto(MapCs2(definition), instructions, switches, source)
        {
            ExecutableSource = Widgets.WidgetPrograms.Render(Widgets.WidgetPrograms.Compile(definition))
        });

        Cs2Source.CallArguments? ResolveCall(int target)
        {
            if (signatures.TryGetValue(target, out var arguments)) return arguments;
            if (table.GetEntry(target, ScriptData.GetArchiveEntryId(target)) is not null)
            {
                try
                {
                    arguments = ArgumentsOf(provider.Get(target));
                }
                catch (System.Exception error) when (error is IOException or InvalidDataException)
                {
                    // An unreadable callee leaves this call unresolved in the caller's source.
                }
            }
            signatures.Add(target, arguments);
            return arguments;
        }
    }

    private static Cs2Source.CallArguments ArgumentsOf(ICs2Definition script) =>
        new(script.IntArgsCount, script.StringArgsCount, script.LongArgsCount);

    private static IResult GetCs2TypeRange(int startId, int endIdExclusive, ITypeProvider<ICs2Definition> provider, ICacheAPI cache)
    {
        var error = ValidateRange(startId, endIdExclusive, provider.ArchiveSize);
        if (error is not null)
            return error;

        var scripts = cache.ReadReferenceTable(ScriptData.IndexId).Entries
            .Where(entry => entry.Key >= startId && entry.Key < endIdExclusive &&
                entry.Value.GetEntry(ScriptData.GetArchiveEntryId(entry.Key)) is not null)
            .OrderBy(entry => entry.Key)
            .Select(entry => MapCs2(provider.Get(entry.Key)))
            .ToArray();
        return TypedResults.Ok(scripts);
    }

    private static Cs2InstructionDto MapInstruction(ICs2Definition script, int opcode, int index) =>
        Cs2Operands.GetKind(opcode) switch
        {
            Cs2OperandKind.String => new(index, opcode, "string", script.StringPool[index]),
            Cs2OperandKind.Long => new(index, opcode, "long", script.LongPool[index].ToString(CultureInfo.InvariantCulture)),
            Cs2OperandKind.Int => new(index, opcode, "int", script.IntPool[index].ToString(CultureInfo.InvariantCulture)),
            _ => new(index, opcode, "byte", script.IntPool[index].ToString(CultureInfo.InvariantCulture))
        };

    private static GetCs2DefinitionDto MapCs2(ICs2Definition definition) =>
        new(
            definition.Id,
            definition.Name,
            definition.IntLocalsCount,
            definition.StringLocalsCount,
            definition.LongLocalsCount,
            definition.IntArgsCount,
            definition.StringArgsCount,
            definition.LongArgsCount,
            definition.Switches.Length,
            definition.Opcodes.Count,
            definition.IntPool.Count,
            definition.StringPool.Count,
            definition.LongPool.Count);
}
