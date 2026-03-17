using UnityEngine;
using System.Collections.Generic;

namespace World.ChunkSystem
{
    /// <summary>
    /// Plantilla reutilizable de configuración de spawns para chunks.
    /// Define qué enemigos aparecen y cómo se distribuyen.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnTemplate", menuName = "World/Chunk Spawn Template", order = 2)]
    public class ChunkSpawnTemplate : ScriptableObject
    {
        [Header("Información")]
        [Tooltip("Nombre descriptivo de la plantilla")]
        public string templateName = "Nueva Plantilla";
        
        [TextArea(2, 4)]
        [Tooltip("Descripción de qué tipo de zona representa")]
        public string description = "Ejemplo: Bosque con Goblins patrullando";
        
        [Header("Configuración de Spawns")]
        [Tooltip("Tipo de distribución de enemigos")]
        public DistributionType distributionType = DistributionType.Grid;
        
        [Tooltip("Enemigos a spawnear en el chunk")]
        public List<SpawnDefinition> spawnDefinitions = new List<SpawnDefinition>();
        
        [Header("Parámetros de Distribución")]
        [Tooltip("Margen desde los bordes del chunk (en metros)")]
        [Range(0f, 20f)]
        public float edgeMargin = 10f;
        
        [Tooltip("Distancia mínima entre enemigos")]
        [Range(5f, 50f)]
        public float minSpacing = 15f;
        
        [Header("IA y Comportamiento")]
        [Tooltip("Estado inicial por defecto para todos los enemigos")]
        public EnemyAIState defaultAIState = EnemyAIState.Patrolling;
        
        [Tooltip("Comportamiento de patrulla por defecto")]
        public PatrolBehavior defaultPatrolBehavior = PatrolBehavior.Loop;
        
        [Tooltip("Tiempo de espera en waypoints (segundos)")]
        [Range(0f, 10f)]
        public float defaultWaypointWaitTime = 2f;
        
        [Header("Waypoints Automáticos")]
        [Tooltip("Generar waypoints automáticamente para patrullas")]
        public bool autoGenerateWaypoints = true;
        
        [Tooltip("Número de waypoints por enemigo")]
        [Range(2, 8)]
        public int waypointsPerEnemy = 4;
        
        [Tooltip("Radio de los waypoints alrededor del spawn")]
        [Range(5f, 30f)]
        public float waypointRadius = 10f;
        
        /// <summary>
        /// Genera las configuraciones de spawn para un chunk específico.
        /// </summary>
        public List<EnemySpawnConfig> GenerateSpawnConfigs(Vector2Int chunkCoords, int chunkSize = 256)
        {
            int effectiveChunkSize = WorldChunkManager.Instance != null 
                ? (int)WorldChunkManager.Instance.ChunkSize 
                : chunkSize;
            
            List<EnemySpawnConfig> configs = new List<EnemySpawnConfig>();
            
            // Calcular posición base del chunk en el mundo
            Vector3 chunkWorldPos = new Vector3(chunkCoords.x * effectiveChunkSize, 0, chunkCoords.y * effectiveChunkSize);
            
            // Área utilizable (restando margen)
            float usableSize = effectiveChunkSize - (edgeMargin * 2);
            
            switch (distributionType)
            {
                case DistributionType.Grid:
                    configs = GenerateGridDistribution(chunkWorldPos, usableSize);
                    break;
                    
                case DistributionType.Random:
                    configs = GenerateRandomDistribution(chunkWorldPos, usableSize);
                    break;
                    
                case DistributionType.Perimeter:
                    configs = GeneratePerimeterDistribution(chunkWorldPos, usableSize);
                    break;
                    
                case DistributionType.Center:
                    configs = GenerateCenterDistribution(chunkWorldPos, usableSize);
                    break;
            }
            
            // Asignar IDs únicos
            for (int i = 0; i < configs.Count; i++)
            {
                configs[i].spawnId = $"chunk_{chunkCoords.x}_{chunkCoords.y}_spawn_{i}";
            }
            
            return configs;
        }
        
