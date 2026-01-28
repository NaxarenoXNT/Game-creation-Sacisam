using UnityEngine;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Daño total infligido.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_DanoInfligido", menuName = "Evolutions/Conditions/Daño Infligido")]
    public class DanoInfligidoConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Daño total requerido")]
        public int cantidad = 1000;

        public override bool EsEscalable => true;

        public override bool Evaluar(EvolutionState state)
        {
            return state.dañoInfligidoTotal >= cantidad;
        }

        public override float GetProgreso(EvolutionState state)
        {
            return cantidad > 0 ? Mathf.Clamp01((float)state.dañoInfligidoTotal / cantidad) : 1f;
        }

        public override string GetDescripcionAuto()
        {
            return $"Inflige {cantidad} de daño total";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            var copia = CreateInstance<DanoInfligidoConditionSO>();
            copia.cantidad = Mathf.RoundToInt(cantidad * multiplicador);
            copia.descripcionUI = descripcionUI;
            copia.icono = icono;
            return copia;
        }
    }
}
