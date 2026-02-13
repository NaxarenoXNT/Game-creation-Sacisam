using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

/// <summary>
/// Manager dinámico de pooling de enemigos.
/// Crea pools on-demand solo para tipos de enemigos necesarios.
/// Limpia automáticamente pools no utilizados.
/// </summary>
public class DynamicEnemyPoolManager : MonoBehaviour
{
    public static DynamicEnemyPoolManager Instance { get; private set; }
    
    [Header("Referencias")]
    [SerializeField] private EnemyController enemyControllerPrefab;
    
    [Header("Configuración")]
    [Tooltip("Tiempo antes de limpiar pools no usados (segundos)")]
    [SerializeField] private float tiempoLimpiezaPools = 120f;
    
    [Tooltip("Tamaño inicial de cada pool creado")]
    [SerializeField] private int tamanoInicialPool = 5;
    
    [Tooltip("Tamaño máximo de cada pool")]
    [SerializeField] private int tamanoMaximoPool = 20;
    
    // Diccionario de pools activos por EnemigoData
    private Dictionary<EnemigoData, ObjectPool<EnemyController>> activePools = new Dictionary<EnemigoData, ObjectPool<EnemyController>>();
    
    // Tracking de último uso de cada pool
    private Dictionary<EnemigoData, float> poolLastUsedTime = new Dictionary<EnemigoData, float>();
    
