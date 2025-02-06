namespace Hagalaz.Data.Entities
{
    public partial class Shop
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public byte Capacity { get; set; }
        public ushort CurrencyId { get; set; }
        public string MainStockItems { get; set; } = null!;
        public string? SampleStockItems { get; set; }
        public byte GeneralStore { get; set; }
    }
}
