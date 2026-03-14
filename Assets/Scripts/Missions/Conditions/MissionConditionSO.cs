using UnityEngine;

namespace Missions.Conditions
{
    /// <summary>
    /// Clase base abstracta para condiciones de desbloqueo de misiones.
    /// Determina cuándo una misión pasa de "Bloqueada" a "Disponible".
    /// Sigue el mismo patrón que EvolutionConditionSO para consistencia.
    /// </summary>
    public abstract class MissionConditionSO : ScriptableObject
    {
        [Header("UI")]
        [Tooltip("Descripción manual para mostrar en UI")]
        public string descripcionUI;

        [Tooltip("Icono opcional para la condición")]
        public Sprite icono;

        /// <summary>
        /// Evalúa si la condición se cumple dado el estado actual del jugador.
        /// </summary>
        public abstract bool Evaluar(Evolution.EvolutionState state);

        /// <summary>
        /// Progreso normalizado de la condición (0 a 1).
        /// </summary>
        public abstract float GetProgreso(Evolution.EvolutionState state);

        /// <summary>
        /// Genera una descripción automática si no hay una manual.
        /// </summary>
        public abstract string GetDescripcionAuto();

        /// <summary>
        /// Descripción final: manual si existe, automática si no.
        /// </summary>
        public string GetDescripcion()
        {
            return string.IsNullOrEmpty(descripcionUI) ? GetDescripcionAuto() : descripcionUI;
        }
    }
}
