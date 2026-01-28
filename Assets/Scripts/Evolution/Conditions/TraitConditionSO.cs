using UnityEngine;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Tener un trait específico.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Trait", menuName = "Evolutions/Conditions/Tiene Trait")]
    public class TraitConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Trait requerido")]
        public TraitDefinition traitRequerido;
        
        [Tooltip("ID del trait (usado si no hay referencia directa)")]
        public string traitId;

        public override bool EsEscalable => false;

        private string GetTraitId()
        {
            return traitRequerido != null ? traitRequerido.id : traitId;
        }

        public override bool Evaluar(EvolutionState state)
        {
            string id = GetTraitId();
            return !string.IsNullOrEmpty(id) && state.traitStacks.ContainsKey(id);
        }

        public override float GetProgreso(EvolutionState state)
        {
            return Evaluar(state) ? 1f : 0f;
        }

        public override string GetDescripcionAuto()
        {
            string nombre = traitRequerido != null ? traitRequerido.nombreMostrar : traitId;
            return $"Requiere trait: {nombre}";
        }
    }
}
