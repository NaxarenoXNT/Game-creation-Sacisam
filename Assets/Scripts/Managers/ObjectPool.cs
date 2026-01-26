using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Pool generico de objetos para reutilizar GameObjects.
    /// Ideal para efectos visuales, proyectiles, particulas, etc.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [System.Serializable]
        public class PoolConfig
        {
            public string poolId;
            public GameObject prefab;
            public int tamanoInicial = 10;
            public int tamanoMaximo = 50;
            public bool expandirSiNecesario = true;
        }
        
        [Header("Configuracion de Pools")]
        [SerializeField] private List<PoolConfig> configuraciones = new List<PoolConfig>();
        
        // Diccionario de pools: poolId -> Queue de objetos
        private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, PoolConfig> configs = new Dictionary<string, PoolConfig>();
        private Dictionary<string, Transform> contenedores = new Dictionary<string, Transform>();
        
        // Singleton
        private static ObjectPool _instance;
        public static ObjectPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ObjectPool>();
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
            if (pools.ContainsKey(config.poolId))
            {
                Debug.LogWarning("Pool ya existe: " + config.poolId);
                return;
            }
            
            // Crear contenedor para organizar objetos
            Transform contenedor = new GameObject("Pool_" + config.poolId).transform;
            contenedor.SetParent(transform);
            contenedores[config.poolId] = contenedor;
            
            // Crear pool y configuracion
            pools[config.poolId] = new Queue<GameObject>();
            configs[config.poolId] = config;
            
            // Pre-instanciar objetos
            for (int i = 0; i < config.tamanoInicial; i++)
            {
                CrearObjeto(config.poolId);
            }
            
            Debug.Log("Pool creado: " + config.poolId + " con " + config.tamanoInicial + " objetos");
        }
        
        /// <summary>
        /// Crea un pool dinamicamente desde codigo.
        /// </summary>
        public void CrearPool(string poolId, GameObject prefab, int tamanoInicial = 10, int tamanoMaximo = 50)
        {
            var config = new PoolConfig
            {
                poolId = poolId,
                prefab = prefab,
                tamanoInicial = tamanoInicial,
                tamanoMaximo = tamanoMaximo,
                expandirSiNecesario = true
            };
            
            CrearPool(config);
        }
        
        private GameObject CrearObjeto(string poolId)
        {
            if (!configs.ContainsKey(poolId)) return null;
            
            var config = configs[poolId];
            GameObject obj = Instantiate(config.prefab, contenedores[poolId]);
            obj.SetActive(false);
            
            // Agregar componente para rastrear el pool de origen
            var tracker = obj.AddComponent<PooledObject>();
            tracker.PoolId = poolId;
            
            pools[poolId].Enqueue(obj);
            return obj;
        }
        
        /// <summary>
        /// Obtiene un objeto del pool especificado.
        /// </summary>
        public GameObject Obtener(string poolId)
        {
            if (!pools.ContainsKey(poolId))
            {
                Debug.LogError("Pool no existe: " + poolId);
                return null;
            }
            
            var pool = pools[poolId];
            var config = configs[poolId];
            
            // Si no hay objetos disponibles
            if (pool.Count == 0)
            {
                if (config.expandirSiNecesario)
                {
                    // Verificar limite maximo
                    int totalObjetos = ObtenerTotalObjetos(poolId);
                    if (totalObjetos >= config.tamanoMaximo)
                    {
                        Debug.LogWarning("Pool " + poolId + " alcanzo el limite maximo: " + config.tamanoMaximo);
                        return null;
                    }
                    
                    CrearObjeto(poolId);
                }
                else
                {
                    return null;
                }
            }
            
            GameObject obj = pool.Dequeue();
            obj.SetActive(true);
            return obj;
        }
        
        /// <summary>
        /// Obtiene un objeto y lo posiciona en el lugar especificado.
        /// </summary>
        public GameObject Obtener(string poolId, Vector3 posicion, Quaternion rotacion)
        {
            GameObject obj = Obtener(poolId);
            if (obj != null)
            {
                obj.transform.position = posicion;
                obj.transform.rotation = rotacion;
            }
            return obj;
        }
        
        /// <summary>
        /// Devuelve un objeto al pool.
        /// </summary>
        public void Devolver(GameObject obj)
        {
            if (obj == null) return;
            
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
            if (!pools.ContainsKey(poolId))
            {
                Debug.LogWarning("Pool no existe: " + poolId + ", destruyendo objeto");
                Destroy(obj);
                return;
            }
            
            obj.SetActive(false);
            obj.transform.SetParent(contenedores[poolId]);
            pools[poolId].Enqueue(obj);
        }
        
        /// <summary>
        /// Devuelve un objeto al pool despues de un delay.
        /// </summary>
        public void DevolverDespuesDe(GameObject obj, float delay)
        {
            StartCoroutine(DevolverCoroutine(obj, delay));
        }
        
        private System.Collections.IEnumerator DevolverCoroutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Devolver(obj);
        }
        
        /// <summary>
        /// Obtiene la cantidad de objetos disponibles en un pool.
        /// </summary>
        public int ObtenerDisponibles(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return 0;
            return pools[poolId].Count;
        }
        
        private int ObtenerTotalObjetos(string poolId)
        {
            if (!contenedores.ContainsKey(poolId)) return 0;
            return contenedores[poolId].childCount;
        }
        
        /// <summary>
        /// Limpia todos los pools.
        /// </summary>
        public void LimpiarTodo()
        {
            foreach (var kvp in pools)
            {
                while (kvp.Value.Count > 0)
                {
                    var obj = kvp.Value.Dequeue();
                    if (obj != null) Destroy(obj);
                }
            }
            
            pools.Clear();
            configs.Clear();
            
            foreach (var contenedor in contenedores.Values)
            {
                if (contenedor != null) Destroy(contenedor.gameObject);
            }
            contenedores.Clear();
        }
    }
    
    /// <summary>
    /// Componente auxiliar para rastrear el pool de origen de un objeto.
    /// </summary>
    public class PooledObject : MonoBehaviour
    {
        public string PoolId { get; set; }
        
        /// <summary>
        /// Devuelve este objeto al pool automaticamente.
        /// </summary>
        public void DevolverAlPool()
        {
            ObjectPool.Instance.Devolver(gameObject);
        }
        
        /// <summary>
        /// Devuelve este objeto al pool despues de un delay.
        /// </summary>
        public void DevolverAlPoolDespuesDe(float delay)
        {
            ObjectPool.Instance.DevolverDespuesDe(gameObject, delay);
        }
    }
}
