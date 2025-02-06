namespace Hagalaz.Data.Entities
{
    public partial class CharactersVariable
    {
        public uint MasterId { get; set; }
        public string Variable { get; set; } = null!;
        public int? IntValue { get; set; }
        public string? StringValue { get; set; }
    }
}
