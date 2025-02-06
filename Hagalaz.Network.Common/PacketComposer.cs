using System;
using System.IO;

namespace Hagalaz.Network.Common
{
    /// <summary>
    /// Defines composition of a packet.
    /// </summary>
    public abstract partial class PacketComposer : IPacketComposer
    {
        #region Fields
        /// <summary>
        /// The bitmasks used to work with updating etc.
        /// </summary>
        private static readonly int[] Bitmasks =
        {
            0, 0x1, 0x3, 0x7,
            0xf, 0x1f, 0x3f, 0x7f,
            0xff, 0x1ff, 0x3ff, 0x7ff,
            0xfff, 0x1fff, 0x3fff, 0x7fff,
            0xffff, 0x1ffff, 0x3ffff, 0x7ffff,
            0xfffff, 0x1fffff, 0x3fffff, 0x7fffff,
            0xffffff, 0x1ffffff, 0x3ffffff, 0x7ffffff,
            0xfffffff, 0x1fffffff, 0x3fffffff, 0x7fffffff,
            -1
        };

        /// <summary>
        /// The default packet size.
        /// </summary>
        private const int DefaultSize = 32;

        /// <summary>
        /// Gets the buffer payload.
        /// </summary>
        /// <value>The payload.</value>
        private byte[] _payload;
        #endregion Fields

        #region Properties
        /// <summary>
        /// The id of the opcode.
        /// </summary>
        /// <value>The opcode.</value>
        public int Opcode { get; private set; }
        /// <summary>
        /// Gets the packet type.
        /// </summary>
        /// <value>The type.</value>
        public SizeType SizeType { get; private set; }
        /// <summary>
        /// Gets the current index into the buffer (by bits).
        /// </summary>
        /// <value>The bit position.</value>
        public int BitPosition { get; private set; }
        /// <summary>
        /// Gets the payload length.
        /// </summary>
        /// <value>The length.</value>
        public int Length => _payload.Length;
        /// <summary>
        /// Gets the payload position into the payload. (current length).
        /// </summary>
        /// <value>The position.</value>
        public int Position { get; set; }
        /// <summary>
        /// Gets whether the packet is a raw packet (no header).
        /// </summary>
        /// <value><c>true</c> if raw; otherwise, <c>false</c>.</value>
        public bool Raw => Opcode == -1;
        /// <summary>
        /// Gets whether the payload is empty.
        /// </summary>
        /// <value><c>true</c> if empty; otherwise, <c>false</c>.</value>
        public bool Empty => Position == 0;
        #endregion Properties

        #region Constructors
        /// <summary>
        /// Constructs a basic packet builder.
        /// </summary>
        protected PacketComposer() : this(DefaultSize) { }

        /// <summary>
        /// Constructs a new packet builder.
        /// </summary>
        /// <param name="capacity">The buffer's initial capacity.</param>
        protected PacketComposer(int capacity) : this(new byte[capacity]) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PacketComposer"/> class.
        /// </summary>
        /// <param name="payload">The payload.</param>
        protected PacketComposer(byte[] payload)
        {
            this._payload = payload;
            Opcode = -1;
            SizeType = SizeType.Fixed;
        }
        #endregion Constructors

        #region Methods
        /// <summary>
        /// Ensures that the buffer is at least <code>minimumBytes</code> bytes.
        /// </summary>
        /// <param name="minimumCapacity">The minimum size required.</param>
        private void EnsureCapacity(int minimumCapacity)
        {
            if (minimumCapacity >= _payload.Length)
            {
                ExpandCapacity(minimumCapacity);
            }
        }

        /// <summary>
        /// Expands the buffer.
        /// </summary>
        /// <param name="minimumCapacity">The minimum amount to expand.</param>
        public void ExpandCapacity(int minimumCapacity)
        {
            int newCapacity = (_payload.Length + 1) * 2;
            if (newCapacity < 0)
            {
                newCapacity = int.MaxValue;
            }
            else if (minimumCapacity > newCapacity)
            {
                newCapacity = minimumCapacity;
            }

            byte[] newPayload = new byte[newCapacity];

            while (Position > _payload.Length)
                Position--;
            Buffer.BlockCopy(_payload, 0, newPayload, 0, Position);
            _payload = newPayload;
        }

        /// <summary>
        /// Initializes the bit access.
        /// </summary>
        public void InitializeBit()
        {
            BitPosition = Position * 8;
        }

        /// <summary>
        /// Finishes the bit access.
        /// </summary>
        public void FinishBit()
        {
            Position = (BitPosition + 7) / 8;
        }

        /// <summary>
        /// Sets the builder's opcode id.
        /// </summary>
        /// <param name="id">The opcode's id.</param>
        public void SetOpcode(int id)
        {
            Opcode = id;
        }

        /// <summary>
        /// Sets the builder's opcode type.
        /// </summary>
        /// <param name="sizeType">The opcode type.</param>
        public void SetType(SizeType sizeType)
        {
            SizeType = sizeType;
        }

        /// <summary>
        /// Serializes the packet to a byte array.
        /// </summary>
        /// <returns>Returns a byte array serving the data composed in this packet.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public byte[] Serialize()
        {
            if (Raw)
            {
                return SerializeBuffer();
            }

            var data = SerializeBuffer(); // Trim the current buffer.
            using (MemoryStream buffer = new MemoryStream(data.Length + 3))
            {
                if (Opcode >= 128)
                    buffer.WriteByte((byte)128);
                buffer.WriteByte((byte)Opcode);
                if (SizeType != SizeType.Fixed)
                {
                    switch (SizeType)
                    {
                        // A 8-bit packet type.
                        // Stack overflow.
                        case SizeType.Byte when data.Length > 255:
                            throw new ArgumentOutOfRangeException("Could not send a packet with " + data.Length + " bytes within 8 bits.");
                        case SizeType.Byte:
                            buffer.WriteByte((byte)data.Length);
                            break;
                        // A 16-bit packet type.
                        // Stack overflow.
                        case SizeType.Short when data.Length > 65535:
                            throw new ArgumentOutOfRangeException("Could not send a packet with " + data.Length + " bytes within 16 bits.");
                        case SizeType.Short:
                            buffer.WriteByte((byte)(data.Length >> 8));
                            buffer.WriteByte((byte)data.Length);
                            break;
                        case SizeType.Fixed: break;
                        default: throw new ArgumentOutOfRangeException();
                    }
                }
                buffer.Write(data, 0, data.Length);
                return buffer.ToArray();
            }
        }

        /// <summary>
        /// Serializes the packet's buffer only.
        /// </summary>
        /// <returns>Returns a byte array holding only buffer data.</returns>
        public byte[] SerializeBuffer()
        {
            var trimmed = new byte[Position];
            Buffer.BlockCopy(_payload, 0, trimmed, 0, Position);
            return trimmed;
        }
        #endregion Methods
    }
}
