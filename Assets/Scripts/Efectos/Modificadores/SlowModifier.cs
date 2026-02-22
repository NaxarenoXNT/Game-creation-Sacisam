using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Lento: reduce la velocidad del portador durante su duración.
    ///
    /// Parámetros en el SO:
    ///   "speedReductionPercent" – porcentaje de velocidad que se quita (ej: 0.30 = -30%)
    /// </summary>
    public sealed class SlowModifier : BaseEffectModifier
    {
        public override int Order => 30;

        public override void OnApply(EffectInstance instance, Entidad owner)
        {
            float percent   = instance.GetParam("speedReductionPercent", 0.30f);
            int reduction   = Mathf.RoundToInt(owner.Velocidad * percent);

            instance.RuntimeState["speedReduction"] = reduction;
            owner.ModificarVelocidad(-reduction);

            Debug.Log($"[Lento] {owner.Nombre_Entidad} pierde {reduction} velocidad ({percent * 100f}%).");
        }

        public override void OnRemove(EffectInstance instance, Entidad owner)
        {
            if (instance.RuntimeState.TryGetValue("speedReduction", out float reduction))
            {
                owner.ModificarVelocidad((int)reduction);
                Debug.Log($"[Lento] {owner.Nombre_Entidad} recupera {(int)reduction} velocidad.");
            }
        }
    }
}
