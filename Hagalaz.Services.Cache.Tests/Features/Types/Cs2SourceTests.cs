using Hagalaz.Services.Cache.Features.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features.Types;

[TestClass]
public sealed class Cs2SourceTests
{
    [TestMethod]
    public void Build_ReconstructsExplicitWidgetMenuOption()
    {
        var instructions = Instructions((145, "Inspect"), (606, "2"), (606, "64028703"), (641, "0"), (2, "0"));
        instructions[0] = new Cs2InstructionDto(0, 145, "string", "Inspect");
        var lines = Cs2Source.Build(instructions, []);
        Assert.AreEqual(new Cs2SourceLineDto(3, "setOption(widget(977, 31), 2, \"Inspect\")", Widget: new(977, 31)),
            lines.Single(line => line.Index == 3));
    }

    [TestMethod]
    public void Build_ReconstructsEveryInstructionInPinnedScript2005()
    {
        // Revision-742 index 12, group 2005, member 0.
        const string bytes =
            "0002630000000202630000000100D50003FB0000000101430000000902630000000202630000000100D50002630000000101" +
            "C40003E60001BB0000000101430000000100020002630000000001C40002630000000101C40000D40001F100000004026300" +
            "00000101C400025E0000000201E70001F100000005026300000002025EFFFFFFFF02BF000000010143000000030263000000" +
            "0001C40001F10000000202630000000202630000000500D400025E0000000001800001F10000000202630000000202630000" +
            "000401DD0001F100000002026300000003026F00000000014300000042026300000002026300000004025E0000007F013500" +
            "0032000263000000000263000000010003000004C1014300000039026300000002026300000004025E000000FF013500030C" +
            "000263000000000263000000010003000004C0014300000030026300000002026300000004025E0000007F01350002110002" +
            "63000000000263000000010003000004C2014300000027026300000002026300000004025E0000007F013500015800026300" +
            "0000000263000000010003000016EC01430000001E026300000002026300000004025E00000004013500025E0000000301DD" +
            "00025E0000000103E60002DE000263000000000263000000010003000004A101430000001102630000000202630000000402" +
            "5E000000FF01350000E300026300000000026300000001025E00000001025E000000010003000007D7014300000006026300" +
            "0000020263000000040003000019660263000000000263000000010003000019670002000000007000060000000000040000" +
            "00000100070000000000000001000000010000000A0000000200000013000000050000001C00000003000000250000000400" +
            "000032000000060000003D003B";
        using var stream = new MemoryStream(Convert.FromHexString(bytes));
        var script = new Hagalaz.Cache.Logic.Codecs.Cs2DefinitionCodec().Decode(2005, stream);
        var instructions = script.Opcodes.Select((opcode, index) =>
        {
            var kind = Hagalaz.Cache.Types.Cs2Operands.GetKind(opcode);
            return new Cs2InstructionDto(index, opcode, kind.ToString().ToLowerInvariant(),
                script.IntPool[index].ToString(System.Globalization.CultureInfo.InvariantCulture));
        }).ToArray();
        var switches = script.Switches.Select(table => table.Select(entry => new Cs2SwitchCaseDto(entry.Key, entry.Value)).ToArray()).ToArray();
        var lines = Cs2Source.Build(instructions, switches, id => id == 2007 ? new(4, 0, 0) : new(2, 0, 0));
        Assert.HasCount(112, instructions);
        Assert.IsFalse(lines.Any(line => line.Raw));
        Assert.AreEqual("int value_2 = getX(value_1)", lines.Single(line => line.Index == 2).Text);
        Assert.AreEqual("int value_48 = (int)((long)127 * value_45 / value_46)", lines.Single(line => line.Index == 48).Text);
        Assert.AreEqual("setClientPreference(musicEffect, value_57)", lines.Single(line => line.Index == 58).Text);
    }

    [TestMethod]
    public void Build_ReconstructsPinnedScript279AndLinksExactComponent()
    {
        var lines = Cs2Source.Build(Instructions((606, "1"), (606, "64028703"), (902, "0"), (2, "0")), []);
        CollectionAssert.AreEqual(new[]
        {
            new Cs2SourceLineDto(2, "setHidden(widget(977, 31), true)", Widget: new(977, 31)),
            new Cs2SourceLineDto(3, "return")
        }, lines);
    }

    [TestMethod]
    public void Build_ResolvesKnownCallAndRelativeBranchTargets()
    {
        var instructions = Instructions((3, "481"), (606, "4"), (623, "0"), (606, "10"), (606, "20"), (703, "-6"), (323, "0"), (2, "0"));
        var lines = Cs2Source.Build(instructions, [[new(4, 4)]], id => id == 481 ? new(0, 0, 0) : null);
        CollectionAssert.AreEqual(
            new[] { "L0:", "call script_481()", "switch (4) {\n  case 4: goto L7\n}", "if (10 == 20) goto L0", "goto L7", "L7:", "return" },
            lines.Select(line => line.Text).ToArray());
    }

