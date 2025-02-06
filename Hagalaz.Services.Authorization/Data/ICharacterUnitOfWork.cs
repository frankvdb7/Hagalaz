using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.Authorization.Data
{
    public interface ICharacterUnitOfWork : IUnitOfWork
    {
        ICharacterOffenceRepository CharacterOffenceRepository { get; }
    }
}