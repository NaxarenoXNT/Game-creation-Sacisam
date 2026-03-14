using UnityEngine;
using Evolution;

namespace Missions.Conditions
{
    /// <summary>
    /// Condición de misión: el karma del jugador debe estar en un rango específico.
    /// </summary>
    [CreateAssetMenu(fileName = "MissCond_Karma", menuName = "Missions/Conditions/Rango Karma")]
    public class KarmaMissionConditionSO : MissionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Karma mínimo requerido (-1 a 1)")]
        [Range(-1f, 1f)]
        public float karmaMinimo = -1f;

        [Tooltip("Karma máximo permitido (-1 a 1)")]
        [Range(-1f, 1f)]
        public float karmaMaximo = 1f;

        public override bool Evaluar(EvolutionState state)
        {
            return state.karma >= karmaMinimo && state.karma <= karmaMaximo;
        }

        public override float GetProgreso(EvolutionState state)
        {
            return Evaluar(state) ? 1f : 0f;
        }

        public override string GetDescripcionAuto()
        {
            if (karmaMinimo <= -1f)
                return $"Karma ≤ {karmaMaximo:F1}";
            if (karmaMaximo >= 1f)
                return $"Karma ≥ {karmaMinimo:F1}";
            return $"Karma entre {karmaMinimo:F1} y {karmaMaximo:F1}";
        }
    }
}
