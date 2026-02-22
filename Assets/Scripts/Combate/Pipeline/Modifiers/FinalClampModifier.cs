using UnityEngine;

namespace Combate.Modifiers
{
    /// <summary>
    /// Etapa final: asegura que los canales no sean negativos
    /// y escribe FinalDamage = Max(1, Round(Physical + Elemental)).
    /// Siempre debe ser el último modificador del pipeline (Order 10000).
    /// </summary>
    public sealed class FinalClampModifier : IDamageModifier
    {
        public int Order => 10000;

        public void Modify(DamageContext context)
        {
            if (context.PhysicalDamage < 0f)  context.PhysicalDamage  = 0f;
            if (context.ElementalDamage < 0f) context.ElementalDamage = 0f;

            context.FinalDamage = Mathf.Max(1, Mathf.RoundToInt(context.TotalRawDamage));
        }
    }
}
