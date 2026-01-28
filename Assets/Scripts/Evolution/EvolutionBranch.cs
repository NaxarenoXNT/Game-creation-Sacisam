using System.Collections.Generic;
using UnityEngine;

namespace Evolution
{
    /// <summary>
    /// Agrupa todas las evoluciones disponibles para una clase base.
    /// Útil para organizar el árbol de evoluciones y para queries rápidas.
    /// </summary>
    [CreateAssetMenu(fileName = "EvolutionBranch", menuName = "Evolutions/EvolutionBranch")]
    public class EvolutionBranch : ScriptableObject
    {
        [Header("Clase Base")]
        [Tooltip("La clase de origen para este árbol de evoluciones")]
        public ClaseData claseOrigen;

        [Header("Evoluciones Disponibles")]
        [Tooltip("Todas las evoluciones posibles para esta clase")]
        public List<ClassEvolutionDefinition> evoluciones = new List<ClassEvolutionDefinition>();
        
        [Header("Traits Relacionados")]
        [Tooltip("Traits temáticos o relacionados con esta clase")]
        public List<TraitDefinition> traitsRelacionados = new List<TraitDefinition>();
    }
}
