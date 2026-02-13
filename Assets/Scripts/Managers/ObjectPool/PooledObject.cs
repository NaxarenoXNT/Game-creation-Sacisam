using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Componente auxiliar para rastrear el pool de origen de un objeto.
    /// Tambien implementa IPooleable para limpiar estado basico.
    /// </summary>
    public class PooledObject : MonoBehaviour, IPooleable
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
        
        public virtual void OnObtenidoDelPool()
        {
            // Override en subclases para reiniciar estado
        }
        
        public virtual void OnDevueltoAlPool()
        {
            // Override en subclases para limpiar estado
            StopAllCoroutines();
        }
    }
}
