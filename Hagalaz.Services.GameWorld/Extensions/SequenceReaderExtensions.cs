using System.Buffers;

namespace Hagalaz.Services.GameWorld.Extensions
{
    // TODO - check in the future if we can optimize this to use unsafe memory methods (faster)
    public static class SequenceReaderExtensions
    {
        public static bool TryReadS(ref this SequenceReader<byte> reader, out byte value)
        {
            var span = reader.UnreadSpan;
            if (span.Length < 1)
            {
                value = default;
                return false;
            }
            value = (byte)(128 - span[0]);
            return true;
        }

        public static bool TryReadC(ref this SequenceReader<byte> reader, out byte value)
        {
            var span = reader.UnreadSpan;
            if (span.Length < 1)
            {
                value = default;
                return false;
            }
            value = (byte)-span[0];
            return true;
        }

        public static bool TryReadS(ref this SequenceReader<byte> reader, out bool value)
        {
            if (!TryReadS(ref reader, out byte val))
            {
                value = default;
                return false;
            }
            value = val == 1;
            return true;
        }

        public static bool TryReadInt24BigEndian(ref this SequenceReader<byte> reader, out int value)
        {
            var span = reader.UnreadSpan;
            if (span.Length < 3)
            {
                value = default;
                return false;
            }
            value = span[0] | (span[1] << 8) | (span[2] << 16);
            reader.Advance(3);
            return true;
        }

        public static bool TryReadBigEndianA(ref this SequenceReader<byte> reader, out short value)
        {
            var span = reader.UnreadSpan;
            if (span.Length < 2)
            {
                value = default;
                return false;
            }
            value = (short)((span[0] << 8) + (span[1] - 128 & 0xFF));
            reader.Advance(2);
            return true;
        }

        public static bool TryReadLittleEndianA(ref this SequenceReader<byte> reader, out short value)
        {
            var span = reader.UnreadSpan;
            if (span.Length < 2)
            {
                value = default;
                return false;
            }
            value = (short)((span[0] - 128 & 0xFF) + (span[1] << 8));
            reader.Advance(2);
            return true;
        }

        public static bool TryReadMiddleEndian(ref this SequenceReader<byte> reader, out int value)
        {
            var span = reader.UnreadSpan;
            if (span.Length < 4)
            {
                value = default;
                return false;
            }
            value = (span[0] << 8) | span[1] | (span[2] << 24) | (span[3] << 16);
            reader.Advance(4);
            return true;
        }

        public static bool TryReadMixedEndian(ref this SequenceReader<byte> reader, out int value)
        {
            var span = reader.UnreadSpan;
            if (span.Length < 4)
            {
                value = default;
                return false;
            }
            value = (span[0] << 16) | (span[1] << 24) | span[2] | (span[3] << 8);
            reader.Advance(4);
            return true;
        }
        
        public static bool TryReadBigEndianSmart(ref this SequenceReader<byte> reader, out short value)
        {
            var span = reader.UnreadSpan;
            var isByte = span[0] < sbyte.MaxValue;
            if (isByte && span.Length < 1 || !isByte && span.Length < 2)
            {
                value = default;
                return false;
            }
            if (span[0] < sbyte.MaxValue)
            {
                value = (short)(span[0] & 0xFF);
                reader.Advance(1);
            }
            else
            {
                value = (short)((span[0] << 8) + (span[1] & 0xFF));
                reader.Advance(2);
            }
            return true;
        }
    }
}
