namespace Hagalaz.Game.Scripts.Skills.Smithing
{
    /// <summary>
    /// </summary>
    public class SmithingDefinition
    {
        /// <summary>
        ///     The bar Id.
        /// </summary>
        public int BarID;

        /// <summary>
        ///     The smelt definition.
        /// </summary>
        public SmeltingBarDefinition SmeltDefinition;

        /// <summary>
        ///     The forge definition.
        /// </summary>
        public ForgingBarDefinition ForgeDefinition;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SmithingDefinition" /> struct.
        /// </summary>
        /// <param name="barID">The bar Id.</param>
        /// <param name="smeltDefinition">The smelt definition.</param>
        /// <param name="forgeDefinition">The forge definition.</param>
        public SmithingDefinition(int barID, SmeltingBarDefinition smeltDefinition, ForgingBarDefinition forgeDefinition)
        {
            BarID = barID;
            SmeltDefinition = smeltDefinition;
            ForgeDefinition = forgeDefinition;
        }
    }
}