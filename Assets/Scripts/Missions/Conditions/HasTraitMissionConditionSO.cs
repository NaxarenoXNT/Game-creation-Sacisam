using UnityEngine;
using Evolution;

namespace Missions.Conditions
{
    /// <summary>
    /// Condición de misión: el jugador debe poseer un Trait específico.
    /// </summary>
    [CreateAssetMenu(fileName = "MissCond_Trait", menuName = "Missions/Conditions/Tiene Trait")]
    public class HasTraitMissionConditionSO : MissionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Referencia directa al trait requerido")]
        public TraitDefinition traitRequerido;

        [Tooltip("ID del trait (fallback si no hay referencia directa)")]
        public string traitId;

        [Tooltip("Cantidad mínima de stacks requeridos")]
        public int stacksMinimos = 1;

        private string GetTraitId()
        {
            return traitRequerido != null ? traitRequerido.id : traitId;
        }

        public override bool Evaluar(EvolutionState state)
        {
            string id = GetTraitId();
            if (string.IsNullOrEmpty(id)) return false;
            return state.traitStacks.TryGetValue(id, out int stacks) && stacks >= stacksMinimos;
        }

        public override float GetProgreso(EvolutionState state)
        {
            string id = GetTraitId();
            if (string.IsNullOrEmpty(id)) return 0f;
            state.traitStacks.TryGetValue(id, out int stacks);
            return stacksMinimos > 0 ? Mathf.Clamp01((float)stacks / stacksMinimos) : 1f;
        }

        public override string GetDescripcionAuto()
        {
            string nombre = traitRequerido != null ? traitRequerido.nombreMostrar : traitId;
            return stacksMinimos > 1
                ? $"Requiere trait: {nombre} (x{stacksMinimos})"
                : $"Requiere trait: {nombre}";
        }
    }
}
