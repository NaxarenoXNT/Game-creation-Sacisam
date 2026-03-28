using System.Collections;
using UnityEngine;

namespace World.Travel
{
    /// <summary>
    /// Contrato para el sistema que gestiona el estado del mundo (chunks/streaming)
    /// durante y después de un viaje rápido.
    ///
    /// Abstrae WorldChunkManager para que TravelManager no dependa directamente
    /// de la implementación concreta del sistema de chunks.
    /// </summary>
    public interface ITravelChunkHandler
    {
        /// <summary>
        /// Prepara el mundo para el viaje: descarga los chunks activos actuales.
        /// Se llama justo antes de mover al jugador, durante el fade out.
        /// </summary>
        void PrepareForTravel(Vector3 destination);

        /// <summary>
        /// Fuerza la actualización del sistema de chunks desde la posición de destino,
        /// luego espera (hasta <paramref name="timeout"/> segundos) a que los chunks
        /// mínimos necesarios estén cargados.
        /// Debe llamarse <i>después</i> de mover al jugador al destino.
        /// </summary>
        IEnumerator WaitForChunksLoaded(Vector3 destination, float timeout);
    }
}
