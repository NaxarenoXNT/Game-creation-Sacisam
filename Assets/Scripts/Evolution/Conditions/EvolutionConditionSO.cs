using UnityEngine;

namespace Evolution
{
    /// <summary>
    /// Clase base abstracta para todas las condiciones de evolución.
    /// Cada tipo de condición debe heredar de esta clase y crear su propio SO.
    /// </summary>
    public abstract class EvolutionConditionSO : ScriptableObject
    {
        [Header("UI")]
        [Tooltip("Descripción para mostrar en UI")]
        public string descripcionUI;
        
        [Tooltip("Icono opcional para la condición")]
        public Sprite icono;

        /// <summary>
        /// Evalúa si la condición se cumple dado el estado actual.
        /// </summary>
        public abstract bool Evaluar(EvolutionState state);

        /// <summary>
        /// Obtiene el progreso actual de la condición (0 a 1).
        /// </summary>
        public abstract float GetProgreso(EvolutionState state);

        /// <summary>
        /// Genera descripción automática si no hay una manual.
        /// </summary>
        public abstract string GetDescripcionAuto();

        /// <summary>
        /// Descripción final para UI (usa manual si existe, si no genera automática).
        /// </summary>
        public string GetDescripcion()
        {
            return string.IsNullOrEmpty(descripcionUI) ? GetDescripcionAuto() : descripcionUI;
        }

        /// <summary>
        /// Crea una copia escalada de esta condición (para multiplicadores en cadenas).
        /// Por defecto retorna la misma referencia. Override si la condición tiene cantidad.
        /// </summary>
        public virtual EvolutionConditionSO CrearCopiaEscalada(float multiplicador)
        {
            // Por defecto, las condiciones no se escalan (ej: karma, trait requerido)
            return this;
        }

        /// <summary>
        /// Indica si esta condición puede ser escalada por multiplicador.
        /// </summary>
        public virtual bool EsEscalable => false;
    }
}
