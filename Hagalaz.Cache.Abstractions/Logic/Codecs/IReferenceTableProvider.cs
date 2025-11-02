using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    public interface IReferenceTableProvider
    {
        IReferenceTable ReadReferenceTable(int indexId);
    }
}
