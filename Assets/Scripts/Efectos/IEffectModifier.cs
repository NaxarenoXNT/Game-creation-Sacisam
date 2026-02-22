using Combate;
using Padres;

namespace Efectos
{
    /// <summary>
    /// Interfaz que define la lógica ejecutable de un tipo de efecto.
    /// Cada implementación es una clase C# pura, sin estado mutable, sin dependencia de Unity.
    /// El estado runtime vive en EffectInstance, no en esta clase.
    ///
    /// Orden de ejecución en el pipeline cuando varios efectos modifican el mismo contexto:
    /// menor Order → se ejecuta primero.
    /// </summary>
    public interface IEffectModifier
    {
        /// <summary>Prioridad de ejecución en el pipeline de daño. Menor = antes.</summary>
        int Order { get; }

        /// <summary>
        /// Se llama una vez cuando el efecto se aplica a la entidad.
        /// Usar para inicializar RuntimeState (ej: capturar ATK del source).
        /// </summary>
        void OnApply(EffectInstance instance, Entidad owner);

        /// <summary>
        /// Se llama cuando el efecto expira o es removido manualmente.
        /// Usar para revertir modificadores de stat o limpiar suscripciones.
        /// </summary>
        void OnRemove(EffectInstance instance, Entidad owner);

        /// <summary>
        /// Se llama al inicio del turno de la entidad portadora.
        /// Usar para daño por turno, regeneración, contadores.
        /// </summary>
        void OnTurnStart(EffectInstance instance, Entidad owner);

        /// <summary>
        /// Se llama al final del turno de la entidad portadora.
        /// </summary>
        void OnTurnEnd(EffectInstance instance, Entidad owner);

        /// <summary>
        /// Participa en el pipeline de daño activo.
        /// Solo se llama si el modificador debe interactuar con un cálculo de daño entrante o saliente.
        /// Modificar context.PhysicalDamage, context.ElementalDamage o los flags según corresponda.
        /// </summary>
        void Modify(DamageContext context, EffectInstance instance);

        /// <summary>
        /// Hook: la entidad portadora acaba de realizar un crítico.
        /// Usar para efectos que reaccionan a críticos propios.
        /// </summary>
        void OnCriticalHit(EffectInstance instance, DamageContext ctx);

        /// <summary>
        /// Hook: la entidad portadora acaba de matar a un enemigo.
        /// </summary>
        void OnKill(EffectInstance instance, Entidad owner);
    }
}
