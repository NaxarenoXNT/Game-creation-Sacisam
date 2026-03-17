using System.Collections.Generic;
using UnityEngine;

namespace World.ChunkSystem
{
    /// <summary>
    /// Gestiona la instanciación y destrucción de props con identidad en chunks.
    /// Props con identidad: edificios, cofres, NPCs, entradas a zonas, etc.
    /// </summary>
    public class ChunkPropsManager
    {
        /// <summary>
        /// Crea el GameObject contenedor de props para un chunk si no existe.
        /// </summary>
        public void EnsurePropsRoot(ChunkData chunk)
        {
            if (chunk.propsRoot != null) return;
            
            var root = new GameObject($"Props_{chunk.coordinates.x}_{chunk.coordinates.y}");
            chunk.propsRoot = root.transform;
        }
        
        /// <summary>
        /// Instancia los props con identidad del chunk (edificios, cofres, NPCs, etc.).
        /// </summary>
        public void SpawnNamedProps(ChunkData chunk, bool showDebugLogs)
        {
            var spawnableProps = chunk.GetSpawnableProps();
            if (spawnableProps.Count == 0) return;
            
            foreach (var config in spawnableProps)
            {
                if (config.propData == null)
                {
                    Debug.LogWarning($"⚠️ PropSpawnConfig '{config.propId}' no tiene PropData asignado.");
                    continue;
                }
                
                if (config.propData.prefab == null)
                {
                    Debug.LogWarning($"⚠️ PropData '{config.propData.propName}' no tiene prefab asignado.");
                    continue;
                }
                
                var go = Object.Instantiate(
                    config.propData.prefab,
                    config.position,
                    config.rotation,
                    chunk.propsRoot
                );
                go.transform.localScale = config.scale;
                
                // Si es interactivo, inicializar el PropController
                if (config.propData.isInteractive)
                {
                    var controller = go.GetComponent<PropController>();
                    if (controller != null)
                    {
                        controller.Initialize(config, chunk.coordinates);
                        config.activeController = controller;
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Prop '{config.propId}' marcado como interactivo pero " +
                                         $"el prefab no tiene PropController.");
                    }
                }
            }
            
            if (showDebugLogs)
                Debug.Log($"🏠 Chunk {chunk.coordinates}: {spawnableProps.Count} props instanciados.");
        }
        
        /// <summary>
        /// Destruye todos los props del chunk de una sola vez.
        /// </summary>
        public void UnloadProps(ChunkData chunk)
        {
            if (chunk.propsRoot != null)
            {
                Object.Destroy(chunk.propsRoot.gameObject);
                chunk.propsRoot = null;
            }
            
            // Limpiar referencias de controladores en los configs
            foreach (var config in chunk.propSpawnConfigs)
                config.activeController = null;
        }
    }
}
