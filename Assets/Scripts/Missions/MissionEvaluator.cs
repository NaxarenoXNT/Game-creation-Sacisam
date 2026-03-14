using System.Collections.Generic;
using System.Linq;
using Missions.Conditions;
using Evolution;

namespace Missions
{
    /// <summary>
    /// Lógica pura de evaluación de misiones.
    /// Determina si condiciones se cumplen sin efectos secundarios.
    /// El MissionManager lo usa internamente para evaluar disponibilidad.
    /// </summary>
    public class MissionEvaluator
    {
        /// <summary>
        /// Evalúa si TODAS las condiciones de una misión se cumplen
        /// contra el EvolutionState de un personaje específico.
        /// </summary>
        public bool CumpleTodasLasCondiciones(
            List<MissionConditionSO> condiciones,
            EvolutionState state)
        {
            if (condiciones == null || condiciones.Count == 0) return true;
            return condiciones.All(c => c == null || c.Evaluar(state));
        }

        /// <summary>
        /// Evalúa si ALGÚN personaje de un conjunto cumple TODAS las condiciones.
        /// Usado para misiones Global/Exclusive.
        /// </summary>
        public bool AlgunPersonajeCumple(
            List<MissionConditionSO> condiciones,
            IEnumerable<EvolutionState> estados)
        {
            if (condiciones == null || condiciones.Count == 0) return true;
            return estados.Any(state => CumpleTodasLasCondiciones(condiciones, state));
        }

        /// <summary>
        /// Progreso promedio de una lista de condiciones (0 a 1).
        /// </summary>
        public float ObtenerProgresoCondiciones(
            List<MissionConditionSO> condiciones,
            EvolutionState state)
        {
            if (condiciones == null || condiciones.Count == 0) return 1f;

            float total = 0f;
            int count = 0;
            foreach (var cond in condiciones)
            {
                if (cond != null)
                {
                    total += cond.GetProgreso(state);
                    count++;
                }
            }
            return count > 0 ? total / count : 1f;
        }
    }
}
