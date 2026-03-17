using UnityEngine;
using System.Collections.Generic;

namespace World.ChunkSystem
{
    /// <summary>
    /// Componente helper para registrar ChunkDataAssets específicos en el WorldChunkManager.
    /// Útil cuando querés cargar chunks puntuales asignados desde el Inspector,
    /// en lugar de depender únicamente del auto-load de Resources/World/Chunks/.
    /// 
    /// NOTA: WorldChunkManager ya carga automáticamente todos los ChunkDataAssets
    /// desde Resources/World/Chunks/ en su Start(). Usá este componente solo
    /// para chunks adicionales que no estén en esa carpeta.
    /// </summary>
    public class ChunkLoader : MonoBehaviour
    {
        [Header("Chunks a Cargar")]
        [Tooltip("Lista de ChunkDataAssets adicionales a registrar al iniciar.\n" +
                 "Los chunks en Resources/World/Chunks/ se cargan automáticamente por WorldChunkManager.")]
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
        /// Registra los chunks asignados en el Inspector en el WorldChunkManager.
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
    }
}
