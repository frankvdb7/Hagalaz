using System.Linq;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    /// <summary>
    /// </summary>
    public record FletchingDefinition
    {
        /// <summary>
        ///     The resource identifier.
        ///     This item is necessary to create products with the tool.
        /// </summary>
        public int ResourceID { get; init; }

        /// <summary>
        ///     The tool identifier.
        ///     This item is necessary to create products from the resource.
        /// </summary>
        public int ToolId { get; init; }

        /// <summary>
        ///     The product ids.
        /// </summary>
        public int[] ProductIDs { get; init; } = [];

        /// <summary>
        ///     The product amounts.
        /// </summary>
        public int[] ProductAmounts { get; init; } = [];

        /// <summary>
        ///     The required levels.
        /// </summary>
        public int[] RequiredLevels { get; init; } = [];

        /// <summary>
        ///     The experience.
        /// </summary>
        public double[] Experience { get; init; } = [];

        /// <summary>
        ///     The animation identifier.
        /// </summary>
        public int AnimationId { get; init; } = -1;

        public FletchingDefinition(int usedID, int usedWithID, int[] productIDs, int[] requiredLevels, double[] experience, int animationId = -1)
        {
            ResourceID = usedID;
            ToolId = usedWithID;
            ProductIDs = productIDs;
            RequiredLevels = requiredLevels;
            Experience = experience;
            AnimationId = animationId;

            // Initialize product amounts with a default of 1 for each product
            ProductAmounts = Enumerable.Repeat(1, ProductIDs.Length).ToArray();
        }

        public FletchingDefinition(
            int usedID, int usedWithID, int[] productIDs, int[] requiredLevels, double[] experience, int[] productAmounts, int animationId = -1)
        {
            ResourceID = usedID;
            ToolId = usedWithID;
            ProductIDs = productIDs;
            RequiredLevels = requiredLevels;
            Experience = experience;
            AnimationId = animationId;
            ProductAmounts = productAmounts;
        }
    }
}