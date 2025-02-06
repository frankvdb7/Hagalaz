using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Builders.Region
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRegionUpdateOptional : IRegionUpdateBuild
    {
        IRegionUpdateBuild WithGraphic(IGraphic graphic);
    }
}