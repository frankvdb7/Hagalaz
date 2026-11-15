using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Cache.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Hagalaz.Services.Cache.Features.Widgets;

internal static class WidgetPrograms
{
    private static readonly Dictionary<int, string> Operations = new()
    {
        [2]="return", [3]="call", [606]="pushInt", [145]="pushString", [63]="pushLong",
        [611]="loadInt", [320]="loadString", [9]="loadLong", [497]="storeInt", [568]="storeString", [99]="storeLong",
        [176]="getVar", [874]="setVar", [1037]="popInt", [16]="popString", [471]="join", [819]="toString",
        [998]="add", [212]="subtract", [870]="multiply", [487]="divide", [323]="jump",
        [703]="equal", [362]="notEqual", [588]="less", [443]="lessEqual", [573]="greater", [1019]="greaterEqual", [623]="switch",
        [18]="setSprite", [445]="setText", [654]="setColor", [843]="setPosition", [902]="setHidden",
        [617]="setSize", [520]="setModel", [494]="setContextSprite", [925]="setContextPosition", [885]="setContextSize",
        [528]="clearDynamicChildren", [302]="setTransparency", [816]="bindMouseEnter", [942]="bindMouseExit",
        [452]="getWidth", [470]="getHeight", [512]="createDynamicChild", [213]="getX",
        [384]="max", [477]="min", [309]="scale", [960]="newIntArray", [32]="loadArrayInt", [260]="storeArrayInt",
        [679]="getStringVar", [671]="setStringVar", [1028]="getDynamicChildCount",
        [704]="setContextColor", [493]="setContextTransparency", [812]="setContextHidden", [370]="setContextText",
        [775]="getY", [147]="getContextY", [788]="setContextModel", [629]="setContextAction", [674]="setAction",
        [551]="setContextSpriteTiled", [459]="bindIntChanged", [841]="setScroll", [585]="getScrollY", [641]="setOption",
        [541]="getVarbit", [326]="setVarbit", [1044]="getVarp", [122]="setVarp"
    };

    internal sealed record Instruction(string Operation, string Operand);
    internal sealed record Program(int Id, int[] Arguments, int[] Locals, Instruction[] Instructions,
        IReadOnlyDictionary<int, int>[] Switches);
    internal sealed record Binding(int Component, WidgetCallback[]? Callbacks);
    internal sealed record Varbit(int ConfigId, int Start, int End);
    internal sealed record Bundle(int Version, Binding[] Bindings, Program[] Programs, int[] Missing, Dictionary<int, Varbit> Varbits);

    public static void MapWidgetPrograms(this IEndpointRouteBuilder group) =>
        group.MapGet("/widgets/{widgetId:int}/program", Load)
            .WithSummary("Load executable RuneScript for a widget preview").Produces<Bundle>();

    private static IResult Load(int widgetId, ICacheAPI cache, ITypeProvider<ICs2Definition> provider,
        IVarpBitDefinitionProvider varpBits, CancellationToken cancellation)
    {
        var widget = cache.ReadReferenceTable(3).GetEntry(widgetId) ?? throw new FileNotFoundException($"Widget {widgetId} was not found.");
        using var archive = cache.ReadArchive(3, widgetId);
        var bindings = widget.Entries.OrderBy(entry => entry.Key)
            .Select(entry => new Binding(entry.Key, WidgetCallbackDecoder.Decode(archive.GetEntry(entry.Value.Index).ToArray()))).ToArray();
        var pending = new Queue<int>(bindings.SelectMany(binding => binding.Callbacks ?? [])
            .Select(callback => callback.ScriptId).OfType<int>());
        var table = cache.ReadReferenceTable(12);
        var visited = new HashSet<int>();
        var missing = new List<int>();
        var programs = new List<Program>();
        var varbits = new Dictionary<int, Varbit>();
        var visitedVarbits = new HashSet<int>();
        while (pending.TryDequeue(out var id))
        {
            cancellation.ThrowIfCancellationRequested();
            if (!visited.Add(id)) continue;
            try
            {
                if (table.GetEntry(id, 0) is null) throw new FileNotFoundException();
                var script = provider.Get(id);
                programs.Add(Compile(script));
                for (var i = 0; i < script.Opcodes.Count; i++)
                {
                    if (script.Opcodes[i] == 3) pending.Enqueue(script.IntPool[i]);
                    if (script.Opcodes[i] is 541 or 326 && visitedVarbits.Add(script.IntPool[i]))
                    {
                        var bitId = script.IntPool[i];
                        try
                        {
                            var bit = varpBits.Get(bitId);
                            varbits.Add(bitId, new Varbit(bit.ConfigID, bit.BitOffset, bit.BitLength));
                        }
                        catch (Exception error) when (error is IOException or InvalidDataException)
                        {
                            // Keep the program available so the runtime only reports missing data if it uses the varbit.
                        }
                    }
                }
                // Callback IDs are stack values. Include cached literal candidates so common
                // listener registrations can execute locally without fetching on hover.
                if (script.Opcodes.Contains(816) || script.Opcodes.Contains(942) || script.Opcodes.Contains(459))
                {
                    for (var i = 0; i < script.Opcodes.Count; i++)
                        if (script.Opcodes[i] == 606 && table.GetEntry(script.IntPool[i], 0) is not null)
                            pending.Enqueue(script.IntPool[i]);
                }
            }
            catch (Exception error) when (error is IOException or InvalidDataException)
            {
                missing.Add(id);
            }
        }
        return TypedResults.Ok(new Bundle(1, bindings, [.. programs], [.. missing], varbits));
    }

    internal static Program Compile(ICs2Definition script) => new(script.Id,
        [script.IntArgsCount, script.StringArgsCount, script.LongArgsCount],
        [script.IntLocalsCount, script.StringLocalsCount, script.LongLocalsCount],
        script.Opcodes.Select((opcode, index) => new Instruction(
            Operations.GetValueOrDefault(opcode) ?? $"unsupported_{opcode}",
            Cs2Operands.GetKind(opcode) switch
            {
                Cs2OperandKind.String => script.StringPool[index],
                Cs2OperandKind.Long => script.LongPool[index].ToString(CultureInfo.InvariantCulture),
                _ => script.IntPool[index].ToString(CultureInfo.InvariantCulture)
            })).ToArray(), script.Switches);

    internal static string[] Render(Program program) => program.Instructions.Select((instruction, index) =>
        $"L{index}: {instruction.Operation} {JsonSerializer.Serialize(instruction.Operand)}").ToArray();
}
