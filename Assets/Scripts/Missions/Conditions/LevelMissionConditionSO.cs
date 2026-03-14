using UnityEngine;
using Evolution;

namespace Missions.Conditions
{
    /// <summary>
    /// Condición de misión: el jugador debe tener un nivel mínimo.
    /// </summary>
    [CreateAssetMenu(fileName = "MissCond_Nivel", menuName = "Missions/Conditions/Nivel Mínimo")]
    public class LevelMissionConditionSO : MissionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Nivel mínimo requerido")]
        public int nivelMinimo = 1;

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
            return $"Nivel mínimo: {nivelMinimo}";
        }
    }
}
