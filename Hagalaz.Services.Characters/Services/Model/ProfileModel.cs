using System.Diagnostics.CodeAnalysis;

namespace Hagalaz.Services.Characters.Services.Model
{
    public record ProfileModel
    {
        [StringSyntax(StringSyntaxAttribute.Json)]
        public string JsonData { get; init; } = string.Empty;
    }
}
