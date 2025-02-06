using System.ComponentModel;

namespace Hagalaz.Game.Abstractions.Builders.HintIcon
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHintIconOptional : IHintIconBuild
    {
        IHintIconOptional WithModelId(int modelId);
        IHintIconOptional WithArrowId(int arrowId);
    }
}