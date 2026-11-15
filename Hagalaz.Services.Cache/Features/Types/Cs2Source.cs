using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Hagalaz.Services.Cache.Features.Types.Dtos;

namespace Hagalaz.Services.Cache.Features.Types;

internal static class Cs2Source
{
    internal sealed record CallArguments(int Ints, int Strings, int Longs);
    // Revision 742, MapRegionType dispatch. Raw relative branches still delimit blocks.
    private static readonly Dictionary<int, string> Comparisons = new()
    {
        [703] = "==", [362] = "!=", [588] = "<", [443] = "<=", [573] = ">", [1019] = ">="
    };
    private static readonly HashSet<int> RelativeBranches = [323, .. Comparisons.Keys, 30, 118, 138, 141, 558, 692, 731, 818];
    private static readonly Dictionary<int, string> Constants = new() { [606] = "int", [145] = "string", [63] = "long" };
    private static readonly Dictionary<int, string> LocalReads = new() { [611] = "int", [320] = "string", [9] = "long" };
    private static readonly Dictionary<int, string> LocalWrites = new() { [497] = "int", [568] = "string", [99] = "long" };
    private static readonly Dictionary<int, string> IntOperators = new() { [998] = "+", [212] = "-", [870] = "*", [487] = "/" };
    private static readonly Dictionary<int, string> IntReads = new() { [176] = "cs2Int", [541] = "varbit", [1044] = "varp" };
    private static readonly Dictionary<int, string> IntWrites = new() { [874] = "cs2Int", [326] = "varbit", [122] = "varp" };
    private static readonly Dictionary<int, string> Discards = new() { [16] = "string", [1037] = "int" };
    private static readonly Dictionary<int, string> WidgetIntGetters = new()
    {
        [213] = "getX", [452] = "getWidth", [470] = "getHeight", [1028] = "getDynamicChildCount", [775] = "getY", [585] = "getScrollY"
    };
    private static readonly Dictionary<int, string> PreferenceWrites = new()
    {
        [50] = "sound_7894", [780] = "musicEffect", [529] = "sound_7887",
        [344] = "sound_7907", [734] = "setting_7886", [227] = "loginMusicVolume"
    };
    private static readonly Dictionary<int, (string Name, int Ints, int Strings, bool Explicit)> WidgetSetters = new()
    {
        [18] = ("setSprite", 1, 0, true), [445] = ("setText", 0, 1, true), [302] = ("setTransparency", 1, 0, true),
        [654] = ("setColor", 1, 0, true), [843] = ("setPosition", 4, 0, true), [902] = ("setHidden", 1, 0, true),
        [617] = ("setSize", 4, 0, true), [520] = ("setModel", 1, 0, true),
        [494] = ("setSprite", 1, 0, false), [925] = ("setPosition", 4, 0, false), [885] = ("setSize", 4, 0, false),
        [704] = ("setColor", 1, 0, false), [493] = ("setTransparency", 1, 0, false),
        [812] = ("setHidden", 1, 0, false), [370] = ("setText", 0, 1, false),
        [788] = ("setModel", 1, 0, false), [629] = ("setAction", 0, 1, false), [674] = ("setAction", 0, 1, true),
        [551] = ("setSpriteTiled", 1, 0, false), [841] = ("setScroll", 2, 0, true), [641] = ("setOption", 1, 1, true)
    };
    private static readonly JsonSerializerOptions StringFormat = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private sealed record Value(int Index, string Text, int? Constant = null, string? Local = null);

