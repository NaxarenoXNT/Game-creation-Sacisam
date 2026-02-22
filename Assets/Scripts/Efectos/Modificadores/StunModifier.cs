using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Aturdimiento: impide actuar al portador durante su duración.
    /// No inflige daño. EffectHandler.EstaIncapacitado retorna true cuando este efecto está activo.
    /// No stackeable (renovar duración en re-aplicación es responsabilidad del EffectHandler).
    /// </summary>
    public sealed class StunModifier : BaseEffectModifier
    {
        public override int Order => 0;

        public override void OnApply(EffectInstance instance, Entidad owner)
        {
            Debug.Log($"[Aturdido] {owner.Nombre_Entidad} queda incapacitado por {instance.RemainingTurns} turno/s.");
        }

        public override void OnRemove(EffectInstance instance, Entidad owner)
        {
            Debug.Log($"[Aturdido] {owner.Nombre_Entidad} se recupera del aturdimiento.");
        }
    }
}
