using UnityEngine;

namespace Combate.Modifiers
{
    /// <summary>
    /// Etapa inicial: establece los canales de daño físico y elemental
    /// a partir de las estadísticas del atacante.
    /// Si el contexto ya tiene valores base (HasBaseValues = true),
    /// no sobreescribe — permite que DamageEffect u otros callers
    /// pre-configuren el daño.
    /// </summary>
    public sealed class BaseDamageModifier : IDamageModifier
    {
        public int Order => 100;

        public void Modify(DamageContext context)
        {
            if (context.HasBaseValues) return;

            // Canal físico = ataque base de la entidad
            context.PhysicalDamage = context.Attacker.PuntosDeAtaque_Entidad;

            // Canal elemental = ataque elemental de CombatStats
            var stats = context.Attacker.CombatStats;
            context.ElementalDamage = stats?.elementalAttack ?? 0f;

            // Elemento del ataque (si no fue ya configurado)
            if (context.AttackElement == Flags.ElementAttribute.None)
                context.AttackElement = stats?.elementoAtaque ?? Flags.ElementAttribute.None;
        }
    }
}
