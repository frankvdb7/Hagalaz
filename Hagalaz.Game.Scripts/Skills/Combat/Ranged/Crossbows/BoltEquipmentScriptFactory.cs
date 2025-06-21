using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Factories;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows
{
    public class BoltEquipmentScriptFactory : IEquipmentScriptFactory
    {
        public async IAsyncEnumerable<(int itemId, Type scriptType)> GetScripts([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            var boltsType = typeof(Bolts);

            foreach (var result in YieldBoltCollection(Bolts.Bronze, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Blurite, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Silver, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Iron, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Steel, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Black, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Mithril, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Adamantine, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Runite, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Abyssalbane, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Basiliskbane, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Dragonbane, boltsType))
            {
                yield return result;
            }

            foreach (var result in YieldBoltCollection(Bolts.Wallasalkibane, boltsType))
            {
                yield return result;
            }

            yield return (Bolts.Bone, boltsType);
            yield return (Bolts.Barbed, boltsType);
            yield return (Bolts.BoltRack, boltsType);
            yield return (Bolts.BroadTipped, boltsType);
            yield return (Bolts.Kebbit, boltsType);
            yield return (Bolts.LongKebbit, boltsType);
            yield break;

            static IEnumerable<(int itemId, Type scriptType)> YieldBoltCollection(IEnumerable<int> boltCollection, Type scriptType)
            {
                foreach (var itemId in boltCollection)
                {
                    yield return (itemId, scriptType);
                }
            }
        }
    }
}