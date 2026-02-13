namespace Managers
{
    /// <summary>
    /// Estadisticas de un pool para debugging y monitoring.
    /// </summary>
    public struct PoolStats
    {
        public string PoolId;
        public int TotalCreados;
        public int TotalReusos;
        public int Activos;
        public int Disponibles;
        public int Total;
        public int TamanoMaximo;
        public float RatioReuso;
        public bool Destruido;
        
        public override string ToString()
        {
            return $"=== Pool<{PoolId}> Stats ===\n" +
                   $"  Creados: {TotalCreados} | Reusos: {TotalReusos} (ratio: {RatioReuso:P1})\n" +
                   $"  Activos: {Activos} | Disponibles: {Disponibles} | Total: {Total}\n" +
                   $"  Tamano Maximo: {TamanoMaximo} | Destruido: {Destruido}";
        }
        
        /// <summary>
        /// Indica si el pool es eficiente (ratio de reuso >= 80%)
        /// </summary>
        public bool EsEficiente => RatioReuso >= 0.8f;
    }
}
