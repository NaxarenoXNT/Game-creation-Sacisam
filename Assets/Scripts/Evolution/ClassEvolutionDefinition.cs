using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evolution
{
    /// <summary>
    /// Evolución de clase: transforma al jugador a una nueva clase.
    /// Las evoluciones requieren TRAITS para desbloquearse, no condiciones directas.
    /// Así los traits actúan como "logros" que habilitan opciones de evolución.
    /// </summary>
    [CreateAssetMenu(fileName = "ClassEvolution", menuName = "Evolutions/ClassEvolution")]
    public class ClassEvolutionDefinition : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("ID único de la evolución")]
        public string id;
        
        [Tooltip("Nombre para mostrar en UI")]
        public string nombreMostrar;
        
        [TextArea]
        [Tooltip("Descripción de la evolución")]
        public string descripcion;
        
        public Sprite icono;

        [Header("Clase")]
        [Tooltip("Clase requerida para esta evolución")]
        public ClaseData claseOrigen;
        
        [Tooltip("Clase a la que evoluciona")]
        public ClaseData claseDestino;
        
        [Tooltip("Tier de la evolución (1=básica, 2=avanzada, 3=legendaria)")]
        public int tier = 1;

        [Header("Presentación")]
        public EvolutionRarity rareza = EvolutionRarity.Common;
        public float pesoOferta = 1f;
        
        [Tooltip("Si es visible u oculta hasta cumplir requisitos")]
        public bool visible = true;
        
        [TextArea]
        [Tooltip("Pista para evoluciones ocultas")]
        public string hintOculto;

        [Header("Requisitos de Traits")]
        [Tooltip("Traits que DEBE tener el jugador para desbloquear esta evolución")]
        public List<TraitDefinition> traitsRequeridos = new List<TraitDefinition>();
        
        [Tooltip("Nivel mínimo (único requisito directo permitido, es universal)")]
        public int nivelMin = 1;

        [Header("Exclusiones")]
        [Tooltip("Otras evoluciones que excluyen esta (no se puede tener ambas)")]
        public List<ClassEvolutionDefinition> exclusiones = new List<ClassEvolutionDefinition>();

        [Header("Efectos al Evolucionar")]
        [Tooltip("Efectos que se aplican cuando el jugador evoluciona")]
        public List<EvolutionEffect> efectos = new List<EvolutionEffect>();
    }

    public enum EvolutionRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }
}
