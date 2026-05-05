using Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Combat.Experimental.Combat
{
    [TestClass]
    public class CombatPhaseTests
    {
        [TestMethod]
        public void ActiveRotation_Set_ActivatesNewRotation()
        {
            var phase = new CombatPhase<ICombatRotation>();
            var rotation = Substitute.For<ICombatRotation>();

            phase.ActiveRotation = rotation;

            rotation.Received(1).Activate();
            Assert.AreEqual(rotation, phase.ActiveRotation);
        }

        [TestMethod]
        public void Active_SetTrue_InvokesOnActivated()
        {
            var phase = new CombatPhase<ICombatRotation>();
            bool activated = false;
            phase.OnActivated += () => activated = true;

            phase.Activate();

            Assert.IsTrue(activated);
        }
    }
}
