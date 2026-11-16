using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Services.Cache.Features.Types.Dtos;

/// <summary>The verified read-only quest definition. Opcodes whose meaning is unproven are not exposed.</summary>
public sealed record GetQuestDefinitionDto(
    int Id,
    string Name,
    int QuestPoints,
    int Category,
    int Difficulty,
    bool Members,
    int GraphicId,
    int QuestPointRequirement,
    IReadOnlyList<QuestProgressThreshold> ProgressVarps,
    IReadOnlyList<QuestProgressThreshold> ProgressVarBits,
    IReadOnlyList<int> PrerequisiteQuestIds,
    IReadOnlyList<QuestSkillRequirement> SkillRequirements,
    IReadOnlyList<QuestVariableRequirement> VarpRequirements,
    IReadOnlyList<QuestVariableRequirement> VarbitRequirements,
    IReadOnlyDictionary<int, object> Params)
{
    public static GetQuestDefinitionDto FromQuest(IQuestType quest) => new(
        quest.Id,
        quest.Name,
        quest.QuestPoints,
        quest.Category,
        quest.Difficulty,
        quest.Members,
        quest.GraphicId,
        quest.QuestPointRequirement,
        quest.ProgressVarps.ToArray(),
        quest.ProgressVarBits.ToArray(),
        quest.PrerequisiteQuestIds.ToArray(),
        quest.SkillRequirements.ToArray(),
        quest.VarpRequirements.ToArray(),
        quest.VarbitRequirements.ToArray(),
        quest.ExtraData);
}
