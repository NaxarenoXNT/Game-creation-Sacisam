using Combate;
using Padres;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Clase base que implementa todos los hooks de IEffectModifier como no-ops.
    /// Los modificadores concretos solo sobreescriben los métodos que necesitan.
    /// </summary>
    public abstract class BaseEffectModifier : IEffectModifier
    {
        public virtual int Order => 100;

        public virtual void OnApply(EffectInstance instance, Entidad owner) { }
        public virtual void OnRemove(EffectInstance instance, Entidad owner) { }
        public virtual void OnTurnStart(EffectInstance instance, Entidad owner) { }
        public virtual void OnTurnEnd(EffectInstance instance, Entidad owner) { }
        public virtual void Modify(DamageContext context, EffectInstance instance) { }
        public virtual void OnCriticalHit(EffectInstance instance, DamageContext ctx) { }
        public virtual void OnKill(EffectInstance instance, Entidad owner) { }
    }
}
