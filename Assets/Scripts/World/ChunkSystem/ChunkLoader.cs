using UnityEngine;
using System.Collections.Generic;

namespace World.ChunkSystem
{
    /// <summary>
    /// Componente helper para cargar ChunkDataAssets en el WorldChunkManager al inicio.
    /// Coloca esto en una escena para auto-cargar los chunks configurados.
    /// </summary>
    public class ChunkLoader : MonoBehaviour
    {
        [Header("Chunks a Cargar")]
        [Tooltip("Lista de ChunkDataAssets a registrar al iniciar")]
        [SerializeField] private List<ChunkDataAsset> chunkAssets = new List<ChunkDataAsset>();
        
        [Header("Opciones")]
        [Tooltip("Cargar automáticamente al Start")]
        [SerializeField] private bool loadOnStart = true;
        
        void Start()
        {
            if (loadOnStart)
            {
                LoadAllChunks();
            }
        }
        
        /// <summary>
        /// Carga todos los chunks en el WorldChunkManager.
        /// </summary>
        [ContextMenu("Cargar Todos los Chunks")]
        public void LoadAllChunks()
        {
            if (WorldChunkManager.Instance == null)
            {
                Debug.LogError("❌ WorldChunkManager no encontrado en la escena");
                return;
            }
            
            int loaded = 0;
            
            foreach (var asset in chunkAssets)
            {
                if (asset != null)
                {
                    WorldChunkManager.Instance.RegisterChunk(asset.ToRuntimeData());
                    loaded++;
                }
            }
            
            Debug.Log($"✅ ChunkLoader: {loaded}/{chunkAssets.Count} chunks cargados");
        }
        
        /// <summary>
        /// Encuentra y carga todos los ChunkDataAssets en Resources.
        /// </summary>
        [ContextMenu("Auto-Detectar Chunks en Resources")]
        public void AutoLoadFromResources()
        {
            var assets = Resources.LoadAll<ChunkDataAsset>("World/Chunks");
            chunkAssets.Clear();
            chunkAssets.AddRange(assets);
            
            Debug.Log($"📦 Chunks detectados: {chunkAssets.Count}");
            
            if (Application.isPlaying)
            {
                LoadAllChunks();
            }
        }
    }
}
