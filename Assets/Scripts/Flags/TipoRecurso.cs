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

    /// <summary>
    /// Arquetipo de IA del enemigo. Define su rol y prioridades de combate.
    /// Asignable desde el inspector en EnemigoData.
    /// </summary>
    public enum ArquetipoIA
    {
        Basico,      // Comportamiento genérico: cura si vida baja, ataca aleatorio
        Guerrero,    // Prioriza ataque al jugador más débil, usa habilidades ofensivas
        Mago,        // Prioriza habilidades de Ataque/Control/Debuff sobre ataque básico
        Sanador,     // Cura aliados heridos primero, ataca en segundo plano
        Berserk,     // Ataca siempre al jugador con más vida, ignora autopreservación
        Tanque,      // Prioriza habilidades defensivas/Buff, ataca al más fuerte
        Controlador, // Prioriza Debuff/Control, luego ataca al más débil
        Soporte,     // Buffea aliados primero, cura en segundo lugar, ataca en último
    }
}