    public static Cs2SourceLineDto[] Build(
        Cs2InstructionDto[] instructions, Cs2SwitchCaseDto[][] switches, Func<int, CallArguments?>? resolveCall = null)
    {
        var labels = instructions.SelectMany(instruction => Targets(instruction, switches)).ToHashSet();
        var lines = new Dictionary<int, Cs2SourceLineDto>();
        var stacks = new Dictionary<string, List<Value>> { ["int"] = [], ["string"] = [], ["long"] = [] };
        foreach (var instruction in instructions)
        {
            var (index, opcode, kind, operand) = instruction;
            if (labels.Contains(index)) Flush();
            if (Constants.TryGetValue(opcode, out var lane))
                stacks[lane].Add(new Value(index, Literal(instruction), lane == "int" ? ReadInt(operand) : null));
            else if (LocalReads.TryGetValue(opcode, out var read))
                stacks[read].Add(new Value(index, $"value_{index}", Local: $"{read}_{operand}"));
            else if (LocalWrites.TryGetValue(opcode, out var write) && stacks[write].Count > 0)
                Add(index, $"{write}_{operand} = {Take(write, 1)[0].Text}");
            else if (IntReads.TryGetValue(opcode, out var variable))
                Produce(index, "int", $"{variable}[{operand}]");
            else if (opcode == 679)
                Produce(index, "string", $"(cs2String[{operand}] ?? \"\")");
            else if (opcode == 671 && stacks["string"].Count > 0)
                Add(index, $"cs2String[{operand}] = {Take("string", 1)[0].Text}");
            else if (IntWrites.TryGetValue(opcode, out var assignment) && stacks["int"].Count > 0)
                Add(index, $"{assignment}[{operand}] = {Take("int", 1)[0].Text}");
            else if (IntOperators.TryGetValue(opcode, out var op) && stacks["int"].Count >= 2)
            {
                var values = Take("int", 2);
                Produce(index, "int", $"{values[0].Text} {op} {values[1].Text}");
            }
            else if (opcode is 384 or 477 && stacks["int"].Count >= 2)
            {
                var values = Take("int", 2);
                Produce(index, "int", $"{(opcode == 384 ? "max" : "min")}({values[0].Text}, {values[1].Text})");
            }
            else if (opcode == 309 && stacks["int"].Count >= 3)
            {
                var values = Take("int", 3);
                Produce(index, "int", $"(int)((long){values[2].Text} * {values[0].Text} / {values[1].Text})");
            }
            else if (opcode == 147)
                Produce(index, "int", $"getY(widgetContext({(operand == "1" ? 1 : 0)}))");
            else if (WidgetIntGetters.TryGetValue(opcode, out var getter) && stacks["int"].Count > 0)
            {
                var component = Take("int", 1)[0];
                var widget = WidgetReference(component);
                Produce(index, "int", $"{getter}({WidgetTarget(widget, component.Text)})", widget);
            }
            else if (opcode == 512 && stacks["int"].Count >= 3)
            {
                var values = Take("int", 3);
                var widget = WidgetReference(values[0]);
                Add(index, $"widgetContext({(operand == "1" ? 1 : 0)}) = createDynamicChild({WidgetTarget(widget, values[0].Text)}, {values[1].Text}, {values[2].Text})", widget: widget);
            }
            else if (opcode == 528 && stacks["int"].Count > 0)
            {
                var component = Take("int", 1)[0];
                var widget = WidgetReference(component);
                Add(index, $"clearDynamicChildren({WidgetTarget(widget, component.Text)})", widget: widget);
            }
            else if (opcode == 459 && BindIntChanged(index))
            {
                continue;
            }
            else if (PreferenceWrites.TryGetValue(opcode, out var preference) && stacks["int"].Count > 0)
                Add(index, $"setClientPreference({preference}, {Take("int", 1)[0].Text})");
            else if (opcode == 819 && stacks["int"].Count > 0)
                Produce(index, "string", $"intToString({Take("int", 1)[0].Text})");
            else if (opcode == 169 && stacks["int"].Count > 0)
                Produce(index, "int", $"boostedSkillLevel({Take("int", 1)[0].Text})");
            else if (opcode == 32 && stacks["int"].Count > 0)
                Produce(index, "int", $"array_{operand}[{Take("int", 1)[0].Text}]");
            else if (opcode == 260 && stacks["int"].Count >= 2)
            {
                var values = Take("int", 2);
                Add(index, $"array_{operand}[{values[0].Text}] = {values[1].Text}");
            }
            else if (opcode == 960 && stacks["int"].Count > 0)
            {
                var packed = ReadInt(operand);
                var fill = (packed & 65535) == 105 ? 0 : -1;
                Add(index, $"array_{packed >> 16} = newIntArray({Take("int", 1)[0].Text}, {fill})");
            }
            else if (opcode == 471 && ReadInt(operand) is var count && count >= 0 && stacks["string"].Count >= count)
                Produce(index, "string", $"joinStrings({string.Join(", ", Take("string", count).Select(value => value.Text))})");
            else if (Discards.TryGetValue(opcode, out var discard) && stacks[discard].Count > 0)
                Add(index, $"discard({Take(discard, 1)[0].Text})");
            else if (WidgetSetters.TryGetValue(opcode, out var setter) &&
                     stacks["int"].Count >= setter.Ints + (setter.Explicit ? 1 : 0) && stacks["string"].Count >= setter.Strings)
                SetWidget(index, operand, setter);
            else if (opcode == 3)
                RenderCall(index, ReadInt(operand));
            else if (opcode == 2)
            {
                Flush();
                Add(index, "return");
            }
            else if (opcode == 323)
            {
                Flush();
                Add(index, $"goto L{index + 1 + ReadInt(operand)}");
            }
            else if (Comparisons.TryGetValue(opcode, out var comparison) && stacks["int"].Count >= 2)
            {
                var values = Take("int", 2);
                Flush();
                Add(index, $"if ({values[0].Text} {comparison} {values[1].Text}) goto L{index + 1 + ReadInt(operand)}");
            }
            else if (opcode == 623 && stacks["int"].Count > 0 && SwitchTable(switches, operand) is { } cases)
            {
                var key = Take("int", 1)[0];
                Flush();
                var body = cases.Select(entry => $"  case {entry.Key}: goto L{index + 1 + entry.Offset}");
                Add(index, string.Join('\n', [$"switch ({key.Text}) {{", .. body, "}"]));
            }
            else
            {
                Flush();
                Add(index, $"opcode {opcode} {kind} {Literal(instruction)}", raw: true);
            }
        }
        Flush();

        var result = new List<Cs2SourceLineDto>();
        foreach (var instruction in instructions)
        {
            var index = instruction.Index;
            if (labels.Contains(index)) result.Add(new Cs2SourceLineDto(index, $"L{index}:", Label: true));
            if (lines.TryGetValue(index, out var line)) result.Add(line);
        }
        return [.. result];

        bool BindIntChanged(int index)
        {
            var ints = stacks["int"];
            var strings = stacks["string"];
            if (ints.Count < 2 || strings.Count == 0 || !strings[^1].Text.StartsWith('"')) return false;
            var signature = JsonSerializer.Deserialize<string>(strings[^1].Text)!;
            var hasTriggers = signature.EndsWith('Y');
            var triggerCount = hasTriggers ? ints[^2].Constant : 0;
            if (triggerCount is null or < 0) return false;
            var triggerSlots = hasTriggers ? 1 + (long)triggerCount.Value : 0;
            if (triggerCount > 0) signature = signature[..^1];
            var stringCount = signature.Count(c => c == 's');
            var longCount = signature.Count(c => c == '§');
            var intCount = signature.Length - stringCount - longCount;
            if (ints.Count < 2 + triggerSlots + intCount || strings.Count < 1 + stringCount || stacks["long"].Count < longCount)
                return false;
            var component = Take("int", 1)[0];
            var widget = WidgetReference(component);
            Take("string", 1);
            if (hasTriggers) Take("int", 1);
            var triggers = Take("int", triggerCount.Value);
            var args = new string[signature.Length];
            for (var i = signature.Length - 1; i >= 0; i--)
                args[i] = Take(signature[i] == 's' ? "string" : signature[i] == '§' ? "long" : "int", 1)[0].Text;
            var script = Take("int", 1)[0];
            Add(index, $"bindIntChanged({WidgetTarget(widget, component.Text)}, {script.Text}, [{string.Join(", ", args)}], [{string.Join(", ", triggers.Select(trigger => trigger.Text))}])", widget: widget);
            return true;
        }

        void Add(int index, string text, bool raw = false, Cs2WidgetReferenceDto? widget = null) =>
            lines[index] = new Cs2SourceLineDto(index, text, Raw: raw, Widget: widget);

        // Only track the known suffix within a block. Preserve values crossing edges as explicit pushes.
        void Flush()
        {
            foreach (var (lane, stack) in stacks)
            {
                foreach (var value in stack)
                {
                    var push = $"push_{lane}({value.Local ?? value.Text})";
                    if (lines.TryGetValue(value.Index, out var line))
                        lines[value.Index] = line with { Text = $"{line.Text}\n{push}" };
                    else
                        Add(value.Index, push);
                }
                stack.Clear();
            }
        }

        List<Value> Take(string lane, int count)
        {
            var stack = stacks[lane];
            var start = stack.Count - count;
            var values = stack.GetRange(start, count);
            stack.RemoveRange(start, count);
            foreach (var value in values)
            {
                if (value.Local is { } local) Add(value.Index, $"{lane} {value.Text} = {local}");
            }
            return values;
        }

        void RenderCall(int index, int id)
        {
            var arguments = resolveCall?.Invoke(id);
            if (arguments is null || stacks["int"].Count < arguments.Ints ||
                stacks["string"].Count < arguments.Strings || stacks["long"].Count < arguments.Longs)
            {
                Flush();
                Add(index, $"call script_{id}(?)  // opcode 3 int {id}", raw: true);
                return;
            }

            var values = Take("int", arguments.Ints).Concat(Take("string", arguments.Strings)).Concat(Take("long", arguments.Longs));
            // The header describes inputs. A callee's return values remain on the shared stacks.
            Flush();
            Add(index, $"call script_{id}({string.Join(", ", values.Select(value => value.Text))})");
        }

        void Produce(int index, string lane, string expression, Cs2WidgetReferenceDto? widget = null)
        {
            var name = $"value_{index}";
            Add(index, $"{lane} {name} = {expression}", widget: widget);
            stacks[lane].Add(new Value(index, name));
        }

        void SetWidget(int index, string operand, (string Name, int Ints, int Strings, bool Explicit) setter)
        {
            // Explicit widget handlers pop the packed component before the setter arguments.
            var component = setter.Explicit ? Take("int", 1)[0] : null;
            var widget = WidgetReference(component);
            // Node_Sub12 selects the second context only when the operand equals 1.
            var target = widget is null ? component?.Text ?? $"widgetContext({(operand == "1" ? 1 : 0)})" : $"widget({widget.Id}, {widget.Component})";
            var ints = Take("int", setter.Ints);
            var strings = Take("string", setter.Strings);
            // Class109.method1352 treats exactly 1 as hidden.
            var arguments = setter.Name is "setHidden" or "setSpriteTiled"
                ? [ints[0].Constant is int value ? (value == 1 ? "true" : "false") : $"{ints[0].Text} == 1"]
                : ints.Concat(strings).Select(value => value.Text);
            Add(index, $"{setter.Name}({target}, {string.Join(", ", arguments)})", widget: widget);
        }
    }

