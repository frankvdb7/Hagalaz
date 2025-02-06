using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Hagalaz.Services.Common.Model
{
    public record MultiValueResult<T>
    {
        [Required]
        public IEnumerable<T> Results { get; init; } = default!;
    }
}