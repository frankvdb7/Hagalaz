using AutoMapper;

namespace Hagalaz.AutoMapper
{
    /// <summary>
    /// An AutoMapper type converter that converts various smaller integer types (byte, sbyte, short, ushort, uint) to a 32-bit integer (int).
    /// </summary>
    public class IntTypeConverter : ITypeConverter<byte, int>, ITypeConverter<sbyte, int>, ITypeConverter<short, int>, ITypeConverter<ushort, int>, ITypeConverter<uint, int>
    {
        /// <inheritdoc />
        public int Convert(byte source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);

        /// <inheritdoc />
        public int Convert(short source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);

        /// <inheritdoc />
        public int Convert(ushort source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);

        /// <inheritdoc />
        public int Convert(sbyte source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);

        /// <inheritdoc />
        public int Convert(uint source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);
    }
}