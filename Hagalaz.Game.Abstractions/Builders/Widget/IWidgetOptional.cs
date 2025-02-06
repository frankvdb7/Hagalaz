using System.ComponentModel;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Builders.Widget
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IWidgetOptional : IWidgetBuild
    {
        IWidgetOptional WithTransparency(int transparency);
        IWidgetOptional WithParentId(int parentId);
        IWidgetOptional WithParentSlot(int parentSlot);
        IWidgetOptional WithScript(IWidgetScript script);
        IWidgetOptional WithScript<TScript>() where TScript : IWidgetScript;
        IWidgetOptional AsFrame();
    }
}