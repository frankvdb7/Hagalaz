namespace Hagalaz.Cache
{
    public interface IReferenceTableProvider
    {
        IReferenceTable ReadReferenceTable(int indexId);
    }
}
