using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Clase interna que maneja la logica de UN solo pool.
    /// Encapsula toda la logica de instanciacion, cola, y tracking de objetos.
    /// </summary>
    internal class PoolLogic
    {
        private readonly PoolConfig config;
        private readonly Transform contenedor;
        private readonly MonoBehaviour coroutineRunner;
        
        private readonly Queue<GameObject> objetosDisponibles = new Queue<GameObject>();
        private readonly List<GameObject> objetosActivos = new List<GameObject>();
        private readonly HashSet<GameObject> todosLosObjetos = new HashSet<GameObject>();
        
        private int totalCreados;
        private int totalReusos;
        private bool isDestroyed;
        
        public int TotalCreados => totalCreados;
        public int TotalReusos => totalReusos;
        public int ActiveCount => objetosActivos.Count;
        public int AvailableCount => objetosDisponibles.Count;
        public int TotalCount => todosLosObjetos.Count;
        public bool IsDestroyed => isDestroyed;
        
        public PoolLogic(PoolConfig config, Transform contenedor, MonoBehaviour coroutineRunner)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.contenedor = contenedor;
            this.coroutineRunner = coroutineRunner;
            
            // Pre-crear objetos
            int precreate = Mathf.Min(config.tamanoInicial, config.tamanoMaximo);
            for (int i = 0; i < precreate; i++)
            {
                CrearObjeto();
            }
        }
        
        private GameObject CrearObjeto()
        {
            if (todosLosObjetos.Count >= config.tamanoMaximo)
            {
                Debug.LogWarning($"Pool<{config.poolId}> alcanzo tamano maximo ({config.tamanoMaximo})");
                return null;
            }
            
            GameObject obj = UnityEngine.Object.Instantiate(config.prefab, contenedor);
            obj.SetActive(false);
            obj.name = $"{config.prefab.name}_Pooled_{totalCreados}";
            
            // Agregar componente tracker
            var tracker = obj.GetComponent<PooledObject>();
            if (tracker == null)
            {
                tracker = obj.AddComponent<PooledObject>();
            }
            tracker.PoolId = config.poolId;
            
            objetosDisponibles.Enqueue(obj);
            todosLosObjetos.Add(obj);
            totalCreados++;
            
            return obj;
        }
        
        public GameObject Obtener()
        {
            if (isDestroyed)
            {
                Debug.LogError($"No se puede obtener de pool destruido: {config.poolId}");
                return null;
            }
            
            // Limpiar objetos destruidos externamente
            while (objetosDisponibles.Count > 0 && objetosDisponibles.Peek() == null)
            {
                objetosDisponibles.Dequeue();
                Debug.LogWarning($"Pool<{config.poolId}>: Objeto destruido externamente detectado");
            }
            
            GameObject obj = null;
            
            // Si no hay disponibles, expandir o reutilizar
            if (objetosDisponibles.Count == 0)
            {
                if (config.expandirSiNecesario && todosLosObjetos.Count < config.tamanoMaximo)
                {
                    CrearObjeto();
                }
                else if (config.reutilizarMasAntiguo && objetosActivos.Count > 0)
                {
                    return ReutilizarMasAntiguo();
                }
                
                if (objetosDisponibles.Count == 0)
                {
                    Debug.LogWarning($"Pool<{config.poolId}> agotado y no se puede expandir");
                    return null;
                }
            }
            
            obj = objetosDisponibles.Dequeue();
            
            // Doble verificacion
            if (obj == null)
            {
                todosLosObjetos.RemoveWhere(o => o == null);
                return Obtener(); // Recursion
            }
            
            totalReusos++;
            obj.SetActive(true);
            objetosActivos.Add(obj);
            
            // Notificar IPooleable
            NotificarObtenido(obj);
            
            // Auto-return si esta configurado
            if (config.autoReturn && coroutineRunner != null)
            {
                coroutineRunner.StartCoroutine(AutoReturnCoroutine(obj, config.autoReturnDelay));
            }
            
            return obj;
        }
        
        public GameObject Obtener(Vector3 posicion, Quaternion rotacion)
        {
            GameObject obj = Obtener();
            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(posicion, rotacion);
            }
            return obj;
        }
        
        public void Devolver(GameObject obj)
        {
            if (obj == null || isDestroyed) return;
            
            // Validar pertenencia
            if (!todosLosObjetos.Contains(obj))
            {
                Debug.LogWarning($"Objeto no pertenece al pool<{config.poolId}>");
                return;
            }
            
            // Evitar doble devolucion
            if (!objetosActivos.Contains(obj))
            {
                Debug.LogWarning($"Objeto ya fue devuelto al pool<{config.poolId}>");
                return;
            }
            
            // Notificar IPooleable
            NotificarDevuelto(obj);
            
            // Resetear y desactivar
            obj.SetActive(false);
            obj.transform.SetParent(contenedor);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;
            
            objetosActivos.Remove(obj);
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
        
        private GameObject ReutilizarMasAntiguo()
        {
            if (objetosActivos.Count == 0) return null;
            
            GameObject oldest = objetosActivos[0];
            objetosActivos.RemoveAt(0);
            
            NotificarDevuelto(oldest);
            NotificarObtenido(oldest);
            
            objetosActivos.Add(oldest);
            return oldest;
        }
        
        private void NotificarObtenido(GameObject obj)
        {
            var pooleables = obj.GetComponents<IPooleable>();
            foreach (var pooleable in pooleables)
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
        }
        
        private void NotificarDevuelto(GameObject obj)
        {
            var pooleables = obj.GetComponents<IPooleable>();
            foreach (var pooleable in pooleables)
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
        }
        
        private IEnumerator AutoReturnCoroutine(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null && obj.activeInHierarchy)
            {
                Devolver(obj);
            }
        }
        
        public PoolStats GetStats()
        {
            return new PoolStats
            {
                PoolId = config.poolId,
                TotalCreados = totalCreados,
                TotalReusos = totalReusos,
                Activos = objetosActivos.Count,
                Disponibles = objetosDisponibles.Count,
                Total = todosLosObjetos.Count,
                TamanoMaximo = config.tamanoMaximo,
                RatioReuso = totalCreados > 0 ? (float)totalReusos / totalCreados : 0f,
                Destruido = isDestroyed
            };
        }
        
        public void Destruir()
        {
            if (isDestroyed) return;
            
            isDestroyed = true;
            
            foreach (var obj in todosLosObjetos)
            {
                if (obj != null)
                {
                    UnityEngine.Object.Destroy(obj);
                }
            }
            
            objetosActivos.Clear();
            objetosDisponibles.Clear();
            todosLosObjetos.Clear();
            
            if (contenedor != null)
            {
                UnityEngine.Object.Destroy(contenedor.gameObject);
            }
        }
    }
}
