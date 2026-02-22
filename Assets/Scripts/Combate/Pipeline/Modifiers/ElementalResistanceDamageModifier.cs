using Flags;
using UnityEngine;

namespace Combate.Modifiers
{
    /// <summary>
    /// Aplica resistencia elemental al canal de daño elemental.
    ///   ELEM_MULT = clamp(1 - resistencia, 0.1, 1.5)
    /// Respeta IsTrueDamage.
    /// No aplica si no hay daño elemental o no hay elemento de ataque.
    /// </summary>
    public sealed class ElementalResistanceDamageModifier : IDamageModifier
    {
        public int Order => 500;

        public void Modify(DamageContext context)
        {
            if (context.IsTrueDamage) return;
            if (context.ElementalDamage <= 0f) return;
            if (context.AttackElement == ElementAttribute.None) return;

            var defenderStats = context.Defender.CombatStats;
            float resistance = defenderStats?.resistencias?.GetResistance(context.AttackElement) ?? 0f;
            float elemMult = Mathf.Clamp(1f - resistance, 0.1f, 1.5f);

            context.ElementalMultiplier = elemMult;
            context.ElementalDamage *= elemMult;
        }
    }
}
