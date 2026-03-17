using System.Collections.Generic;
using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Gestiona la carga y descarga dinámica de TerrainData para chunks.
    /// Instancia Terrain GameObjects en runtime desde Resources/World/TerrainData/.
    /// </summary>
    public class ChunkTerrainLoader
    {
        // Chunks sin TerrainData registrados, para no repetir warning en consola
        private readonly HashSet<Vector2Int> noTerrainChunks = new HashSet<Vector2Int>();

        /// <summary>
        /// Carga el TerrainData del chunk desde Resources e instancia el Terrain en el mundo.
        /// Ruta esperada: Resources/World/TerrainData/Chunk_X_Y_Terrain.asset
        /// </summary>
        public void LoadTerrainForChunk(ChunkData chunk, float chunkSize, bool showDebugLogs)
        {
            if (chunk.terrainInstance != null) return;
            
            string resourcePath = $"World/TerrainData/Chunk_{chunk.coordinates.x}_{chunk.coordinates.y}_Terrain";
            var tData = Resources.Load<TerrainData>(resourcePath);
            
            if (tData == null)
            {
                // No hay terreno para este chunk — es válido (chunk fuera del área generada)
                // Solo loguear una vez por coordenada para evitar spam en consola
                if (showDebugLogs && noTerrainChunks.Add(chunk.coordinates))
                    Debug.Log($"ℹ️ Sin TerrainData para chunk {chunk.coordinates} — buscado en Resources/{resourcePath}");
                return;
            }
            
            var terrainGO = Terrain.CreateTerrainGameObject(tData);
            terrainGO.transform.position = new Vector3(
                chunk.coordinates.x * chunkSize, 0f, chunk.coordinates.y * chunkSize);
            terrainGO.name = $"Terrain_{chunk.coordinates.x}_{chunk.coordinates.y}";
            chunk.terrainInstance = terrainGO;
            
            if (showDebugLogs)
                Debug.Log($"🏔️ Terreno cargado: {terrainGO.name}");
        }
        
        /// <summary>
        /// Destruye el Terrain GameObject del chunk.
        /// </summary>
        public void UnloadTerrainForChunk(ChunkData chunk, bool showDebugLogs)
        {
            if (chunk.terrainInstance == null) return;
            
            if (showDebugLogs)
                Debug.Log($"🏔️ Terreno descargado: {chunk.terrainInstance.name}");
            
            Object.Destroy(chunk.terrainInstance);
            chunk.terrainInstance = null;
        }
    }
}
