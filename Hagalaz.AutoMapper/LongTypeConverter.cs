using AutoMapper;

namespace Hagalaz.AutoMapper
{
    /// <summary>
    /// An AutoMapper type converter that converts an unsigned 64-bit integer (ulong) to a signed 64-bit integer (long).
    /// </summary>
    public class LongTypeConverter : ITypeConverter<ulong, long>
    {
        /// <inheritdoc />
        public long Convert(ulong source, long destination, ResolutionContext context) => System.Convert.ToInt64(source);
    }
}
