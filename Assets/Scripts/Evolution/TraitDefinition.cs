using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolution
{
    /// <summary>
    /// Trait: se desbloquea cumpliendo condiciones genéricas.
    /// Los traits desbloquean evoluciones de clase.
    /// </summary>
    [CreateAssetMenu(fileName = "Trait", menuName = "Evolutions/Trait")]
    public class TraitDefinition : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("ID único del trait")]
        public string id;
        
        [Tooltip("Nombre para mostrar en UI")]
        public string nombreMostrar;
        
        [TextArea]
        [Tooltip("Descripción del trait")]
        public string descripcion;
        
        public Sprite icono;
        public EvolutionRarity rareza = EvolutionRarity.Common;
        public float pesoOferta = 1f;
        
        [Tooltip("Si es visible u oculto hasta cumplir condiciones")]
        public bool visible = true;
        
        [TextArea]
        [Tooltip("Pista para traits ocultos")]
        public string hintOculto;

        [Header("Restricciones de Clase")]
        [Tooltip("Clases que NO pueden obtener este trait (dejar vacío = todas pueden)")]
        public List<ClaseData> clasesBloqueadas = new List<ClaseData>();
        
        [Tooltip("Si se puede obtener múltiples veces")]
        public bool stackeable;
        
        [Tooltip("Máximo de stacks si es stackeable")]
        public int maxStacks = 1;

        [Header("Condiciones de Desbloqueo")]
        [Tooltip("Lista de condiciones que deben cumplirse para desbloquear este trait (referencias a SOs)")]
        public List<EvolutionConditionSO> condiciones = new List<EvolutionConditionSO>();
        
        [Tooltip("Otros traits que excluyen este (no se puede tener ambos)")]
        public List<TraitDefinition> exclusiones = new List<TraitDefinition>();

        [Header("Efectos al Obtener")]
        [Tooltip("Efectos que se aplican cuando obtienes este trait")]
        public List<EvolutionEffect> efectos = new List<EvolutionEffect>();
        
        /// <summary>
        /// Evalúa si todas las condiciones del trait se cumplen.
        /// </summary>
        public bool CumpleCondiciones(EvolutionState state)
        {
            foreach (var cond in condiciones)
            {
                if (cond != null && !cond.Evaluar(state))
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// Obtiene el progreso promedio de todas las condiciones.
        /// </summary>
        public float GetProgreso(EvolutionState state)
        {
            if (condiciones.Count == 0) return 1f;
            
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
        
        /// <summary>
        /// Obtiene las descripciones de todas las condiciones.
        /// </summary>
        public List<string> GetDescripcionesCondiciones()
        {
            var descripciones = new List<string>();
            foreach (var cond in condiciones)
            {
                if (cond != null)
                {
                    descripciones.Add(cond.GetDescripcion());
                }
            }
            return descripciones;
        }
    }
}
