using System.Collections.Generic;
using UnityEngine;
using Console.Context;
using Managers;
using World.ChunkSystem;

namespace Console.Adapters
{
    /// <summary>
    /// Adapter que conecta IEnemySpawner con los sistemas reales del juego:
    /// - DynamicEnemyPoolManager (obtener controllers)
    /// - WorldChunkManager (tracking de enemigos activos, kill all)
    /// - EnemigoData assets en Resources/EnemigosData/
    /// 
    /// Los enemigos spawneados por consola se posicionan cerca del jugador
    /// y NO participan del sistema de chunks (sin spawnId ni persistencia).
    /// </summary>
    public class EnemySpawnerAdapter : IEnemySpawner
    {
        private readonly Dictionary<string, EnemigoData> _enemyLookup = new Dictionary<string, EnemigoData>();
        private readonly List<EnemyController> _consoleSpawnedEnemies = new List<EnemyController>();

        public EnemySpawnerAdapter()
        {
            LoadEnemyDatabase();
        }

        private void LoadEnemyDatabase()
        {
            var allEnemies = Resources.LoadAll<EnemigoData>("EnemigosData");
            foreach (var data in allEnemies)
            {
                if (data == null || string.IsNullOrEmpty(data.nombreEnemigo))
                    continue;

                string key = data.nombreEnemigo.ToLowerInvariant().Replace(" ", "_");
                if (!_enemyLookup.ContainsKey(key))
                {
                    _enemyLookup[key] = data;
                }
            }

            Debug.Log($"[Console] EnemySpawnerAdapter: {_enemyLookup.Count} enemy types loaded.");
        }

        public bool EnemyExists(string enemyId)
        {
            return _enemyLookup.ContainsKey(enemyId.ToLowerInvariant());
        }

        public bool Spawn(string enemyId, int count)
        {
            string key = enemyId.ToLowerInvariant();
            if (!_enemyLookup.TryGetValue(key, out EnemigoData data))
                return false;

            var poolManager = DynamicEnemyPoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("[Console] DynamicEnemyPoolManager.Instance is null.");
                return false;
            }

            // Posición base: cerca del jugador
            Vector3 basePos = GetPlayerPosition();

            for (int i = 0; i < count; i++)
            {
                var controller = poolManager.ObtenerController(data);
                if (controller == null)
                {
                    Debug.LogWarning($"[Console] Failed to obtain controller for {data.nombreEnemigo}");
                    continue;
                }

                // Offset en círculo alrededor del jugador
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                float radius = 3f + count * 0.5f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Vector3 spawnPos = basePos + offset;

                controller.transform.position = spawnPos;
                controller.transform.rotation = Quaternion.LookRotation(basePos - spawnPos);
                controller.Inicializar(data);
                controller.gameObject.SetActive(true);

                _consoleSpawnedEnemies.Add(controller);
            }

            return true;
        }

        public void KillAll()
        {
            int killed = 0;

            // 1. Matar enemigos spawneados por consola
            for (int i = _consoleSpawnedEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _consoleSpawnedEnemies[i];
                if (enemy != null && enemy.EstaVivo())
                {
                    KillEnemy(enemy);
                    killed++;
                }
            }
            _consoleSpawnedEnemies.Clear();

            // 2. Matar enemigos del sistema de chunks
            var chunkManager = WorldChunkManager.Instance;
            if (chunkManager != null)
            {
                killed += KillChunkEnemies(chunkManager);
            }

            Debug.Log($"[Console] KillAll: {killed} enemies killed.");
        }

        private int KillChunkEnemies(WorldChunkManager chunkManager)
        {
            int killed = 0;
            var poolManager = DynamicEnemyPoolManager.Instance;

            // Iterar sobre todos los chunks cargados via reflexión del diccionario interno
            // Usamos el accessor público GetChunk + coordenadas conocidas
            // Alternativa más segura: recorrer un radio amplio alrededor del jugador
            Vector3 playerPos = GetPlayerPosition();
            Vector2Int playerChunk = chunkManager.WorldToChunkCoords(playerPos);
            int searchRadius = 5;

            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int z = -searchRadius; z <= searchRadius; z++)
                {
                    var coords = playerChunk + new Vector2Int(x, z);
                    var chunk = chunkManager.GetChunk(coords);
                    if (chunk == null || chunk.activeEnemies == null)
                        continue;

                    for (int i = chunk.activeEnemies.Count - 1; i >= 0; i--)
                    {
                        var enemy = chunk.activeEnemies[i];
                        if (enemy != null && enemy.EstaVivo())
                        {
                            KillEnemy(enemy);
                            if (poolManager != null)
                                poolManager.DevolverController(enemy, enemy.DatosEnemigo);
                            killed++;
                        }
                    }
                    chunk.activeEnemies.Clear();
                }
            }

            return killed;
        }

        private void KillEnemy(EnemyController enemy)
        {
            // Aplicar daño letal para triggerear el flujo normal de muerte
            int overkill = enemy.Vida_Entidad * 10;
            enemy.RecibirDano(overkill, Flags.ElementAttribute.None);
        }

        private Vector3 GetPlayerPosition()
        {
            if (PlayerPartyManager.Instance != null && PlayerPartyManager.Instance.MainTransform != null)
                return PlayerPartyManager.Instance.MainTransform.position + PlayerPartyManager.Instance.MainTransform.forward * 5f;

            return Vector3.zero;
        }

        /// <summary>
        /// Returns available enemy IDs for error messages.
        /// </summary>
        public string GetAvailableEnemyIds()
        {
            return string.Join(", ", _enemyLookup.Keys);
        }
    }
}
