using UnityEngine;

namespace Missions.Objectives
{
    /// <summary>
    /// Clase base abstracta para objetivos de misión.
    /// Define QUÉ debe hacer el jugador para completar una misión activa.
    /// Cada objetivo crea su propia instancia runtime (MissionObjectiveInstance)
    /// para rastrear progreso sin mutar el SO.
    /// </summary>
    public abstract class MissionObjectiveSO : ScriptableObject
    {
        [Header("UI")]
        [Tooltip("Descripción manual del objetivo")]
        public string descripcionUI;

        [Tooltip("Icono opcional del objetivo")]
        public Sprite icono;

        [Tooltip("Si este objetivo es opcional para completar la misión")]
        public bool opcional;

        /// <summary>
        /// Genera una descripción automática para UI.
        /// </summary>
        public abstract string GetDescripcionAuto();

        /// <summary>
        /// Descripción final: manual si existe, automática si no.
        /// </summary>
        public string GetDescripcion()
        {
            return string.IsNullOrEmpty(descripcionUI) ? GetDescripcionAuto() : descripcionUI;
        }

        /// <summary>
        /// Crea una instancia runtime para rastrear el progreso de este objetivo.
        /// </summary>
        public abstract MissionObjectiveInstance CrearInstancia();
    }

    /// <summary>
    /// Estado runtime de un objetivo de misión. Mutable por diseño.
    /// NO es un ScriptableObject — vive en memoria durante la partida.
    /// </summary>
    [System.Serializable]
    public abstract class MissionObjectiveInstance
    {
        public bool completado;

        /// <summary>
        /// Progreso normalizado (0 a 1).
        /// </summary>
        public abstract float GetProgreso();

        /// <summary>
        /// Descripción del progreso actual (ej: "3/10 enemigos").
        /// </summary>
        public abstract string GetDescripcionProgreso();
    }
}
