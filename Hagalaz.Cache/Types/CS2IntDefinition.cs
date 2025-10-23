using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Types
{
    public class Cs2IntDefinition : ICs2IntDefinition
    {
        public int Id { get; }
        public char AChar327 { get; internal set; }
        public int AnInt325 { get; internal set; }

        public Cs2IntDefinition(int id)
        {
            Id = id;
            AnInt325 = 1;
        }
    }
}
