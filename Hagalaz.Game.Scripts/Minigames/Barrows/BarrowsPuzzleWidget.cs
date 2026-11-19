using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.Barrows
{
    /// <summary>
    ///     Interface 25: "Which is the next shape in the above sequence?". The server draws the models and receives the clicks.
    /// </summary>
    public class BarrowsPuzzleWidget : WidgetScript
    {
        public BarrowsPuzzleWidget(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        internal BarrowsScript? Barrows { get; set; }

        internal int PuzzleIndex { get; set; }

        internal int[] Choices { get; set; } = [];

        internal int CorrectIndex { get; set; }

        public override void OnOpen()
        {
            var sequence = BarrowsPuzzle.SequenceModels(PuzzleIndex);
            for (var i = 0; i < BarrowsPuzzle.SequenceComponents.Length; i++)
            {
                InterfaceInstance.DrawModel(BarrowsPuzzle.SequenceComponents[i], sequence[i]);
            }

            for (var i = 0; i < BarrowsPuzzle.ChoiceComponents.Length; i++)
            {
                var choice = i;
                InterfaceInstance.DrawModel(BarrowsPuzzle.ChoiceComponents[i], Choices[i]);
                InterfaceInstance.AttachClickHandler(BarrowsPuzzle.ChoiceComponents[i], (_, type, _, _) =>
                {
                    if (type != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    Owner.Widgets.CloseWidget(InterfaceInstance);
                    Barrows?.AnswerPuzzle(choice == CorrectIndex);
                    return true;
                });
            }
        }

        public override void OnClose() { }
    }
}
