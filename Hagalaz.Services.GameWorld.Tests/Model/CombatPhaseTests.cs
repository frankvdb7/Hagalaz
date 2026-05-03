using Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Tests.Model
{
    [TestClass]
    public class CombatPhaseTests
    {
        public interface IMockRotation : ICombatRotation { }

        [TestMethod]
        public void ActiveRotation_CanBeNull()
        {
            var phase = new CombatPhase<IMockRotation>();
            phase.ActiveRotation = null;
            Assert.IsNull(phase.ActiveRotation);
        }

        [TestMethod]
        public void SelectNewRotation_WorksWithEmptyList()
        {
            var phase = new CombatPhase<IMockRotation>();
            phase.SelectNewRotation(list => null!);
            Assert.IsNull(phase.ActiveRotation);
        }
    }
}
