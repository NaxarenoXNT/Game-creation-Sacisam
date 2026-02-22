using Padres;
using UnityEngine;

namespace Efectos.Modificadores
{
    /// <summary>
    /// Congelado: impide actuar y reduce velocidad a 0 mientras dure.
    /// Al expirar, restaura la velocidad original.
    ///
    /// Parámetros en el SO:
    ///   "speedReduction" – reducción absoluta de velocidad aplicada (ej: para el valor de velocidad actual)
    ///                       Si 0, se usa velocidad completa como reducción.
    /// </summary>
    public sealed class FreezeModifier : BaseEffectModifier
    {
        public override int Order => 0;

        public override void OnApply(EffectInstance instance, Entidad owner)
        {
            // Guardar velocidad original antes de reducirla
            instance.RuntimeState["originalSpeed"] = owner.Velocidad;

            // Reducir velocidad a 0
            int reduction = -owner.Velocidad; // negativo = subir a 0
            owner.ModificarVelocidad(reduction);

            Debug.Log($"[Congelado] {owner.Nombre_Entidad} velocidad reducida a 0 por {instance.RemainingTurns} turno/s.");
        }

        public override void OnRemove(EffectInstance instance, Entidad owner)
        {
            // Restaurar velocidad original
            if (instance.RuntimeState.TryGetValue("originalSpeed", out float original))
            {
                int restore = (int)original - owner.Velocidad;
                if (restore != 0)
                    owner.ModificarVelocidad(restore);
            }

            Debug.Log($"[Congelado] {owner.Nombre_Entidad} se descongela.");
        }
    }
}
