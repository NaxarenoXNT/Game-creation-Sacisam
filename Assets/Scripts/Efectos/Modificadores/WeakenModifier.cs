using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Debilitar: reduce el ATK del portador durante su duración.
    ///
    /// Parámetros en el SO:
    ///   "atkReductionPercent" – porcentaje de ATK que se quita (ej: 0.20 = -20% ATK)
    /// </summary>
    public sealed class WeakenModifier : BaseEffectModifier
    {
        public override int Order => 30;

        public override void OnApply(EffectInstance instance, Entidad owner)
        {
            float percent   = instance.GetParam("atkReductionPercent", 0.20f);
            int reduction   = Mathf.RoundToInt(owner.PuntosDeAtaque_Entidad * percent);

            instance.RuntimeState["atkReduction"] = reduction;
            owner.ModificarAtaque(-reduction);

            Debug.Log($"[Debilitar] {owner.Nombre_Entidad} pierde {reduction} ATK ({percent * 100f}%).");
        }

        public override void OnRemove(EffectInstance instance, Entidad owner)
        {
            if (instance.RuntimeState.TryGetValue("atkReduction", out float reduction))
            {
                owner.ModificarAtaque((int)reduction);
                Debug.Log($"[Debilitar] {owner.Nombre_Entidad} recupera {(int)reduction} ATK.");
            }
        }
    }
}
