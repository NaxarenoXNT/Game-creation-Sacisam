using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Cegado (Light): reduce la probabilidad de crítico del portador.
    ///
    /// Parámetros en el SO:
    ///   "critReductionPercent" – fracción de reducción de critChance (ej: 0.50 = -50% de critChance)
    ///
    /// Nota: actúa sobre CombatStats del portador. Cuando el sistema de precisión se implemente
    /// se puede expandir aquí sin romper nada.
    /// </summary>
    public sealed class BlindModifier : BaseEffectModifier
    {
        public override int Order => 40;

        public override void OnApply(EffectInstance instance, Entidad owner)
        {
            if (owner.CombatStats == null) return;

            float percent   = instance.GetParam("critReductionPercent", 0.50f);
            float reduction = owner.CombatStats.critChance * percent;

            instance.RuntimeState["critReduction"] = reduction;
            owner.CombatStats.critChance = Mathf.Max(0f, owner.CombatStats.critChance - reduction);

            Debug.Log($"[Cegado] {owner.Nombre_Entidad} critChance reducida en {reduction:F2}.");
        }

        public override void OnRemove(EffectInstance instance, Entidad owner)
        {
            if (owner.CombatStats == null) return;

            if (instance.RuntimeState.TryGetValue("critReduction", out float reduction))
            {
                owner.CombatStats.critChance = Mathf.Min(1f, owner.CombatStats.critChance + reduction);
                Debug.Log($"[Cegado] {owner.Nombre_Entidad} recupera critChance.");
            }
        }
    }
}