    private static Cs2WidgetReferenceDto? WidgetReference(Value? component) =>
        component?.Constant is int packed ? new(packed >>> 16, packed & 65535) : null;

    private static string WidgetTarget(Cs2WidgetReferenceDto? widget, string fallback) =>
        widget is null ? fallback : $"widget({widget.Id}, {widget.Component})";

    private static int ReadInt(string operand) => int.Parse(operand, CultureInfo.InvariantCulture);

    private static string Literal(Cs2InstructionDto instruction) =>
        instruction.OperandKind == "string" ? JsonSerializer.Serialize(instruction.Operand, StringFormat) : instruction.Operand;

    private static Cs2SwitchCaseDto[]? SwitchTable(Cs2SwitchCaseDto[][] switches, string operand)
    {
        var index = ReadInt(operand);
        return index >= 0 && index < switches.Length ? switches[index] : null;
    }

    private static IEnumerable<int> Targets(Cs2InstructionDto instruction, Cs2SwitchCaseDto[][] switches)
    {
        var next = instruction.Index + 1;
        if (RelativeBranches.Contains(instruction.Opcode)) return [next + ReadInt(instruction.Operand)];
        if (instruction.Opcode == 623 && SwitchTable(switches, instruction.Operand) is { } cases)
            return cases.Select(entry => next + entry.Offset);
        return [];
    }
}
