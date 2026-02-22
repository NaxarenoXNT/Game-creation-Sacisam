using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Veneno: inflige daño fijo por turno. No stackeable.
    /// Al intentar re-aplicar, solo renueva la duración (manejado en EffectHandler).
    ///
    /// Parámetros en el SO:
    ///   "damagePerTurn" – daño fijo absoluto por turno (ej: 8)
    /// </summary>
    public sealed class PoisonModifier : BaseEffectModifier
    {
        public override int Order => 20;

        public override void OnTurnStart(EffectInstance instance, Entidad owner)
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(instance.GetParam("damagePerTurn", 5f)));

            owner.RecibirDanoPuro(damage, Flags.ElementAttribute.None);

            Debug.Log($"[Veneno] {owner.Nombre_Entidad} pierde {damage} HP.");
        }
    }
}
