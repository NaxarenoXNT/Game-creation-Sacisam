using UnityEngine;
using System.Collections.Generic;

namespace World.ChunkSystem
{
    /// <summary>
    /// ScriptableObject para diseñar chunks en el editor.
    /// Permite crear y configurar chunks con enemigos de forma visual.
    /// </summary>
    [CreateAssetMenu(fileName = "Chunk", menuName = "World/Chunk Data", order = 1)]
    public class ChunkDataAsset : ScriptableObject
    {
        [Header("Identificación")]
        [Tooltip("Coordenadas del chunk en el grid")]
        public Vector2Int coordinates;
        
        [Header("Configuración de Enemigos")]
        [Tooltip("Lista de enemigos a spawnear en este chunk")]
        public List<EnemySpawnConfig> enemySpawns = new List<EnemySpawnConfig>();
        
        [Header("Props con Identidad")]
        [Tooltip("Objetos con posición fija: edificios, cofres, NPCs, entradas a zonas.")]
        public List<PropSpawnConfig> propSpawnConfigs = new List<PropSpawnConfig>();
        
        [Header("Exclusiones Procedurales")]
        [Tooltip("Zonas donde no se genera vegetación procedural: caminos, plazas, footprints.")]
        public List<ProceduralExclusion> proceduralExclusions = new List<ProceduralExclusion>();
        
        [Header("Preview")]
        [Tooltip("Color para visualizar este chunk en la escena")]
        public Color gizmoColor = Color.cyan;
        
        /// <summary>
        /// Convierte el asset en ChunkData runtime.
        /// </summary>
        public ChunkData ToRuntimeData()
        {
            return new ChunkData
            {
                coordinates = coordinates,
                chunkId = $"chunk_{coordinates.x}_{coordinates.y}",
                enemySpawnConfigs = new List<EnemySpawnConfig>(enemySpawns),
                propSpawnConfigs = new List<PropSpawnConfig>(propSpawnConfigs),
                proceduralExclusions = new List<ProceduralExclusion>(proceduralExclusions)
            };
        }
        
        /// <summary>
        /// Registra este chunk en el WorldChunkManager.
        /// </summary>
        [ContextMenu("Registrar en Manager")]
        public void RegisterInManager()
        {
            if (WorldChunkManager.Instance != null)
            {
                WorldChunkManager.Instance.RegisterChunk(ToRuntimeData());
                Debug.Log($"✅ Chunk {coordinates} registrado en WorldChunkManager");
            }
            else
            {
                Debug.LogError("❌ WorldChunkManager no encontrado en la escena");
            }
        }
        
        void OnValidate()
        {
            // Auto-generar IDs únicos para spawns sin ID
            for (int i = 0; i < enemySpawns.Count; i++)
            {
                if (string.IsNullOrEmpty(enemySpawns[i].spawnId))
                {
                    enemySpawns[i].spawnId = $"{name}_spawn_{i}";
                }
                
                // VALIDAR Y CORREGIR ROTACIÓN INVÁLIDA
                enemySpawns[i].ValidateRotation();
            }
            
            // Auto-generar IDs únicos para props sin ID
            for (int i = 0; i < propSpawnConfigs.Count; i++)
            {
                if (string.IsNullOrEmpty(propSpawnConfigs[i].propId))
                {
                    propSpawnConfigs[i].propId = $"{name}_prop_{i}";
                }
            }
        }
    }
}
