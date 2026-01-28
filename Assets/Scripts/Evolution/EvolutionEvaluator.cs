using System.Collections.Generic;
using System.Linq;

namespace Evolution
{
    /// <summary>
    /// Evalúa condiciones genéricas y determina qué traits/evoluciones están disponibles.
    /// El sistema funciona así:
    /// - TRAITS se desbloquean cumpliendo condiciones genéricas (kills, misiones, karma, etc.)
    /// - EVOLUCIONES se desbloquean teniendo los TRAITS requeridos
    /// </summary>
    public class EvolutionEvaluator
    {
        /// <summary>
        /// Filtra evoluciones disponibles basándose en:
        /// 1. Clase origen correcta
        /// 2. Nivel mínimo
        /// 3. Tiene TODOS los traits requeridos
        /// 4. No está excluida por otra evolución ya aplicada
        /// </summary>
        public List<ClassEvolutionDefinition> FiltrarEvolucionesDisponibles(
            IEnumerable<ClassEvolutionDefinition> defs,
            EvolutionState state,
            ClaseData claseActual)
        {
            return defs.Where(def => 
                CumpleClaseOrigen(def, claseActual) &&
                CumpleNivel(def.nivelMin, state.nivelJugador) &&
                TieneTodosLosTraits(def.traitsRequeridos, state) &&
                !EsEvolucionExcluida(def, state)
            ).ToList();
        }

        /// <summary>
        /// Filtra traits disponibles basándose en:
        /// 1. La clase actual no está bloqueada
        /// 2. Cumple TODAS las condiciones genéricas
        /// 3. No está excluido por otro trait
        /// 4. No supera el máximo de stacks
        /// </summary>
        public List<TraitDefinition> FiltrarTraitsDisponibles(
            IEnumerable<TraitDefinition> defs,
            EvolutionState state,
            ClaseData claseActual)
        {
            return defs.Where(def =>
                !ClaseBloqueada(def, claseActual) &&
                CumpleTodasLasCondiciones(def.condiciones, state) &&
                !EsTraitExcluido(def, state) &&
                NoSuperaStacks(def, state)
            ).ToList();
        }

        #region Evaluadores de Evolución

        private bool CumpleClaseOrigen(ClassEvolutionDefinition def, ClaseData claseActual)
        {
            // Si no tiene clase origen definida, aplica a todas
            if (def.claseOrigen == null) return true;
            // Compara por referencia de SO o por nombre
            return claseActual != null && def.claseOrigen == claseActual;
        }

        private bool CumpleNivel(int nivelMin, int nivelActual)
            => nivelActual >= nivelMin;

        private bool TieneTodosLosTraits(List<TraitDefinition> traitsRequeridos, EvolutionState state)
        {
            if (traitsRequeridos == null || traitsRequeridos.Count == 0) return true;
            return traitsRequeridos.All(t => state.traitStacks.ContainsKey(t.id));
        }

        private bool EsEvolucionExcluida(ClassEvolutionDefinition def, EvolutionState state)
        {
            if (def.exclusiones == null || def.exclusiones.Count == 0) return false;
            return def.exclusiones.Any(excl => state.evolucionesAplicadas.Contains(excl.id));
        }

        #endregion

        #region Evaluadores de Trait

        private bool ClaseBloqueada(TraitDefinition def, ClaseData claseActual)
        {
            if (def.clasesBloqueadas == null || def.clasesBloqueadas.Count == 0) return false;
            return claseActual != null && def.clasesBloqueadas.Contains(claseActual);
        }

        private bool EsTraitExcluido(TraitDefinition def, EvolutionState state)
        {
            if (def.exclusiones == null || def.exclusiones.Count == 0) return false;
            return def.exclusiones.Any(excl => state.traitStacks.ContainsKey(excl.id));
        }

        private bool NoSuperaStacks(TraitDefinition def, EvolutionState state)
        {
            if (!state.traitStacks.TryGetValue(def.id, out int stacks)) return true; // No tiene, puede obtener
            if (!def.stackeable) return false; // Ya lo tiene y no es stackeable
            return stacks < def.maxStacks;
        }

        #endregion

        #region Evaluador de Condiciones Genéricas

        /// <summary>
        /// Evalúa una lista de condiciones SO contra el estado actual.
        /// TODAS las condiciones deben cumplirse (AND lógico).
        /// </summary>
        public bool CumpleTodasLasCondiciones(List<EvolutionConditionSO> condiciones, EvolutionState state)
        {
            if (condiciones == null || condiciones.Count == 0) return true;
            return condiciones.All(c => c == null || c.Evaluar(state));
        }

        /// <summary>
        /// Obtiene el progreso promedio de una lista de condiciones.
        /// </summary>
        public float ObtenerProgresoCondiciones(List<EvolutionConditionSO> condiciones, EvolutionState state)
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

        #endregion
    }
}
