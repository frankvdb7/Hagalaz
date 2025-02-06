using System;
using System.IO;
using System.Text;

namespace Hagalaz.Network.Common
{
    /// <summary>
    /// Represents a packet. Allows multiple ways to read data.
    /// </summary>
    public partial class Packet : IDisposable
    {        
        #region Fields
        /// <summary>
        /// Possible hex values.
        /// </summary>
        private static readonly char[] Hex = "0123456789ABCDEF".ToCharArray();
        /// <summary>
        /// Gets the packet buffer.
        /// </summary>
        public MemoryStream BaseBuffer { get; private set; }
        #endregion Fields

        #region Properties
        /// <summary>
        /// Gets or sets the opcode id.
        /// </summary>
        public int Opcode { get; set; }
        /// <summary>
        /// Gets or sets the packet type.
        /// </summary>
        public SizeType SizeType { get; set; }
        /// <summary>
        /// Gets the packet's payload length.
        /// </summary>
        public long Length => BaseBuffer.Length;
        /// <summary>
        /// Gets the current position into the payload.
        /// </summary>
        public long Position => BaseBuffer.Position;

        /// <summary>
        /// Gets whether the packet is a raw packet (no header).
        /// </summary>
        public bool Raw => Opcode == -1;

        /// <summary>
        /// Gets the remaining amount of data within the buffer.
        /// </summary>
        public long RemainingAmount => BaseBuffer.Length - Position;
        #endregion Properties

        #region Constructors
        /// <summary>
        /// Constructs a new packet.
        /// </summary>
        /// <param name="opcode">The opcode id.</param>
        /// <param name="sizeType">The packet type.</param>
        /// <param name="data">The payload.</param>
        public Packet(int opcode, SizeType sizeType, byte[]? data = null)
        {
            Opcode = opcode;
            SizeType = sizeType;

            BaseBuffer = data == null ? new MemoryStream() : new MemoryStream(data);
        }

        /// <summary>
        /// Constructs a new packet.
        /// </summary>
        /// <param name="opcode">The packet id.</param>
        /// <param name="data">The payload.</param>
        public Packet(int opcode, byte[] data) : this(opcode, SizeType.Fixed, data) { }

        /// <summary>
        /// Constructs a raw bare packet.
        /// </summary>
        /// <param name="data">The payload.</param>
        public Packet(byte[] data) : this(-1, data) { }
        #endregion Constructors

        #region Methods
        /// <summary>
        /// Gets the remaining data as a seperate array.
        /// </summary>
        /// <param name="movePosition">if set to <c>true</c> [move position].</param>
        /// <returns>
        /// Returns an array of Int8 integers.
        /// </returns>
        public byte[] GetRemainingData(bool movePosition = true)
        {
            var position = Position;
            var remaining = new byte[RemainingAmount];
            BaseBuffer.Read(remaining, 0, (int)RemainingAmount);
            if (!movePosition) BaseBuffer.Position = position;
            return remaining;
        }

        /// <summary>
        /// Prints out the packet opcode, length and the payload itself.
        /// </summary>
        /// <returns>Returns a string.</returns>
        public override string ToString()
        {
            byte[] data = BaseBuffer.ToArray();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < Length; i++)
            {
                if (sb.Length != 0)
                {
                    sb.Append(",");
                }
                sb.Append(ByteToHex(data[i], true));
            }
            sb.Append("]");
            sb.Insert(0, "[opcode=" + Opcode + ",length=" + Length + ",data=");
            return sb.ToString();
        }

        /// <summary>
        /// Turns a byte into a hex.
        /// </summary>
        /// <param name="b">The byte to convert.</param>
        /// <param name="forceLeadingZero">Whether to add a reading zero.</param>
        /// <returns>Returns a string containing the hex value of the byte.</returns>
        private static string ByteToHex(byte b, bool forceLeadingZero)
        {
            StringBuilder sb = new StringBuilder();
            int ub = b & 0xff;
            if (ub / 16 > 0 || forceLeadingZero)
                sb.Append(Hex[ub / 16]);
            sb.Append(Hex[ub % 16]);
            return sb.ToString();
        }

        /// <summary>
        /// Skips a given amount of bytes into the payload.
        /// </summary>
        /// <param name="length">The number of bytes to skip.</param>
        public void Skip(long length) => BaseBuffer.Position += length;

        /// <summary>
        /// Rewinds the payload's index to 0.
        /// </summary>
        public void Rewind() => BaseBuffer.Position = 0;

        #region IDispose Members
        /// <summary>
        /// Attempts to dispose the session.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Whether to dispose managed code.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (BaseBuffer != null)
                {
                    BaseBuffer.Close();
                    BaseBuffer = null!;
                }
            }
        }
        #endregion IDispose Members
        #endregion Methods
    }
}
