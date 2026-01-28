using UnityEngine;

namespace Evolution.Conditions
{
    /// <summary>
    /// Condición: Misión completada.
    /// </summary>
    [CreateAssetMenu(fileName = "Cond_Mision", menuName = "Evolutions/Conditions/Mision Completada")]
    public class MisionConditionSO : EvolutionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("ID de la misión requerida")]
        public string misionId;
        
        // TODO: Cuando crees MisionData SO, agregar:
        // public MisionData misionRequerida;

        public override bool EsEscalable => false;

        public override bool Evaluar(EvolutionState state)
        {
            return !string.IsNullOrEmpty(misionId) && state.misionesCompletadas.Contains(misionId);
        }

        public override float GetProgreso(EvolutionState state)
        {
            return Evaluar(state) ? 1f : 0f;
        }

        public override string GetDescripcionAuto()
        {
            return $"Completa misión: {misionId}";
        }
    }
}
