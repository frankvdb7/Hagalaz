
namespace Hagalaz.Data.Entities
{
    public partial class CharactersProfile
    {
        public uint MasterId { get; set; }
        public string Data { get; set; } = default!;


        public virtual Character Master { get; set; } = null!;
    }
}
