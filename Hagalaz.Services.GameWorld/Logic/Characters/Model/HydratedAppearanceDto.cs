using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedAppearanceDto
    {
        public int Gender { get; init; }
        public int HairColor { get; init; }
        public int TorsoColor { get; init; }
        public int LegColor { get; init; }
        public int FeetColor { get; init; }
        public int SkinColor { get; init; }
        public int HairLook { get; init; }
        public int BeardLook { get; init; }
        public int TorsoLook { get; init; }
        public int ArmsLook { get; init; }
        public int WristLook { get; init; }
        public int LegsLook { get; init; }
        public int FeetLook { get; init; }
        public DisplayTitle DisplayTitle { get; init; }
    }
}
