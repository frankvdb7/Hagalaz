using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconType
    {
        IHintIconLocationOptional AtLocation(ILocation location);
        IHintIconEntityOptional AtEntity(IEntity entity);
    }
}