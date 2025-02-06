using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;

namespace Hagalaz.Game.Abstractions.Builders.Region
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRegionUpdateBuild
    {
        IRegionPartUpdate Build();
        IRegionPartUpdate Queue();
    }
}