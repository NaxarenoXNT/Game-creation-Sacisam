using System;
using System.Collections.Generic;
using UnityEngine;
using Missions.Conditions;
using Missions.Objectives;

namespace Missions
{
    /// <summary>
    /// Datos estáticos de una misión. Toda la información de diseño vive aquí.
    /// Es inmutable en runtime — nunca se modifica durante el juego.
    /// </summary>
    [CreateAssetMenu(fileName = "Mision", menuName = "Missions/Mission Definition")]
    public class MissionDefinitionSO : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("ID único de la misión")]
        public string misionId;

        [Tooltip("Nombre para mostrar en UI")]
        public string nombreMostrar;

        [TextArea]
        [Tooltip("Descripción de la misión")]
        public string descripcion;

        public Sprite icono;
        public MissionCategory categoria = MissionCategory.Secundaria;

        [Tooltip("Alcance de la misión: Global (cualquier pj), Personal (un pj específico), Exclusive (global hasta aceptarse)")]
        public MissionScope scope = MissionScope.Global;

        [Header("Condiciones de Desbloqueo")]
        [Tooltip("TODAS deben cumplirse para que la misión pase de Locked a Available (AND lógico)")]
        public List<MissionConditionSO> condicionesDesbloqueo = new List<MissionConditionSO>();

        [Header("Objetivos")]
        [Tooltip("Lista de objetivos. Los no-opcionales deben completarse todos")]
        public List<MissionObjectiveSO> objetivos = new List<MissionObjectiveSO>();

        [Header("Recompensas")]
        public MissionRewards recompensas;

        [Header("Configuración")]
        [Tooltip("Si la misión se acepta automáticamente al estar disponible")]
        public bool autoAceptar;

        [Tooltip("Si la misión es repetible después de completarse")]
        public bool repetible;

        [Tooltip("Si es visible en el diario antes de estar disponible")]
        public bool visibleBloqueada;

        [Tooltip("Pista para misiones bloqueadas/ocultas")]
        [TextArea]
        public string hintBloqueada;

        /// <summary>
        /// Evalúa si TODAS las condiciones de desbloqueo se cumplen.
        /// </summary>
        public bool CumpleCondicionesDesbloqueo(Evolution.EvolutionState state)
        {
            foreach (var cond in condicionesDesbloqueo)
            {
                if (cond != null && !cond.Evaluar(state))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Progreso promedio de las condiciones de desbloqueo (0 a 1).
        /// </summary>
        public float GetProgresoDesbloqueo(Evolution.EvolutionState state)
        {
            if (condicionesDesbloqueo.Count == 0) return 1f;

            float total = 0f;
            int count = 0;
            foreach (var cond in condicionesDesbloqueo)
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
        /// Obtiene las descripciones de todas las condiciones para UI.
        /// </summary>
        public List<string> GetDescripcionesCondiciones()
        {
            var descripciones = new List<string>();
            foreach (var cond in condicionesDesbloqueo)
            {
                if (cond != null)
                    descripciones.Add(cond.GetDescripcion());
            }
            return descripciones;
        }
    }

    /// <summary>
    /// Recompensas otorgadas al completar una misión.
    /// </summary>
    [Serializable]
    public struct MissionRewards
    {
        [Tooltip("XP otorgada al completar")]
        public int xp;

        [Tooltip("Oro otorgado al completar")]
        public int oro;

        [Tooltip("Delta de karma al completar (-1 a 1)")]
        [Range(-1f, 1f)]
        public float karmaDelta;

        [Tooltip("IDs de items otorgados como recompensa")]
        public List<RewardItem> items;

        [Tooltip("Flags que se activan al completar")]
        public List<string> flagsActivar;
    }

    [Serializable]
    public struct RewardItem
    {
        public string itemId;
        public int cantidad;
    }
}
