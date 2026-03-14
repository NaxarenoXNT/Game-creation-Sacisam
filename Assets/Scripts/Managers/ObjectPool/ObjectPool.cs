using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{

    public class ObjectPool : MonoBehaviour
    {
        [Header("Configuracion de Pools")]
        [SerializeField] private List<PoolConfig> configuraciones = new List<PoolConfig>();
        
        // Diccionario de pools usando la nueva arquitectura
        private Dictionary<string, PoolLogic> pools = new Dictionary<string, PoolLogic>();
        private bool isDestroyed = false;
        
        // Singleton
        private static ObjectPool _instance;
        public static ObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ObjectPool>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ObjectPool");
                        _instance = go.AddComponent<ObjectPool>();
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InicializarPools();
        }
        
        private void InicializarPools()
        {
            foreach (var config in configuraciones)
            {
                CrearPool(config);
            }
        }
        
        /// <summary>
        /// Crea un pool nuevo con la configuracion especificada.
        /// </summary>
        public void CrearPool(PoolConfig config)
        {
            if (isDestroyed)
            {
                Debug.LogError($"No se puede crear pool en ObjectPool destruido");
                return;
            }
            
            if (pools.ContainsKey(config.poolId))
            {
                Debug.LogWarning($"Pool ya existe: {config.poolId}");
                return;
            }
            
            // Crear contenedor para organizar objetos
            Transform contenedor = new GameObject($"Pool_{config.poolId}").transform;
            contenedor.SetParent(transform);
            
            // Crear el pool usando PoolLogic
            var pool = new PoolLogic(config, contenedor, this);
            pools[config.poolId] = pool;
            
            Debug.Log($"Pool creado: {config.poolId} con {pool.TotalCreados} objetos (max: {config.tamanoMaximo})");
        }
        
        /// <summary>
        /// Crea un pool dinamicamente desde codigo.
        /// </summary>
        public void CrearPool(string poolId, GameObject prefab, int tamanoInicial = 10, int tamanoMaximo = 50, 
            bool autoReturn = false, float autoReturnDelay = 2f, bool reutilizarMasAntiguo = true)
        {
            var config = new PoolConfig
            {
                poolId = poolId,
                prefab = prefab,
                tamanoInicial = tamanoInicial,
                tamanoMaximo = tamanoMaximo,
                expandirSiNecesario = true,
                autoReturn = autoReturn,
                autoReturnDelay = autoReturnDelay,
                reutilizarMasAntiguo = reutilizarMasAntiguo
            };
            
            CrearPool(config);
        }
        
        /// <summary>
        /// Obtiene un objeto del pool especificado.
        /// API publica mantenida para compatibilidad.
        /// </summary>
        public GameObject Obtener(string poolId)
        {
            if (isDestroyed)
            {
                Debug.LogError($"No se puede obtener de ObjectPool destruido");
                return null;
            }
            
            if (!pools.ContainsKey(poolId))
            {
                Debug.LogError($"Pool no existe: {poolId}");
                return null;
            }
            
            return pools[poolId].Obtener();
        }
        
        /// <summary>
        /// Obtiene un objeto y lo posiciona en el lugar especificado.
        /// </summary>
        public GameObject Obtener(string poolId, Vector3 posicion, Quaternion rotacion)
        {
            if (isDestroyed)
            {
                Debug.LogError($"No se puede obtener de ObjectPool destruido");
                return null;
            }
            
            if (!pools.ContainsKey(poolId))
            {
                Debug.LogError($"Pool no existe: {poolId}");
                return null;
            }
            
            return pools[poolId].Obtener(posicion, rotacion);
        }
        
        /// <summary>
        /// Devuelve un objeto al pool.
        /// API publica mantenida para compatibilidad.
        /// </summary>
        public void Devolver(GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("Intento de devolver objeto null al pool");
                return;
            }
            
            var tracker = obj.GetComponent<PooledObject>();
            if (tracker == null)
            {
                Debug.LogWarning("Objeto no pertenece a ningun pool, destruyendo...");
                Destroy(obj);
                return;
            }
            
            Devolver(tracker.PoolId, obj);
        }
        
        /// <summary>
        /// Devuelve un objeto al pool especificado.
        /// </summary>
        public void Devolver(string poolId, GameObject obj)
        {
            if (obj == null)
            {
                Debug.LogWarning($"Intento de devolver objeto null al pool<{poolId}>");
                return;
            }
            
            if (isDestroyed)
            {
                Debug.LogWarning($"No se puede devolver a ObjectPool destruido. Destruyendo objeto.");
                Destroy(obj);
                return;
            }
            
            if (!pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"Pool no existe: {poolId}, destruyendo objeto");
                Destroy(obj);
                return;
            }
            
            pools[poolId].Devolver(obj);
        }
        
        /// <summary>
        /// Devuelve un objeto al pool despues de un delay.
        /// </summary>
        public void DevolverDespuesDe(GameObject obj, float delay)
        {
            if (obj == null) return;
            StartCoroutine(DevolverCoroutine(obj, delay));
        }
        
        private IEnumerator DevolverCoroutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeInHierarchy)
            {
                Devolver(obj);
            }
        }
        
        /// <summary>
        /// Devuelve todos los objetos activos de un pool.
        /// </summary>
        public void DevolverTodos(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return;
            pools[poolId].DevolverTodos();
        }
        
        /// <summary>
        /// Obtiene la cantidad de objetos disponibles en un pool.
        /// </summary>
        public int ObtenerDisponibles(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return 0;
            return pools[poolId].AvailableCount;
        }
        
        /// <summary>
        /// Obtiene la cantidad de objetos activos en un pool.
        /// </summary>
        public int ObtenerActivos(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return 0;
            return pools[poolId].ActiveCount;
        }
        
        /// <summary>
        /// Obtiene el total de objetos en un pool.
        /// </summary>
        public int ObtenerTotal(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return 0;
            return pools[poolId].TotalCount;
        }
        
        /// <summary>
        /// Obtiene estadisticas de un pool para debugging/monitoring.
        /// </summary>
        public PoolStats ObtenerEstadisticas(string poolId)
        {
            if (!pools.ContainsKey(poolId))
            {
                return new PoolStats { PoolId = poolId, Destruido = true };
            }
            
            return pools[poolId].GetStats();
        }
        
        /// <summary>
        /// Limpia un pool especifico.
        /// </summary>
        public void LimpiarPool(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return;
            
            Debug.Log($"Limpiando pool: {poolId}");
            pools[poolId].Destruir();
            pools.Remove(poolId);
        }
        
        /// <summary>
        /// Limpia todos los pools.
        /// </summary>
        public void LimpiarTodo()
        {
            Debug.Log($"Limpiando todos los pools ({pools.Count} pools)");
            
            foreach (var pool in pools.Values)
            {
                pool.Destruir();
            }
            
            pools.Clear();
        }
        
        private void OnDestroy()
        {
            isDestroyed = true;
            LimpiarTodo();
        }
        
        #region Debug Methods
        
        [ContextMenu("Debug: Mostrar Estado")]
        private void DebugMostrarEstado()
        {
            Debug.Log("=== OBJECT POOL MANAGER ===");
            Debug.Log($"Pools activos: {pools.Count}");
            Debug.Log($"Destruido: {isDestroyed}");
            Debug.Log("");
            
            if (pools.Count == 0)
            {
                Debug.Log("No hay pools activos");
                return;
            }
            
            foreach (var kvp in pools)
            {
                var stats = kvp.Value.GetStats();
                Debug.Log(stats.ToString());
                Debug.Log("");
            }
        }
        
        [ContextMenu("Debug: Devolver Todos")]
        private void DebugDevolverTodos()
        {
            foreach (var poolId in pools.Keys)
            {
                DevolverTodos(poolId);
            }
            Debug.Log("Todos los objetos devueltos a sus pools");
        }
        
        #endregion
    }
}
