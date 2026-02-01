namespace Flags
{
    /// <summary>
    /// Tipos de recursos que pueden consumir las habilidades.
    /// Fácilmente extensible agregando nuevos valores.
    /// </summary>
    public enum TipoRecurso
    {
        Ninguno,    // Habilidades sin costo (ataque básico)
        Mana,       // Recurso mágico clásico
        Energia,    // Recurso físico (guerreros, ladrones)
        Sangre,     // Recurso de sacrificio (habilidades oscuras)
        Fe,         // Recurso divino (paladines, clérigos)
        Furia,      // Se acumula con combate (berserkers)
        Concentracion, // Se gasta al recibir daño
        Cargas      // Usos limitados que se recargan
    }

    /// <summary>
    /// Categoría funcional de la habilidad.
    /// Útil para IA, UI y filtrado.
    /// </summary>
    public enum CategoriaHabilidad
    {
        Ataque,     // Habilidades ofensivas
        Curacion,   // Restaurar vida/recursos
        Buff,       // Mejoras a aliados
        Debuff,     // Penalizaciones a enemigos
        Control,    // Stun, root, silence, etc.
        Utilidad    // Movimiento, invocación, etc.
    }
}
