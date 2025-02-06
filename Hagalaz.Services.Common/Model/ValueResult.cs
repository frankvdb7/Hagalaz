using System.ComponentModel.DataAnnotations;

namespace Hagalaz.Services.Common.Model
{
    public record ValueResult<T>
    {
        [Required]
        public T? Result { get; init; }
    }
}