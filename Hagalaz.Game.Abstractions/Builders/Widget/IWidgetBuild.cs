using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IWidgetBuild
    {
        IWidget Build();
        void Open();
    }
}