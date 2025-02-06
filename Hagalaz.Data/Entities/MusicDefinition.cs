using System.Collections.Generic;

namespace Hagalaz.Data.Entities
{
    public partial class MusicDefinition
    {
        public MusicDefinition()
        {
            MusicLocations = new HashSet<MusicLocation>();
        }

        public ushort Id { get; set; }
        public string Name { get; set; } = null!;
        public string Hint { get; set; } = null!;

        public virtual ICollection<MusicLocation> MusicLocations { get; set; }
    }
}
