using UnityEngine;
using Evolution;

namespace Missions.Conditions
{
    /// <summary>
    /// Condición de misión: otra misión debe estar completada.
    /// </summary>
    [CreateAssetMenu(fileName = "MissCond_Mision", menuName = "Missions/Conditions/Misión Completada")]
    public class MissionCompletedConditionSO : MissionConditionSO
    {
        [Header("Configuración")]
        [Tooltip("Referencia directa a la misión requerida")]
        public MissionDefinitionSO misionRequerida;

        [Tooltip("ID de la misión (fallback si no hay referencia directa)")]
        public string misionId;

        private string GetMisionId()
        {
            return misionRequerida != null ? misionRequerida.misionId : misionId;
        }

        public override bool Evaluar(EvolutionState state)
        {
            string id = GetMisionId();
            return !string.IsNullOrEmpty(id) && state.misionesCompletadas.Contains(id);
        }

        public override float GetProgreso(EvolutionState state)
        {
            return Evaluar(state) ? 1f : 0f;
        }

        public override string GetDescripcionAuto()
        {
            string nombre = misionRequerida != null ? misionRequerida.nombreMostrar : misionId;
            return $"Completa misión: {nombre}";
        }
    }
}
