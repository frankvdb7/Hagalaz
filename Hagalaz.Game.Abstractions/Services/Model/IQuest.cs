namespace Hagalaz.Game.Abstractions.Services.Model
{
    public interface IQuest
    {
        string Name { get; }
       QuestPrimaryStatus PrimaryStatus { get; set; }
    }
}
