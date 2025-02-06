using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Characters.Messages.Model
{
    public record ProfileDto
    {
        [StringSyntax(StringSyntaxAttribute.Json)]
        public string JsonData { get; init; } = string.Empty;
    }
}
