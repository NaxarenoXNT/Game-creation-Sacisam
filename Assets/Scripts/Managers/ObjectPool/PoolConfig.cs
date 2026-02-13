using UnityEngine;

namespace Managers
{
    /// <summary>
    /// Configuracion de un pool individual.
    /// </summary>
    [System.Serializable]
    public class PoolConfig
    {
        public string poolId;
        public GameObject prefab;
        public int tamanoInicial = 10;
        public int tamanoMaximo = 50;
        public bool expandirSiNecesario = true;
        
        [Tooltip("Si es true, devuelve objetos automaticamente despues del delay")]
        public bool autoReturn = false;
        
        [Tooltip("Tiempo en segundos antes de auto-devolucion")]
        public float autoReturnDelay = 2f;
        
        [Tooltip("Si es true, reutiliza el objeto mas antiguo cuando el pool esta lleno")]
        public bool reutilizarMasAntiguo = true;
    }
}
