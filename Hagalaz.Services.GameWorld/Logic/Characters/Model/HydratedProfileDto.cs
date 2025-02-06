using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Services.GameWorld.Logic.Characters.Model
{
    public record HydratedProfileDto
    {
        [StringSyntax(StringSyntaxAttribute.Json)]
        public required string JsonData { get; init; }
    }
}
