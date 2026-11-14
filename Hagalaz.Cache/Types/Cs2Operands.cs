using System.IO;

namespace Hagalaz.Cache.Types;

public enum Cs2OperandKind
{
    Byte,
    Int,
    Long,
    String
}

public static class Cs2Operands
{
    // Revision 742: Class111_Sub1 indexes CS2InstructionInfo by opcode.
    public static Cs2OperandKind GetKind(int opcode) => opcode switch
    {
        63 => Cs2OperandKind.Long,
        145 => Cs2OperandKind.String,
        3 or 9 or 30 or 32 or 41 or 95 or 99 or 118 or 122 or 138 or 141 or 176 or
        260 or 282 or 285 or 310 or 320 or 323 or 326 or 362 or 443 or 471 or 480 or
        497 or 522 or 527 or 541 or 556 or 558 or 568 or 573 or 588 or 606 or 611 or
        623 or 639 or 671 or 679 or 692 or 703 or 728 or 731 or 794 or 818 or 874 or
        960 or 1000 or 1019 or 1044 => Cs2OperandKind.Int,
        >= 0 and <= 1045 => Cs2OperandKind.Byte,
        _ => throw new InvalidDataException($"Unknown revision-742 CS2 opcode {opcode}.")
    };
}
