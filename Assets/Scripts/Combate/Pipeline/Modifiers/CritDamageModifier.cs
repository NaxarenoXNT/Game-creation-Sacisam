namespace Combate.Modifiers
{
    /// <summary>
    /// Aplica el multiplicador de daño crítico.
    /// El flag IsCritical y CritMultiplier DEBEN estar resueltos
    /// ANTES de que el pipeline ejecute esta etapa.
    /// El pipeline NO toma decisiones aleatorias.
    /// </summary>
    public sealed class CritDamageModifier : IDamageModifier
    {
        public int Order => 300;

        public void Modify(DamageContext context)
        {
            if (!context.IsCritical) return;

            float critMult = context.CritMultiplier;
            if (critMult <= 1f) critMult = 1.5f;

            context.PhysicalDamage *= critMult;

            // Elemental crit solo si configurado
            if (context.CritAppliesToElemental)
                context.ElementalDamage *= critMult;
        }
    }
}
