using UnityEngine;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Curación total realizada.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Curacion", menuName = "Evolutions/Conditions/Curación Total")]
    public class CuracionConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Curación total requerida")]
        public int cantidad = 500;

        public override bool EsEscalable => true;

        public override bool Evaluar(EvolutionState state)
        {
            return state.curacionTotal >= cantidad;
        }

        public override float GetProgreso(EvolutionState state)
        {
            return cantidad > 0 ? Mathf.Clamp01((float)state.curacionTotal / cantidad) : 1f;
        }

        public override string GetDescripcionAuto()
        {
            return $"Cura {cantidad} puntos de vida total";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            var copia = CreateInstance<CuracionConditionSO>();
            copia.cantidad = Mathf.RoundToInt(cantidad * multiplicador);
            copia.descripcionUI = descripcionUI;
            copia.icono = icono;
            return copia;
        }
    }
}
