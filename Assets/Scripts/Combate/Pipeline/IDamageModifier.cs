namespace Combate
{
    /// <summary>
    /// Interfaz para etapas del pipeline de daño.
    /// Cada implementación modifica un aspecto del DamageContext.
    /// Se ejecutan en orden ascendente de Order.
    /// 
    /// Convenciones de Order:
    ///   100 - BaseDamage (daño inicial)
    ///   200 - Race (multiplicadores de raza)
    ///   300 - Crit (multiplicador crítico)
    ///   400 - Defense (mitigación por defensa)
    ///   500 - ElementalResistance (resistencia elemental)
    ///   600-900 - Reservado para entity modifiers y effects
    ///   10000 - FinalClamp (clamp final)
    /// </summary>
    public interface IDamageModifier
    {
        /// <summary>Prioridad de ejecución. Menor = antes.</summary>
        int Order { get; }

        /// <summary>
        /// Modifica el contexto de daño en curso.
        /// No tomar decisiones aleatorias aquí — el contexto llega con flags ya resueltos.
        /// </summary>
        void Modify(DamageContext context);
    }
}
