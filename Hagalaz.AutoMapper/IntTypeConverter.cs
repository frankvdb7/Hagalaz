using AutoMapper;

namespace Hagalaz.AutoMapper
{
    public class IntTypeConverter : ITypeConverter<byte, int>, ITypeConverter<sbyte, int>, ITypeConverter<short, int>, ITypeConverter<ushort, int>, ITypeConverter<uint, int>
    {
        public int Convert(byte source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);
        public int Convert(short source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);
        public int Convert(ushort source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);
        public int Convert(sbyte source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);
        public int Convert(uint source, int destination, ResolutionContext context) => System.Convert.ToInt32(source);
    }
}