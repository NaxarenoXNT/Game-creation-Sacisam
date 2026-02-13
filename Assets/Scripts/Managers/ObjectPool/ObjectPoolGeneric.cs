using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Pool genérico optimizado para componentes de Unity (sin locks - main thread only).
    /// Usado por DynamicEnemyPoolManager y otros sistemas de pooling específico.
    /// 
    /// Uso:
    /// var pool = new ObjectPool<EnemyController>(prefab, 20, container);
    /// var obj = pool.Obtener();
    /// pool.Devolver(obj);
    /// </summary>
    public class ObjectPool<T> : IDisposable where T : Component
    {
        private readonly T prefab;
        private readonly Transform contenedor;
        private readonly int maxSize;
        private readonly bool allowGrowth;
        private readonly bool autoReturn;
        private readonly float autoReturnDelay;
        
        private readonly Queue<T> objetosDisponibles = new Queue<T>();
        private readonly List<T> objetosActivos = new List<T>();
        private readonly HashSet<T> todosLosObjetos = new HashSet<T>();
        
        private bool isDestroyed = false;
        
        // Estadísticas
        public int TotalCreated { get; private set; }
        public int TotalReusos { get; private set; }
        public int ActiveCount => objetosActivos.Count;
        public int AvailableCount => objetosDisponibles.Count;
        public int TotalCount => todosLosObjetos.Count;
        
        public ObjectPool(
            T prefab, 
            int cantidadInicial, 
            Transform contenedor = null,
            int maxSize = -1,
            bool allowGrowth = true,
            bool autoReturn = false,
            float autoReturnDelay = 2f)
        {
            if (prefab == null)
                throw new ArgumentNullException(nameof(prefab), "Prefab no puede ser null");
            
            if (cantidadInicial < 0)
                throw new ArgumentException("cantidadInicial debe ser >= 0", nameof(cantidadInicial));
            
            this.prefab = prefab;
            this.contenedor = contenedor;
            this.maxSize = maxSize <= 0 ? int.MaxValue : maxSize;
            this.allowGrowth = allowGrowth;
            this.autoReturn = autoReturn;
            this.autoReturnDelay = autoReturnDelay;
            
            // Pre-crear objetos
            int precreate = Mathf.Min(cantidadInicial, this.maxSize);
            for (int i = 0; i < precreate; i++)
            {
                CrearNuevo();
            }
            
            Debug.Log($"ObjectPool<{typeof(T).Name}> creado: {precreate} objetos (max: {this.maxSize})");
        }
        
        private T CrearNuevo()
        {
            if (TotalCount >= maxSize)
            {
                Debug.LogWarning($"Pool<{typeof(T).Name}> alcanzo tamano maximo ({maxSize})");
                return null;
            }
            
            T obj = UnityEngine.Object.Instantiate(prefab, contenedor);
            obj.gameObject.SetActive(false);
            obj.name = $"{prefab.name}_Pooled_{TotalCreated}";
            
            objetosDisponibles.Enqueue(obj);
            todosLosObjetos.Add(obj);
            TotalCreated++;
            
            return obj;
        }
        
        public T Obtener()
        {
            if (isDestroyed)
            {
                Debug.LogError($"No se puede obtener de ObjectPool<{typeof(T).Name}> destruido");
                return null;
            }
            
            // Expandir si es necesario
            if (objetosDisponibles.Count == 0)
            {
                if (!allowGrowth || TotalCount >= maxSize)
                {
                    Debug.LogWarning($"Pool<{typeof(T).Name}> agotado, reutilizando mas antiguo");
                    return ReutilizarMasAntiguo();
                }
                
                CrearNuevo();
            }
            
            T obj = objetosDisponibles.Dequeue();
            
            // Validar que el objeto no fue destruido externamente
            if (obj == null)
            {
                todosLosObjetos.RemoveWhere(o => o == null);
                return Obtener(); // Recursión para obtener uno válido
            }
            
            obj.gameObject.SetActive(true);
            objetosActivos.Add(obj);
            
            if (obj is IPooleable pooleable)
            {
                try
                {
                    pooleable.OnObtenidoDelPool();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error en OnObtenidoDelPool: {e.Message}");
                }
            }
            
            if (autoReturn && obj is MonoBehaviour mono)
            {
                mono.StartCoroutine(AutoReturnCoroutine(obj, autoReturnDelay));
            }
            
            TotalReusos++;
            return obj;
        }
        
        public T Obtener(Vector3 posicion, Quaternion rotacion)
        {
            T obj = Obtener();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(posicion, rotacion);
            }
            return obj;
        }
        
        public void Devolver(T obj)
        {
            if (obj == null) return;
            if (isDestroyed) return;
            
            if (!todosLosObjetos.Contains(obj))
            {
                Debug.LogWarning($"Objeto no pertenece al pool<{typeof(T).Name}>: {obj.name}");
                return;
            }
            
            if (!objetosActivos.Remove(obj))
            {
                Debug.LogWarning($"Objeto ya inactivo: {obj.name}");
                return;
            }
            
            if (obj is IPooleable pooleable)
            {
                try
                {
                    pooleable.OnDevueltoAlPool();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error en OnDevueltoAlPool: {e.Message}");
                }
            }
            
            obj.gameObject.SetActive(false);
            
            if (contenedor != null)
            {
                obj.transform.SetParent(contenedor);
            }
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            
            objetosDisponibles.Enqueue(obj);
        }
        
        public void DevolverTodos()
        {
            foreach (var obj in objetosActivos.ToArray())
            {
                if (obj != null)
                {
                    Devolver(obj);
                }
            }
        }
        
        public void Destruir()
        {
            if (isDestroyed) return;
            
            isDestroyed = true;
            
            foreach (var obj in todosLosObjetos)
            {
                if (obj != null)
                    UnityEngine.Object.Destroy(obj.gameObject);
            }
            
            objetosActivos.Clear();
            objetosDisponibles.Clear();
            todosLosObjetos.Clear();
            
            Debug.Log($"ObjectPool<{typeof(T).Name}> destruido");
        }
        
        public void Dispose()
        {
            Destruir();
        }
        
        private T ReutilizarMasAntiguo()
        {
            if (objetosActivos.Count == 0) return null;
            
            T oldest = objetosActivos[0];
            objetosActivos.RemoveAt(0);
            
            if (oldest is IPooleable pooleable)
            {
                try
                {
                    pooleable.OnDevueltoAlPool();
                    pooleable.OnObtenidoDelPool();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error en callbacks de reutilizacion: {e.Message}");
                }
            }
            
            objetosActivos.Add(oldest);
            return oldest;
        }
        
        private IEnumerator AutoReturnCoroutine(T obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (obj != null && obj.gameObject.activeInHierarchy)
            {
                Devolver(obj);
            }
        }
        
        public PoolStats GetStats()
        {
            return new PoolStats
            {
                PoolId = typeof(T).Name,
                TotalCreados = TotalCreated,
                TotalReusos = TotalReusos,
                Activos = ActiveCount,
                Disponibles = AvailableCount,
                Total = TotalCount,
                TamanoMaximo = maxSize,
                RatioReuso = TotalCreated > 0 ? (float)TotalReusos / TotalCreated : 0f,
                Destruido = isDestroyed
            };
        }
    }
}
