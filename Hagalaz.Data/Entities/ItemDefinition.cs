namespace Hagalaz.Data.Entities
{
    public partial class ItemDefinition
    {
        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public string Examine { get; set; } = null!;
        public int TradePrice { get; set; }
        public int LowAlchemyValue { get; set; }
        public int HighAlchemyValue { get; set; }
        public byte Tradeable { get; set; }
        public decimal Weight { get; set; }
    }
}
