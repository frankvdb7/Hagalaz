using System.Collections.Generic;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    public interface IItemType : IType
    {
        /// <summary>
        /// Contains ID of this item.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IItemType"/> is noted.
        /// </summary>
        /// <value>
        ///   <c>true</c> if noted; otherwise, <c>false</c>.
        /// </value>
        bool Noted { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="IItemType"/> is stackable.
        /// </summary>
        /// <value>
        ///   <c>true</c> if stackable; otherwise, <c>false</c>.
        /// </value>
        bool Stackable { get; }

        /// <summary>
        /// Gets the note identifier.
        /// </summary>
        /// <value>
        /// The note identifier.
        /// </value>
        int NoteId { get; }

        /// <summary>
        /// Get's item render animation ID.
        /// </summary>
        /// <returns>Return's render animation ID or -1 if it doesn't have it.</returns>
        int RenderAnimationId { get; }

        /// <summary>
        /// Determines whether [has destroy option].
        /// </summary>
        /// <returns><c>true</c> if [has destroy option]; otherwise, <c>false</c>.</returns>
        bool HasDestroyOption { get; }

        /// <summary>
        /// Gets the note template ID.
        /// </summary>
        /// <value>The note template ID.</value>
        int NoteTemplateId { get; }

        /// <summary>
        /// Gets the lend template ID.
        /// </summary>
        /// <value>The lend template ID.</value>
        int LendTemplateId { get; }

        /// <summary>
        /// Gets the unknown template ID.
        /// </summary>
        /// <value>The unknown template ID.</value>
        int BoughtTemplateId { get; }

        /// <summary>
        /// Gets the unknown item ID.
        /// </summary>
        /// <value>The unknown item ID.</value>
        int BoughtItemId { get; }

        /// <summary>
        /// Gets the lend ID.
        /// </summary>
        /// <value>The lend ID.</value>
        int LendId { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        int Value { get; }

        /// <summary>
        /// Gets the type of the item degrade (if dropped).
        /// Used to determine if the item should be protected, destroyed or dropped.
        /// </summary>
        DegradeType DegradeType { get; }

        /// <summary>
        /// Gets a value indicating whether [members only].
        /// </summary>
        /// <value><c>true</c> if [members only]; otherwise, <c>false</c>.</value>
        bool MembersOnly { get; }

        /// <summary>
        /// Gets the male worn model id1.
        /// </summary>
        /// <value>The male worn model id1.</value>
        int MaleWornModelId1 { get; }

        /// <summary>
        /// Gets the female worn model id1.
        /// </summary>
        /// <value>The female worn model id1.</value>
        int MaleWornModelId2 { get; }

        /// <summary>
        /// Gets the male worn model id2.
        /// </summary>
        /// <value>The male worn model id2.</value>
        int FemaleWornModelId1 { get; }

        /// <summary>
        /// Gets the female worn model id2.
        /// </summary>
        /// <value>The female worn model id2.</value>
        int FemaleWornModelId2 { get; }

        /// <summary>
        /// Gets the unknown int1.
        /// </summary>
        /// <value>The unknown int1.</value>
        int MaleHeadModel { get; }

        /// <summary>
        /// Gets the unknown int2.
        /// </summary>
        /// <value>The unknown int2.</value>
        int FemaleHeadModel { get; }

        /// <summary>
        /// Gets the unknown int3.
        /// </summary>
        /// <value>The unknown int3.</value>
        int MaleHeadModel2 { get; }

        /// <summary>
        /// Gets the unknown int4.
        /// </summary>
        /// <value>The unknown int4.</value>
        int FemaleHeadModel2 { get; }

        /// <summary>
        /// Gets the unknown int12.
        /// </summary>
        /// <value>The unknown int12.</value>
        int MaleWearXOffset { get; }

        /// <summary>
        /// Gets the unknown int13.
        /// </summary>
        /// <value>The unknown int13.</value>
        int MaleWearYOffset { get; }

        /// <summary>
        /// Gets the unknown int14.
        /// </summary>
        /// <value>The unknown int14.</value>
        int MaleWearZOffset { get; }

        /// <summary>
        /// Gets the unknown int15.
        /// </summary>
        /// <value>The unknown int15.</value>
        int FemaleWearXOffset { get; }

        /// <summary>
        /// Gets the unknown int16.
        /// </summary>
        /// <value>The unknown int16.</value>
        int FemaleWearYOffset { get; }

        /// <summary>
        /// Gets the unknown int17.
        /// </summary>
        /// <value>The unknown int17.</value>
        int FemaleWearZOffset { get; }

        /// <summary>
        /// Gets the interface model id.
        /// </summary>
        /// <value>The interface model id.</value>
        int InterfaceModelId { get; }

        /// <summary>
        /// Gets the original model colors.
        /// </summary>
        /// <value>The original model colors.</value>
        int[]? OriginalModelColors { get; }

        /// <summary>
        /// Gets the modified model colors.
        /// </summary>
        /// <value>The modified model colors.</value>
        int[]? ModifiedModelColors { get; }

        /// <summary>
        /// Gets the texture colour1.
        /// </summary>
        /// <value>The texture colour1.</value>
        int[]? OriginalTextureColors { get; }

        /// <summary>
        /// Gets the texture colour2.
        /// </summary>
        /// <value>The texture colour2.</value>
        int[]? ModifiedTextureColors { get; }

        /// <summary>
        /// Gets the model offset1.
        /// </summary>
        /// <value>The model offset1.</value>
        int ModelOffset1 { get; }

        /// <summary>
        /// Gets the model offset2.
        /// </summary>
        /// <value>The model offset2.</value>
        int ModelOffset2 { get; }

        /// <summary>
        /// Gets the colour equip1.
        /// </summary>
        /// <value>The colour equip1.</value>
        int MaleWornModelId3 { get; }

        /// <summary>
        /// Gets the colour equip2.
        /// </summary>
        /// <value>The colour equip2.</value>
        int FemaleWornModelId3 { get; }

        /// <summary>
        /// Gets the ground options.
        /// </summary>
        /// <value>The ground options.</value>
        string?[] GroundOptions { get; }

        /// <summary>
        /// Contains item extra data.
        /// </summary>
        /// <value>The extra data.</value>
        IReadOnlyDictionary<int, object>? ExtraData { get; }

        /// <summary>
        /// Gets the model zoom.
        /// </summary>
        /// <value>The model zoom.</value>
        int ModelZoom { get; }

        /// <summary>
        /// Gets the unknown int5.
        /// </summary>
        /// <value>The unknown int5.</value>
        int Zan2D { get; }

        /// <summary>
        /// Gets the unknown array1.
        /// </summary>
        /// <value>The unknown array1.</value>
        sbyte[]? UnknownArray1 { get; }

        /// <summary>
        /// Gets the model rotation2.
        /// </summary>
        /// <value>The model rotation2.</value>
        int ModelRotation2 { get; }

        /// <summary>
        /// Gets the inventory options.
        /// </summary>
        /// <value>The inventory options.</value>
        string?[] InventoryOptions { get; }

        /// <summary>
        /// Gets the team id.
        /// </summary>
        /// <value>The team id.</value>
        int TeamId { get; }

        /// <summary>
        /// Gets the equip ID.
        /// </summary>
        /// <value>
        /// The equip ID.
        /// </value>
        sbyte EquipSlot { get; }

        /// <summary>
        /// Gets the type of the equip.
        /// </summary>
        /// <value>
        /// The type of the equip.
        /// </value>
        sbyte EquipType { get; }

        /// <summary>
        /// Gets the model rotation1.
        /// </summary>
        /// <value>The model rotation1.</value>
        int ModelRotation1 { get; }

        /// <summary>
        /// Gets the stackable.
        /// </summary>
        /// <value>The stackable.</value>
        int StackableType { get; }

        /// <summary>
        /// Get's if this item has a special bar.
        /// </summary>
        /// <returns>Return's true if this item has special bar.</returns>
        bool HasSpecialBar();

        /// <summary>
        /// Get's Quest ID of this item.
        /// </summary>
        /// <returns>Return's quest ID or -1 if it's not found.</returns>
        int GetQuestId();

        /// <summary>
        /// Get's attack bonus types of this weapon.
        /// Return's array of size 4.
        /// Stab = 1,
        /// Slash = 2,
        /// Crush = 3,
        /// Ranged = 4,
        /// Magic = 5,
        /// </summary>
        /// <returns>System.Int32[][].</returns>
        int[] GetAttackBonusTypes();

        /// <summary>
        /// Get's attack styles types of this weapon.
        /// Return's array of size 4.
        /// MeleeAccurate = 1,
        /// MeleeAggressive = 2,
        /// MeleeDefensive = 3,
        /// MeleeControlled = 4,
        /// RangedAccurate = 5,
        /// RangedRapid = 6,
        /// RangedLongrange = 7,
        /// MagicNormal = 8,
        /// MagicDefensive = 9,
        /// </summary>
        /// <returns>System.Int32[][].</returns>
        int[] GetAttackStylesTypes();

        /// <summary>
        /// Gets the equipment level requirements.
        /// </summary>
        /// <returns>Dictionary{System.Int32System.Int32}.</returns>
        IReadOnlyDictionary<int, int> GetEquipmentRequirements();

        /// <summary>
        /// Gets the create item requirements.
        /// </summary>
        /// <returns></returns>
        IReadOnlyDictionary<int, int>? GetCreateItemRequirements();

        /// <summary>
        /// Gets the attack speed.
        /// </summary>
        /// <returns></returns>
        int GetAttackSpeed();

        /// <summary>
        /// Gets the stab attack.
        /// </summary>
        /// <returns></returns>
        int GetStabAttack();

        /// <summary>
        /// Gets the slash attack.
        /// </summary>
        /// <returns></returns>
        int GetSlashAttack();

        /// <summary>
        /// Gets the crush attack.
        /// </summary>
        /// <returns></returns>
        int GetCrushAttack();

        /// <summary>
        /// Gets the magic attack.
        /// </summary>
        /// <returns></returns>
        int GetMagicAttack();

        /// <summary>
        /// Gets the range attack.
        /// </summary>
        /// <returns></returns>
        int GetRangeAttack();

        /// <summary>
        /// Gets the stab defence.
        /// </summary>
        /// <returns></returns>
        int GetStabDefence();

        /// <summary>
        /// Gets the slash defence.
        /// </summary>
        /// <returns></returns>
        int GetSlashDefence();

        /// <summary>
        /// Gets the crush defence.
        /// </summary>
        /// <returns></returns>
        int GetCrushDefence();

        /// <summary>
        /// Gets the magic defence.
        /// </summary>
        /// <returns></returns>
        int GetMagicDefence();

        /// <summary>
        /// Gets the range defence.
        /// </summary>
        /// <returns></returns>
        int GetRangeDefence();

        /// <summary>
        /// Gets the summoning defence.
        /// </summary>
        /// <returns></returns>
        int GetSummoningDefence();

        /// <summary>
        /// Gets the absorb melee bonus.
        /// </summary>
        /// <returns></returns>
        int GetAbsorbMeleeBonus();

        /// <summary>
        /// Gets the absorb mage bonus.
        /// </summary>
        /// <returns></returns>
        int GetAbsorbMageBonus();

        /// <summary>
        /// Gets the absorb range bonus.
        /// </summary>
        /// <returns></returns>
        int GetAbsorbRangeBonus();

        /// <summary>
        /// Gets the strength bonus.
        /// </summary>
        /// <returns></returns>
        int GetStrengthBonus();

        /// <summary>
        /// Gets the ranged strength bonus.
        /// </summary>
        /// <returns></returns>
        int GetRangedStrengthBonus();

        /// <summary>
        /// Gets the magic damage.
        /// </summary>
        /// <returns></returns>
        int GetMagicDamage();

        /// <summary>
        /// Gets the prayer bonus.
        /// </summary>
        /// <returns></returns>
        int GetPrayerBonus();

        /// <summary>
        /// Adds note data to this item definition.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="template">The template.</param>
        void MakeNote(IItemType item, IItemType template);

        /// <summary>
        /// Add's lend data to this item definition.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="template">The template.</param>
        void MakeLend(IItemType item, IItemType template);

        /// <summary>
        /// Add's data which is unknown to this item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="template">The template.</param>
        void MakeBought(IItemType item, IItemType template);
    }
}