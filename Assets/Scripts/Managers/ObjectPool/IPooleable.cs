namespace Managers
{
    /// <summary>
    /// Interfaz para objetos que necesitan callbacks al ser pooled/unpooled.
    /// Implementa esto en tus componentes para reiniciar estado correctamente.
    /// </summary>
    public interface IPooleable
    {
        /// <summary>
        /// Llamado cuando el objeto sale del pool (se activa).
        /// Usa esto para reiniciar estado, suscribirse a eventos, etc.
        /// </summary>
        void OnObtenidoDelPool();
        
        /// <summary>
        /// Llamado cuando el objeto vuelve al pool (se desactiva).
        /// Usa esto para limpiar referencias, desuscribirse de eventos, etc.
        /// </summary>
        void OnDevueltoAlPool();
    }
}
