using UnityEngine;

namespace Combate.Modifiers
{
    /// <summary>
    /// Aplica reducción de daño por defensa usando fórmula hiperbólica:
    ///   DEF_MULT = 1 / (1 + DEF / K)
    /// Solo afecta al canal físico. El elemental se maneja por resistencias.
    /// Respeta IgnoreDefense e IsTrueDamage.
    /// </summary>
    public sealed class DefenseDamageModifier : IDamageModifier
    {
        public int Order => 400;

        public void Modify(DamageContext context)
        {
            if (context.IgnoreDefense || context.IsTrueDamage) return;

            float defense = context.Defender.PuntosDeDefensa_Entidad * context.RaceDefMultiplier;
            float k = context.DefenseConstantK > 0 ? context.DefenseConstantK : 22f;

            float defMult = defense <= 0 ? 1f : Mathf.Clamp01(1f / (1f + defense / k));

            context.DefenseMultiplier = defMult;
            context.PhysicalDamage *= defMult;
        }
    }
}
