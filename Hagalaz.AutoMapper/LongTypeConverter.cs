using AutoMapper;

namespace Hagalaz.AutoMapper
{
    public class LongTypeConverter : ITypeConverter<ulong, long>
    {
        public long Convert(ulong source, long destination, ResolutionContext context) => System.Convert.ToInt64(source);
    }
}