    // Estadísticas
    public int TotalActivePools => activePools.Count;
    public int TotalActiveEnemies
    {
        get
        {
            int total = 0;
            foreach (var pool in activePools.Values)
            {
                total += pool.ActiveCount;
            }
            return total;
        }
    }
    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("✨ DynamicEnemyPoolManager inicializado");
    }
    
    void Start()
    {
        // Iniciar coroutine de limpieza periódica
        StartCoroutine(LimpiezaPeriodica());
    }
    
    /// <summary>
    /// Obtiene un controller del pool apropiado (crea el pool si no existe).
    /// </summary>
    public EnemyController ObtenerController(EnemigoData enemyData)
    {
        if (enemyData == null)
        {
            Debug.LogError("EnemigoData es null");
            return null;
        }
        
        // Obtener o crear el pool
        var pool = ObtenerOCrearPool(enemyData);
        if (pool == null)
        {
            Debug.LogError($"No se pudo crear/obtener pool para {enemyData.name}");
            return null;
        }
        
        // Obtener controller del pool
        var controller = pool.Obtener();
        
        if (controller != null)
        {
            // Actualizar timestamp de uso
            poolLastUsedTime[enemyData] = Time.time;
        }
        
        return controller;
    }
    
    /// <summary>
    /// Devuelve un controller a su pool apropiado.
    /// </summary>
    public void DevolverController(EnemyController controller, EnemigoData enemyData)
    {
        if (controller == null || enemyData == null)
        {
            Debug.LogWarning("Controller o EnemyData es null al intentar devolver");
            return;
        }
        
        if (activePools.TryGetValue(enemyData, out var pool))
        {
            pool.Devolver(controller);
            poolLastUsedTime[enemyData] = Time.time;
        }
        else
        {
            Debug.LogWarning($"No existe pool para {enemyData.name}, destruyendo controller");
            Destroy(controller.gameObject);
        }
    }
    
    /// <summary>
    /// Pre-carga un pool para un tipo de enemigo específico.
    /// Útil para cargar enemigos de una zona antes de que el jugador llegue.
    /// </summary>
    public void PrecargarPool(EnemigoData enemyData, int initialSize = -1, int maxSize = -1)
    {
        if (enemyData == null)
        {
            Debug.LogError("EnemigoData es null en PrecargarPool");
            return;
        }
        
        if (!activePools.ContainsKey(enemyData))
        {
            int initSize = initialSize > 0 ? initialSize : tamanoInicialPool;
            int maximoSize = maxSize > 0 ? maxSize : tamanoMaximoPool;
            CrearPool(enemyData, initSize, maximoSize);
        }
    }
    
    /// <summary>
    /// Limpia un pool específico.
    /// </summary>
    public void LimpiarPool(EnemigoData enemyData)
    {
        if (enemyData == null) return;
        
        if (activePools.TryGetValue(enemyData, out var pool))
        {
            Debug.Log($"🧹 Limpiando pool: {enemyData.name}");
            pool.Destruir();
            activePools.Remove(enemyData);
            poolLastUsedTime.Remove(enemyData);
        }
    }
    
    /// <summary>
    /// Limpia todos los pools activos.
    /// </summary>
    public void LimpiarTodosLosPools()
    {
        Debug.Log($"🧹 Limpiando todos los pools ({activePools.Count} pools)");
        
        foreach (var pool in activePools.Values)
        {
            pool?.Destruir();
        }
        
        activePools.Clear();
        poolLastUsedTime.Clear();
    }
    
    private ObjectPool<EnemyController> ObtenerOCrearPool(EnemigoData enemyData)
    {
        if (!activePools.TryGetValue(enemyData, out var pool))
        {
            pool = CrearPool(enemyData, tamanoInicialPool, tamanoMaximoPool);
        }
        
        return pool;
    }
    
    private ObjectPool<EnemyController> CrearPool(EnemigoData enemyData, int initialSize, int maxSize)
    {
        if (enemyControllerPrefab == null)
        {
            Debug.LogError("EnemyController prefab no está asignado en DynamicEnemyPoolManager");
            return null;
        }
        
        Debug.Log($"📦 Creando pool: {enemyData.name} (Init: {initialSize}, Max: {maxSize})");
        
        // Crear contenedor para organizar objetos
        var container = new GameObject($"Pool_{enemyData.name}").transform;
        container.SetParent(transform);
        
        // Crear el pool usando ObjectPool<T>
        var pool = new ObjectPool<EnemyController>(
            prefab: enemyControllerPrefab,
            cantidadInicial: initialSize,
            contenedor: container,
            maxSize: maxSize,
            allowGrowth: true,
            autoReturn: false // Los enemigos se devuelven manualmente
        );
        
        // Registrar el pool
        activePools[enemyData] = pool;
        poolLastUsedTime[enemyData] = Time.time;
        
        return pool;
    }
    
    private IEnumerator LimpiezaPeriodica()
    {
        while (true)
        {
            yield return new WaitForSeconds(tiempoLimpiezaPools);
            
            var poolsALimpiar = new List<EnemigoData>();
            float tiempoActual = Time.time;
            
            // Buscar pools que no se han usado en mucho tiempo
            foreach (var kvp in poolLastUsedTime)
            {
                float tiempoDesdeUltimoUso = tiempoActual - kvp.Value;
                
                // Si no se usa por el doble del tiempo de limpieza, marcar para eliminar
                if (tiempoDesdeUltimoUso > tiempoLimpiezaPools * 2)
                {
                    // Solo limpiar si no tiene objetos activos
                    if (activePools.TryGetValue(kvp.Key, out var pool) && pool.ActiveCount == 0)
                    {
                        poolsALimpiar.Add(kvp.Key);
                    }
                }
            }
            
            // Limpiar pools marcados
            foreach (var enemyData in poolsALimpiar)
            {
                LimpiarPool(enemyData);
            }
            
            if (poolsALimpiar.Count > 0)
            {
                Debug.Log($"🧹 Limpieza periódica: {poolsALimpiar.Count} pools eliminados");
            }
        }
    }
    
    void OnDestroy()
    {
        LimpiarTodosLosPools();
    }
    
    #region Debug Methods
    
    [ContextMenu("Debug: Mostrar Estado")]
    private void DebugMostrarEstado()
    {
        Debug.Log("=== DYNAMIC ENEMY POOL MANAGER ===");
        Debug.Log($"Pools activos: {TotalActivePools}");
        Debug.Log($"Enemigos activos totales: {TotalActiveEnemies}");
        Debug.Log("");
        
        if (activePools.Count == 0)
        {
            Debug.Log("No hay pools activos");
            return;
        }
        
        foreach (var kvp in activePools)
        {
            var enemyData = kvp.Key;
            var pool = kvp.Value;
            var stats = pool.GetStats();
            var lastUsed = Time.time - poolLastUsedTime[enemyData];
            
            Debug.Log($"📦 Pool: {enemyData.name}");
            Debug.Log($"   Activos: {stats.Activos} | Disponibles: {stats.Disponibles} | Total: {stats.Total}");
            Debug.Log($"   Creados: {stats.TotalCreados} | Reusos: {stats.TotalReusos} | Ratio: {stats.RatioReuso:P1}");
            Debug.Log($"   Último uso: hace {lastUsed:F1}s");
            Debug.Log("");
        }
    }
    
    [ContextMenu("Debug: Devolver Todos")]
    private void DebugDevolverTodos()
    {
        foreach (var pool in activePools.Values)
        {
            pool.DevolverTodos();
        }
        
        Debug.Log("Todos los enemigos devueltos a sus pools");
    }
    
    [ContextMenu("Debug: Limpiar Pools No Usados")]
    private void DebugLimpiarPoolsNoUsados()
    {
        var poolsALimpiar = new List<EnemigoData>();
        
        foreach (var kvp in activePools)
        {
            if (kvp.Value.ActiveCount == 0)
            {
                poolsALimpiar.Add(kvp.Key);
            }
        }
        
        foreach (var enemyData in poolsALimpiar)
        {
            LimpiarPool(enemyData);
        }
        
        Debug.Log($"🧹 {poolsALimpiar.Count} pools limpiados");
    }
    
    #endregion
}
