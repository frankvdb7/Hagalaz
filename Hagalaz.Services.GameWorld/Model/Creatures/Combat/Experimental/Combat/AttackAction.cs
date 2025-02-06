namespace Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat
{
    /// <summary>
    /// 
    /// </summary>
    public class AttackAction
    {
        /// <summary>
        /// The target reach action
        /// </summary>
        private TargetReachBase _targetReachAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="AttackAction" /> class.
        /// </summary>
        /// <param name="targetReachAction">The target reach action.</param>
        public AttackAction(TargetReachBase targetReachAction) => _targetReachAction = targetReachAction;
    }
}