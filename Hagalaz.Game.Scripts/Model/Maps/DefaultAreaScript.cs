namespace Hagalaz.Game.Scripts.Model.Maps
{
    using Hagalaz.Game.Configuration;
    using Microsoft.Extensions.Options;

    /// <summary>
    /// Class DefaultAreaScript
    /// </summary>
    public class DefaultAreaScript : AreaScript
    {
        public DefaultAreaScript(IOptions<WorldOptions> worldOptions) : base(worldOptions) { }

        /// <summary>
        /// Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}
