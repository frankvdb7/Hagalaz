namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// A simple item containing only the most basic information about an item.
    /// </summary>
    public struct ItemDto : IItemBase
    {
        /// <summary>
        /// The item id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The item count.
        /// </summary>
        public int Count { get; set; } = 1;

        public ItemDto()
        {
            
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public ItemDto(int id) => Id = id;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="count"></param>
        public ItemDto(int id, int count)
        {
            Id = id;
            Count = count;
        }
    }
}