        private List<EnemySpawnConfig> GenerateGridDistribution(Vector3 basePos, float usableSize)
        {
            List<EnemySpawnConfig> configs = new List<EnemySpawnConfig>();
            
            if (spawnDefinitions.Count == 0) return configs;
            
            // Calcular grid según cantidad de enemigos
            int totalEnemies = 0;
            foreach (var def in spawnDefinitions)
                totalEnemies += def.count;
            
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalEnemies));
            float cellSize = usableSize / gridSize;
            
            int enemyIndex = 0;
            
            foreach (var def in spawnDefinitions)
            {
                for (int i = 0; i < def.count; i++)
                {
                    int gridX = enemyIndex % gridSize;
                    int gridY = enemyIndex / gridSize;
                    
                    Vector3 position = basePos + new Vector3(
                        edgeMargin + (gridX * cellSize) + (cellSize / 2),
                        0,
                        edgeMargin + (gridY * cellSize) + (cellSize / 2)
                    );
                    
                    // Añadir pequeña variación aleatoria
                    position += new Vector3(
                        Random.Range(-cellSize * 0.2f, cellSize * 0.2f),
                        0,
                        Random.Range(-cellSize * 0.2f, cellSize * 0.2f)
                    );
                    
                    configs.Add(CreateSpawnConfig(def, position));
                    enemyIndex++;
                }
            }
            
