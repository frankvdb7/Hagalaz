using System;
using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    internal enum AkrisaeStyle
    {
        None,
        Melee,
        Ranged,
        Magic
    }

    /// <summary>
    ///     Akrisae's tiny protection-prayer state: once a combat style has dealt <see cref="SwitchDamage" /> to him he
    ///     protects against it, and that style's hits are reduced. Not a general NPC prayer system.
    ///     The threshold is the pre-EoC figure from period guides (Darkan archive, Tip.It); his 1,050 life points in the
    ///     database make it meaningful, where the post-EoC 1,500 would exceed them. The reduction reuses the 40% that the
    ///     tormented demon applies; no period source gives Akrisae's own figure.
    /// </summary>
    internal sealed class AkrisaeProtection
    {
        public const int SwitchDamage = 150;
        private const double ProtectedFactor = 0.60;

        private readonly int[] _damage = new int[4];

        public AkrisaeStyle Protected { get; private set; } = AkrisaeStyle.None;

        public static AkrisaeStyle StyleOf(DamageType type) => type switch
        {
            DamageType.StandardMelee or DamageType.FullMelee => AkrisaeStyle.Melee,
            DamageType.StandardRange or DamageType.FullRange => AkrisaeStyle.Ranged,
            DamageType.StandardMagic or DamageType.FullMagic => AkrisaeStyle.Magic,
            _ => AkrisaeStyle.None
        };

        /// <summary>
        ///     Records damage dealt to him. Returns true when this hit made him switch his protection.
        /// </summary>
        public bool Record(DamageType type, int damage)
        {
            var style = StyleOf(type);
            if (style == AkrisaeStyle.None || damage <= 0)
            {
                return false;
            }

            _damage[(int)style] += damage;
            if (_damage[(int)style] < SwitchDamage)
            {
                return false;
            }

            Array.Clear(_damage);
            if (Protected == style)
            {
                return false;
            }

            Protected = style;
            return true;
        }

        public int Reduce(DamageType type, int damage) =>
            damage > 0 && Protected != AkrisaeStyle.None && StyleOf(type) == Protected ? (int)(damage * ProtectedFactor) : damage;

        public void Reset()
        {
            Array.Clear(_damage);
            Protected = AkrisaeStyle.None;
        }
    }
}
