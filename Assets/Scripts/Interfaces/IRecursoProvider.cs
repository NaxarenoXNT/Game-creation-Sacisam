using Flags;
using System;

namespace Interfaces
{
    /// <summary>
    /// Interfaz para entidades que poseen recursos consumibles (Mana, Energía, Sangre, etc.).
    /// Permite que el sistema de habilidades verifique y consuma recursos de forma genérica.
    /// </summary>
    public interface IRecursoProvider
    {
        /// <summary>
        /// Obtiene la cantidad actual de un recurso.
        /// Retorna 0 si la entidad no posee ese tipo de recurso.
        /// </summary>
        float ObtenerRecursoActual(TipoRecurso tipo);

        /// <summary>
        /// Obtiene la cantidad máxima de un recurso.
        /// Retorna 0 si la entidad no posee ese tipo de recurso.
        /// </summary>
        float ObtenerRecursoMaximo(TipoRecurso tipo);

        /// <summary>
        /// Verifica si la entidad tiene suficiente recurso.
        /// </summary>
        bool TieneRecursoSuficiente(TipoRecurso tipo, float cantidad);

        /// <summary>
        /// Consume una cantidad del recurso especificado.
        /// Retorna true si se pudo consumir, false si no había suficiente.
        /// </summary>
        bool ConsumirRecurso(TipoRecurso tipo, float cantidad);

        /// <summary>
        /// Restaura una cantidad del recurso especificado.
        /// </summary>
        void RestaurarRecurso(TipoRecurso tipo, float cantidad);

        /// <summary>
        /// Verifica si la entidad posee un tipo de recurso.
        /// </summary>
        bool PoseeRecurso(TipoRecurso tipo);

        /// <summary>
        /// Evento disparado cuando un recurso cambia.
        /// Parámetros: (TipoRecurso tipo, float actual, float maximo)
        /// </summary>
        event Action<TipoRecurso, float, float> OnRecursoCambiado;
    }
}