            return configs;
        }
        
        private List<EnemySpawnConfig> GenerateRandomDistribution(Vector3 basePos, float usableSize)
        {
            List<EnemySpawnConfig> configs = new List<EnemySpawnConfig>();
            List<Vector3> usedPositions = new List<Vector3>();
            
            foreach (var def in spawnDefinitions)
            {
                for (int i = 0; i < def.count; i++)
                {
                    Vector3 position = Vector3.zero;
                    int attempts = 0;
                    bool validPosition = false;
                    
                    // Intentar encontrar posición válida
                    while (!validPosition && attempts < 30)
                    {
                        position = basePos + new Vector3(
                            edgeMargin + Random.Range(0f, usableSize),
                            0,
                            edgeMargin + Random.Range(0f, usableSize)
                        );
                        
                        // Verificar distancia mínima con otros spawns
                        validPosition = true;
                        foreach (var usedPos in usedPositions)
                        {
                            if (Vector3.Distance(position, usedPos) < minSpacing)
                            {
                                validPosition = false;
                                break;
                            }
                        }
                        
                        attempts++;
                    }
                    
                    if (validPosition)
                    {
                        usedPositions.Add(position);
                        configs.Add(CreateSpawnConfig(def, position));
                    }
                }
            }
            
            return configs;
        }
        
        private List<EnemySpawnConfig> GeneratePerimeterDistribution(Vector3 basePos, float usableSize)
        {
            List<EnemySpawnConfig> configs = new List<EnemySpawnConfig>();
            
            int totalEnemies = 0;
            foreach (var def in spawnDefinitions)
                totalEnemies += def.count;
            
            float perimeter = usableSize * 4;
            float spacing = perimeter / totalEnemies;
            
            int enemyIndex = 0;
            
            foreach (var def in spawnDefinitions)
            {
                for (int i = 0; i < def.count; i++)
                {
                    float distance = enemyIndex * spacing;
                    Vector3 position = basePos + new Vector3(edgeMargin, 0, edgeMargin);
                    
                    // Calcular posición en el perímetro
                    if (distance < usableSize) // Lado inferior
                    {
                        position += new Vector3(distance, 0, 0);
                    }
                    else if (distance < usableSize * 2) // Lado derecho
                    {
                        position += new Vector3(usableSize, 0, distance - usableSize);
                    }
                    else if (distance < usableSize * 3) // Lado superior
                    {
                        position += new Vector3(usableSize - (distance - usableSize * 2), 0, usableSize);
                    }
                    else // Lado izquierdo
                    {
                        position += new Vector3(0, 0, usableSize - (distance - usableSize * 3));
                    }
                    
                    configs.Add(CreateSpawnConfig(def, position));
                    enemyIndex++;
                }
            }
            
            return configs;
        }
        
        private List<EnemySpawnConfig> GenerateCenterDistribution(Vector3 basePos, float usableSize)
        {
            List<EnemySpawnConfig> configs = new List<EnemySpawnConfig>();
            Vector3 center = basePos + new Vector3(usableSize / 2 + edgeMargin, 0, usableSize / 2 + edgeMargin);
            
            int totalEnemies = 0;
            foreach (var def in spawnDefinitions)
                totalEnemies += def.count;
            
            float angleStep = 360f / totalEnemies;
            int enemyIndex = 0;
            
            foreach (var def in spawnDefinitions)
            {
                for (int i = 0; i < def.count; i++)
                {
                    float angle = enemyIndex * angleStep * Mathf.Deg2Rad;
                    float radius = usableSize * 0.25f;
                    
                    Vector3 position = center + new Vector3(
                        Mathf.Cos(angle) * radius,
                        0,
                        Mathf.Sin(angle) * radius
                    );
                    
                    configs.Add(CreateSpawnConfig(def, position));
                    enemyIndex++;
                }
            }
            
            return configs;
        }
        
        private EnemySpawnConfig CreateSpawnConfig(SpawnDefinition def, Vector3 position)
        {
            EnemySpawnConfig config = new EnemySpawnConfig
            {
                enemyData = def.enemyData,
                spawnPosition = position,
                spawnRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0),
                initialAIState = def.overrideAIState ? def.aiState : defaultAIState,
                patrolBehavior = def.overridePatrolBehavior ? def.patrolBehavior : defaultPatrolBehavior,
                waypointWaitTime = defaultWaypointWaitTime,
                detectionRadius = def.customDetectionRadius > 0 ? def.customDetectionRadius : 0,
                isUnique = def.isUnique,
                customTags = new List<string>(def.customTags)
            };
            
            // Generar waypoints automáticos si está habilitado
            if (autoGenerateWaypoints && config.initialAIState == EnemyAIState.Patrolling)
            {
                config.patrolWaypoints = GenerateWaypoints(position, waypointRadius, waypointsPerEnemy);
            }
            else if (def.customWaypoints != null && def.customWaypoints.Count > 0)
            {
                // Usar waypoints personalizados (relativos a la posición)
                config.patrolWaypoints = new List<Vector3>();
                foreach (var offset in def.customWaypoints)
                {
                    config.patrolWaypoints.Add(position + offset);
                }
            }
            
            return config;
        }
        
        private List<Vector3> GenerateWaypoints(Vector3 center, float radius, int count)
        {
            List<Vector3> waypoints = new List<Vector3>();
            float angleStep = 360f / count;
            
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 waypoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                waypoints.Add(waypoint);
            }
            
            return waypoints;
        }
    }
    
    /// <summary>
    /// Definición de un tipo de spawn.
    /// </summary>
    [System.Serializable]
    public class SpawnDefinition
    {
        [Header("Enemigo")]
        [Tooltip("Datos del enemigo a spawnear")]
        public EnemigoData enemyData;
        
        [Tooltip("Cantidad de este enemigo")]
        [Range(1, 20)]
        public int count = 1;
        
        [Header("Configuración Personalizada")]
        [Tooltip("Sobreescribir estado de IA por defecto")]
        public bool overrideAIState = false;
        public EnemyAIState aiState = EnemyAIState.Patrolling;
        
        [Tooltip("Sobreescribir comportamiento de patrulla")]
        public bool overridePatrolBehavior = false;
        public PatrolBehavior patrolBehavior = PatrolBehavior.Loop;
        
        [Tooltip("Radio de detección personalizado (0 = usar default)")]
        public float customDetectionRadius = 0f;
        
        [Tooltip("Enemigo único (boss, no respawnea)")]
        public bool isUnique = false;
        
        [Tooltip("Waypoints personalizados (relativos a posición de spawn)")]
        public List<Vector3> customWaypoints = new List<Vector3>();
        
        [Tooltip("Tags personalizados")]
        public List<string> customTags = new List<string>();
    }
    
    /// <summary>
    /// Tipo de distribución de enemigos en el chunk.
    /// </summary>
    public enum DistributionType
    {
        Grid,       // Grid uniforme
        Random,     // Posiciones aleatorias con espaciado mínimo
        Perimeter,  // Alrededor del borde
        Center      // Círculo central
    }
}
