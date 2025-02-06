using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Model.Items
{
    /// <summary>
    /// Represents a single item.
    /// </summary>
    public class Item : IItem
    {
        /// <summary>
        /// The item id.
        /// </summary>
        /// <value>The id.</value>
        public int Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => ItemDefinition.Name;

        /// <summary>
        /// The quantity of the item.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; set; }

        /// <summary>
        /// The item's definition.
        /// </summary>
        /// <value>The item definition.</value>
        public IItemDefinition ItemDefinition { get; }

        /// <summary>
        /// Contains item as equipment definition.
        /// </summary>
        /// <value>The equipment definition.</value>
        public IEquipmentDefinition EquipmentDefinition { get; }

        /// <summary>
        /// Contains this item script.
        /// </summary>
        /// <value>The item script.</value>
        public IItemScript ItemScript { get; }

        /// <summary>
        /// Contains this item as equipment script.
        /// </summary>
        /// <value>The equipment script.</value>
        public IEquipmentScript EquipmentScript { get; }

        /// <summary>
        /// Contains item extra data.
        /// </summary>
        public long[] ExtraData { get; private set; } = [];

        /// <summary>
        /// Constructs the item specified by the item id, and the quantity.
        /// </summary>
        /// <param name="id">The item id.</param>
        /// <param name="count">The item quantity.</param>
        /// <param name="itemScript">The item script.</param>
        /// <param name="equipmentScript">The equipment script.</param>
        /// <exception cref="System.InvalidOperationException">Item[id,count] cannot be negative.</exception>
        [Obsolete("Use the item builder instead")]
        public Item(int id, int count = 1, IItemScript? itemScript = null, IEquipmentScript? equipmentScript = null)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(id, nameof(id));
            ArgumentOutOfRangeException.ThrowIfNegative(count, nameof(count));

            Id = id;
            Count = count;

            using var scope = ServiceLocator.Current.CreateScope();
            var itemManager = scope.ServiceProvider.GetRequiredService<IItemService>();
            ItemDefinition = itemManager.FindItemDefinitionById(id);

            var equipmentRepository = scope.ServiceProvider.GetRequiredService<IEquipmentService>();
            EquipmentDefinition = equipmentRepository.FindEquipmentDefinitionById(id);
            var itemScriptRepository = scope.ServiceProvider.GetRequiredService<IItemScriptProvider>();
            ItemScript = itemScript ?? itemScriptRepository.FindItemScriptById(id);

            var equipmentScriptRepository = scope.ServiceProvider.GetRequiredService<IEquipmentScriptProvider>();
            EquipmentScript = equipmentScript ?? equipmentScriptRepository.FindEquipmentScriptById(id);
        }

        public Item(
            int id, int count, IItemDefinition itemDefinition, IEquipmentDefinition equipmentDefinition, IItemScript itemScript,
            IEquipmentScript equipmentScript)
        {
            Id = id;
            Count = count;
            ItemDefinition = itemDefinition;
            EquipmentDefinition = equipmentDefinition;
            ItemScript = itemScript;
            EquipmentScript = equipmentScript;
        }

        /// <summary>
        /// Get's if slot is valid for extra data.
        /// </summary>
        /// <param name="index">Slot which should be checked.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ValidExtraDataSlot(int index)
        {
            if (index < 0 || index >= ExtraData.Length) return false;
            return true;
        }

        /// <summary>
        /// Set's extra data, returns false
        /// if slot is invalid, otherwise - true.
        /// </summary>
        /// <param name="index">Slot which should be set.</param>
        /// <param name="value">Value which should be set.</param>
        /// <returns>If setting was sucessfull.</returns>
        public bool SetExtraData(int index, long value)
        {
            if (index < 0)
                return false;
            else if (index >= ExtraData.Length) ReboundExtraData(index + 1);
            ExtraData[index] = value;
            return true;
        }

        /// <summary>
        /// Rebound's extra data.
        /// </summary>
        /// <param name="newLength">New length of extra data, can't be lower than 0</param>
        /// <exception cref="System.Exception"></exception>
        public void ReboundExtraData(int newLength)
        {
            if (newLength < 0) throw new Exception("ReboundExtraData(" + newLength + ") - newLength can't be <0!");
            var newdata = new long[newLength];
            for (int i = 0; i < newdata.Length; i++)
            {
                if (i >= ExtraData.Length) break;
                newdata[i] = ExtraData[i];
            }

            ExtraData = newdata;
        }

        /// <summary>
        /// Serializes the data for database storing.
        /// </summary>
        /// <returns>Returns a string for database.</returns>
        public string? SerializeExtraData()
        {
            if (ExtraData.Length <= 0) return null;
            var sb = new StringBuilder();
            for (var i = 0; i < ExtraData.Length; i++)
            {
                sb.Append(ExtraData[i]);
                if (i + 1 < ExtraData.Length) sb.Append(',');
            }

            var result = sb.ToString();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        /// <summary>
        /// Unserializes the sqldata.
        /// </summary>
        /// <param name="sqlData">The data retrieved from sql database.</param>
        public void UnserializeExtraData(string sqlData)
        {
            if (sqlData.Length == 0) return;
            var dataSplit = sqlData.Split(',');
            ExtraData = new long[dataSplit.Length];

            for (var i = 0; i < dataSplit.Length; i++)
            {
                if (long.TryParse(dataSplit[i], out var data))
                {
                    ExtraData[i] = data;
                }
            }
        }

        /// <summary>
        /// Copies this item.
        /// </summary>
        /// <returns>Item.</returns>
        public IItem Clone()
        {
            var newItem = new Item(Id, Count, ItemDefinition, EquipmentDefinition, ItemScript, EquipmentScript)
            {
                ExtraData = new long[ExtraData.Length]
            };
            for (var i = 0; i < ExtraData.Length; i++) newItem.ExtraData[i] = ExtraData[i];
            return newItem;
        }

        /// <summary>
        /// Copies the specified new count.
        /// </summary>
        /// <param name="newCount">The new count.</param>
        /// <returns></returns>
        public IItem Clone(int newCount)
        {
            var newItem = new Item(Id, newCount, ItemDefinition, EquipmentDefinition, ItemScript, EquipmentScript)
            {
                ExtraData = new long[ExtraData.Length]
            };
            for (var i = 0; i < ExtraData.Length; i++) newItem.ExtraData[i] = ExtraData[i];
            return newItem;
        }

        /// <summary>
        /// Get's if this item equals to other item.
        /// </summary>
        /// <param name="otherItem">The other item.</param>
        /// <param name="ignoreCount">Wheter item count should be ignored.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool Equals(IItem otherItem, bool ignoreCount = true)
        {
            if (Id != otherItem.Id || (!ignoreCount && Count != otherItem.Count))
            {
                return false;
            }

            if (ExtraData.Length != otherItem.ExtraData.Length) return false;
            return !ExtraData.Where((t, i) => t != otherItem.ExtraData[i]).Any();
        }


        /// <summary>
        /// This item summarized as a string.
        /// </summary>
        /// <returns>Returns a string.</returns>
        public override string ToString() => "item[id=" + Id + ",name=" + ItemDefinition.Name + ",count=" + Count + ",extra={" + SerializeExtraData() + "}]";
    }
}