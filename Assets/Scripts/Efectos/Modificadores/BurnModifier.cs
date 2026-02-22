using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Quemado (Fire): inflige daño por turno proporcional al daño elemental
    /// del atacante en el momento de aplicación.
    ///
    /// Parámetros en el SO:
    ///   "burnPercent" – fracción del daño elemental del source por turno (ej: 0.10 = 10%)
    /// </summary>
    public sealed class BurnModifier : BaseEffectModifier
    {
        public override int Order => 15;

        public override void OnApply(EffectInstance instance, Entidad owner)
        {
            // Capturar el ataque elemental del source al momento de aplicación
            float sourceElemental = 0f;
            if (instance.Source != null)
                sourceElemental = instance.Source.CombatStats?.elementalAttack ?? 0f;

            instance.RuntimeState["sourceElemental"] = sourceElemental;
        }

        public override void OnTurnStart(EffectInstance instance, Entidad owner)
        {
            float sourceElemental = instance.RuntimeState.TryGetValue("sourceElemental", out var se) ? se : 0f;
            float burnPercent     = instance.GetParam("burnPercent", 0.10f);

            int burnDamage = Mathf.Max(1, Mathf.RoundToInt(sourceElemental * burnPercent));

            // Daño de fuego verdadero
            owner.RecibirDanoPuro(burnDamage, Flags.ElementAttribute.Fire);

            Debug.Log($"[Quemado] {owner.Nombre_Entidad} pierde {burnDamage} HP (fuego).");
        }
    }
}
