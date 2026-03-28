using System.Collections;
using UnityEngine;
using World.ChunkSystem;

namespace World.Travel
{
    /// <summary>
    /// Implementación por defecto de ITravelChunkHandler.
    /// Coordina el WorldChunkManager durante un viaje rápido:
    ///   1. Descarga todos los chunks activos (PrepareForTravel).
    ///   2. Fuerza la carga de chunks en el destino.
    ///   3. Espera hasta que el chunk central esté marcado como cargado (o timeout).
    /// </summary>
    public class TravelChunkHandler : ITravelChunkHandler
    {
        public void PrepareForTravel(Vector3 destination)
        {
            var manager = WorldChunkManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[TravelChunkHandler] WorldChunkManager no disponible. Se omite la descarga de chunks.");
                return;
            }

            // Descargar todos los chunks cargados actualmente para liberar memoria
            // antes de cargar los del destino.
            manager.ClearAllChunks();

            Debug.Log($"[TravelChunkHandler] Chunks descargados en preparación para viaje a {destination}.");
        }

        public IEnumerator WaitForChunksLoaded(Vector3 destination, float timeout)
        {
            var manager = WorldChunkManager.Instance;
            if (manager == null) yield break;

            // Forzar la actualización del sistema como si el jugador estuviera ya en destino.
            // El Transform del main ya fue movido, pero WorldChunkManager actualiza
            // por su propio intervalo; este método lo dispara inmediatamente.
            manager.ForceUpdateAtPosition(destination);

            // Dar un frame para que los coroutines de spawn/decoración arranquen.
            yield return null;

            Vector2Int targetChunk = manager.WorldToChunkCoords(destination);
            float      elapsed     = 0f;

            // Esperar hasta que el chunk central esté cargado (isLoaded = true).
            while (elapsed < timeout)
            {
                var chunkData = manager.GetChunk(targetChunk);
                if (chunkData != null && chunkData.isLoaded)
                {
                    Debug.Log($"[TravelChunkHandler] Chunk {targetChunk} cargado en {elapsed:F2}s.");
                    yield break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Debug.LogWarning($"[TravelChunkHandler] Timeout ({timeout}s) esperando chunk {targetChunk} en {destination}. " +
                             $"El viaje continuará de todas formas.");
        }
    }
}
