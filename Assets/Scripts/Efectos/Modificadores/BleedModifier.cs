using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Sangrado: inflige daño por turno proporcional al ATK del atacante
    /// en el momento en que se aplicó el efecto. Stackeable.
    ///
    /// Parámetros en el SO:
    ///   "damagePercent" – fracción del sourceAttack que se inflige por turno (ej: 0.15 = 15%)
    ///
    /// Inmunidades recomendadas en el SO: Undead, Elemental
    /// stackable: true | maxStacks: definido en el SO
    /// </summary>
    public sealed class BleedModifier : BaseEffectModifier
    {
        public override int Order => 10;

        // Capturamos el ATK del atacante en el momento de aplicación,
        // no en el tick. Así el sangrado escala con el poder del golpe original.
        public override void OnApply(EffectInstance instance, Entidad owner)
        {
            float sourceAttack = instance.Source != null
                ? instance.Source.PuntosDeAtaque_Entidad
                : 0f;

            instance.RuntimeState["sourceAttack"] = sourceAttack;
        }

        public override void OnTurnStart(EffectInstance instance, Entidad owner)
        {
            float sourceAttack  = instance.RuntimeState.TryGetValue("sourceAttack", out var sa) ? sa : 0f;
            float damagePercent = instance.GetParam("damagePercent", 0.15f);
            int stacks          = instance.CurrentStacks;

            int bleedDamage = Mathf.Max(1, Mathf.RoundToInt(sourceAttack * damagePercent * stacks));

            // Daño verdadero: bypassa defensa y el pipeline (sangrado no se mitiga)
            owner.RecibirDanoPuro(bleedDamage, Flags.ElementAttribute.None);

            Debug.Log($"[Sangrado] {owner.Nombre_Entidad} pierde {bleedDamage} HP (stack {stacks}, {damagePercent * 100f}% de {sourceAttack} ATK).");
        }
    }
}
