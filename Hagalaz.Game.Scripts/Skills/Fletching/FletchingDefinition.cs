namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    /// <summary>
    /// </summary>
    public class FletchingDefinition
    {
        /// <summary>
        ///     The resource identifier.
        ///     This item is neccessary to create products with the tool.
        /// </summary>
        public int ResourceID;

        /// <summary>
        ///     The tool identifier.
        ///     This item is neccesary to create products from the resource.
        /// </summary>
        public int ToolID;

        /// <summary>
        ///     The product ids.
        /// </summary>
        public int[] ProductIDs;

        /// <summary>
        ///     The product amounts.
        /// </summary>
        public int[] ProductAmounts;

        /// <summary>
        ///     The required levels.
        /// </summary>
        public byte[] RequiredLevels;

        /// <summary>
        ///     The experience.
        /// </summary>
        public double[] Experience;

        /// <summary>
        ///     The animation identifier.
        /// </summary>
        public short AnimationID;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FletchingDefinition" /> struct.
        /// </summary>
        /// <param name="usedID">The used identifier.</param>
        /// <param name="usedWithID">The used with identifier.</param>
        /// <param name="productIDs">The product ids.</param>
        /// <param name="requiredLevels">The required levels.</param>
        /// <param name="experience">The experience granted</param>
        /// <param name="animationID">The animation identifier.</param>
        public FletchingDefinition(int usedID, int usedWithID, int[] productIDs, byte[] requiredLevels, double[] experience, short animationID = -1)
        {
            ResourceID = usedID;
            ToolID = usedWithID;
            ProductIDs = productIDs;
            RequiredLevels = requiredLevels;
            Experience = experience;
            AnimationID = animationID;
            ProductAmounts = new int[productIDs.Length];
            for (var i = 0; i < ProductAmounts.Length; i++)
            {
                ProductAmounts[i] = 1;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FletchingDefinition" /> struct.
        /// </summary>
        /// <param name="usedID">The used identifier.</param>
        /// <param name="usedWithID">The used with identifier.</param>
        /// <param name="productIDs">The product i ds.</param>
        /// <param name="requiredLevels">The required levels.</param>
        /// <param name="experience">The experience.</param>
        /// <param name="productAmounts">The product amounts.</param>
        /// <param name="animationId">The animation identifier.</param>
        public FletchingDefinition(int usedID, int usedWithID, int[] productIDs, byte[] requiredLevels, double[] experience, int[] productAmounts, short animationId = -1)
        {
            ResourceID = usedID;
            ToolID = usedWithID;
            ProductIDs = productIDs;
            RequiredLevels = requiredLevels;
            Experience = experience;
            AnimationID = animationId;
            ProductAmounts = productAmounts;
        }
    }
}