    [TestMethod]
    public void Build_LeavesDynamicWidgetUnlinkedAndShowsUnresolvedInstructions()
    {
        var instructions = Instructions((606, "1"), (611, "0"), (902, "0"), (606, "64028703"), (1045, "1"), (902, "0"), (2, "0"));
        var lines = Cs2Source.Build(instructions, []);

        Assert.IsTrue(lines.All(line => line.Widget is null));
        Assert.AreEqual("setHidden(value_1, true)", lines.Single(line => line.Index == 2).Text);
        CollectionAssert.AreEqual(new[] { 4, 5 }, lines.Where(line => line.Raw).Select(line => line.Index).ToArray());
        Assert.AreEqual("opcode 1045 byte 1", lines.Single(line => line.Index == 4).Text);
    }

    [TestMethod]
    public void Build_ReconstructsVariableArithmeticAndTextForWidget()
    {
        Cs2InstructionDto[] instructions =
        [
            new(0, 145, "string", "Count: "), new(1, 541, "int", "75"), new(2, 606, "int", "2"),
            new(3, 212, "byte", "0"), new(4, 819, "byte", "0"), new(5, 471, "int", "2"),
            new(6, 606, "int", "64028703"), new(7, 445, "byte", "0"), new(8, 2, "byte", "0")
        ];
        var lines = Cs2Source.Build(instructions, []);
        CollectionAssert.AreEqual(new[]
        {
            "int value_1 = varbit[75]", "int value_3 = value_1 - 2", "string value_4 = intToString(value_3)",
            "string value_5 = joinStrings(\"Count: \", value_4)", "setText(widget(977, 31), value_5)", "return"
        }, lines.Select(line => line.Text).ToArray());
        Assert.AreEqual(new Cs2WidgetReferenceDto(977, 31), lines[4].Widget);
        Assert.IsFalse(lines.Any(line => line.Raw));
    }

    [TestMethod]
    public void Build_PreservesReadOrderAndRetainsComputedValueAcrossUnknownInstruction()
    {
        var instructions = Instructions((176, "9"), (606, "4"), (874, "9"), (1045, "0"), (2, "0"));
        var lines = Cs2Source.Build(instructions, []);
        CollectionAssert.AreEqual(new[]
        {
            "int value_0 = cs2Int[9]\npush_int(value_0)", "cs2Int[9] = 4", "opcode 1045 byte 0", "return"
        }, lines.Select(line => line.Text).ToArray());
    }

    [TestMethod]
    public void Build_ReconstructsArrayWritesAndReadsWithTheCorrectOperands()
    {
        var instructions = Instructions((606, "4"), (960, "131177"), (606, "1"), (606, "30"),
            (260, "2"), (606, "1"), (32, "2"), (606, "3"), (487, "0"), (497, "0"), (2, "0"));
        var lines = Cs2Source.Build(instructions, []);
        CollectionAssert.AreEqual(new[]
        {
            "array_2 = newIntArray(4, 0)", "array_2[1] = 30", "int value_6 = array_2[1]",
            "int value_8 = value_6 / 3", "int_0 = value_8", "return"
        }, lines.Select(line => line.Text).ToArray());
        Assert.IsFalse(lines.Any(line => line.Raw));
    }

    [TestMethod]
    public void Build_DistinguishesContextWidgetFromExplicitWidgetReference()
    {
        var instructions = Instructions((606, "831"), (494, "1"), (606, "64028703"),
            (606, "64028702"), (520, "0"), (2, "0"));
        var lines = Cs2Source.Build(instructions, []);

        Assert.AreEqual("setSprite(widgetContext(1), 831)", lines[0].Text);
        Assert.IsNull(lines[0].Widget);
        Assert.AreEqual("setModel(widget(977, 30), 64028703)", lines[1].Text);
        Assert.AreEqual(new Cs2WidgetReferenceDto(977, 30), lines[1].Widget);
    }

    [TestMethod]
    public void Build_KeepsStringAndLongValuesOnSeparateStacks()
    {
        Cs2InstructionDto[] instructions =
        [
            new(0, 145, "string", "€"), new(1, 63, "long", "-9223372036854775807"),
            new(2, 568, "int", "0"), new(3, 99, "int", "1"), new(4, 2, "byte", "0")
        ];
        var lines = Cs2Source.Build(instructions, []);
        CollectionAssert.AreEqual(new[] { "string_0 = \"€\"", "long_1 = -9223372036854775807", "return" }, lines.Select(line => line.Text).ToArray());
    }

    private static Cs2InstructionDto[] Instructions(params (int Opcode, string Operand)[] input) =>
        input.Select((entry, index) => new Cs2InstructionDto(index, entry.Opcode,
            entry.Opcode is 2 or 902 or 1045 ? "byte" : "int", entry.Operand)).ToArray();
}
