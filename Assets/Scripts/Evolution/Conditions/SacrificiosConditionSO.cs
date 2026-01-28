using UnityEngine;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Cantidad de sacrificios realizados.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Sacrificios", menuName = "Evolutions/Conditions/Sacrificios")]
    public class SacrificiosConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Cantidad de sacrificios requeridos")]
        public int cantidad = 10;

        public override bool EsEscalable => true;

        public override bool Evaluar(EvolutionState state)
        {
            return state.sacrificios >= cantidad;
        }

        public override float GetProgreso(EvolutionState state)
        {
            return cantidad > 0 ? Mathf.Clamp01((float)state.sacrificios / cantidad) : 1f;
        }

        public override string GetDescripcionAuto()
        {
            return $"Realiza {cantidad} sacrificios";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            var copia = CreateInstance<SacrificiosConditionSO>();
            copia.cantidad = Mathf.RoundToInt(cantidad * multiplicador);
            copia.descripcionUI = descripcionUI;
            copia.icono = icono;
            return copia;
        }
    }
}
