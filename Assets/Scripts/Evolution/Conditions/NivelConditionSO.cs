using UnityEngine;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Nivel mínimo del jugador.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Nivel", menuName = "Evolutions/Conditions/Nivel Minimo")]
    public class NivelConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Nivel mínimo requerido")]
        public int nivelMinimo = 10;

        public override bool EsEscalable => true;

        public override bool Evaluar(EvolutionState state)
        {
            return state.nivelJugador >= nivelMinimo;
        }

        public override float GetProgreso(EvolutionState state)
        {
            return nivelMinimo > 0 ? Mathf.Clamp01((float)state.nivelJugador / nivelMinimo) : 1f;
        }

        public override string GetDescripcionAuto()
        {
            return $"Alcanza nivel {nivelMinimo}";
        }

        public override EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            var copia = CreateInstance<NivelConditionSO>();
            copia.nivelMinimo = Mathf.RoundToInt(nivelMinimo * multiplicador);
            copia.descripcionUI = descripcionUI;
            copia.icono = icono;
            return copia;
        }
    }
}
