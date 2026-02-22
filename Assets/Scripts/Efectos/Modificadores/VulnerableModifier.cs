using Combate;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Vulnerable: aumenta el daño físico y elemental recibido por el portador.
    /// Actúa directamente sobre el DamageContext cuando la entidad es el defensor.
    ///
    /// Parámetros en el SO:
    ///   "damageIncreasePercent" – fracción de aumento de daño recibido (ej: 0.25 = +25%)
    /// </summary>
    public sealed class VulnerableModifier : BaseEffectModifier
    {
        // Order bajo para que actúe antes de los modificadores de daño finales
        public override int Order => 50;

        public override void Modify(DamageContext context, EffectInstance instance)
        {
            // Solo aplica cuando la entidad que lo porta es el defensor
            if (context.Defender != instance.Owner) return;

            float increase = instance.GetParam("damageIncreasePercent", 0.25f);

            context.PhysicalDamage  *= (1f + increase);
            context.ElementalDamage *= (1f + increase);

            Debug.Log($"[Vulnerable] {instance.Owner.Nombre_Entidad} recibe +{increase * 100f}% daño.");
        }
    }
}
