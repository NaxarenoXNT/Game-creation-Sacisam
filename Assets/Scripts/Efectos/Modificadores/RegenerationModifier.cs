using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Regeneración: cura al portador al inicio de cada turno.
    ///
    /// Parámetros en el SO:
    ///   "healPercent"    – % de vida máxima que se cura por turno (ej: 0.05 = 5% HP max)
    ///   "healFlat"       – curación fija adicional por turno (puede usarse solo o combinada)
    /// </summary>
    public sealed class RegenerationModifier : BaseEffectModifier
    {
        public override int Order => 5;

        public override void OnTurnStart(EffectInstance instance, Entidad owner)
        {
            float percent  = instance.GetParam("healPercent", 0.05f);
            float flat     = instance.GetParam("healFlat", 0f);
            int   healAmt  = Mathf.RoundToInt(owner.Vida_Entidad * percent + flat);

            if (healAmt > 0)
            {
                owner.Curar(healAmt);
                Debug.Log($"[Regeneración] {owner.Nombre_Entidad} recupera {healAmt} HP.");
            }
        }
    }
}
