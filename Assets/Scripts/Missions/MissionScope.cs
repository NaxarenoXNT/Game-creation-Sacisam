namespace Missions
{
    /// <summary>
    /// Alcance de una misión. Define quién puede progresar/completar la misión
    /// y cómo se distribuyen las recompensas.
    /// </summary>
    public enum MissionScope
    {
        /// <summary>
        /// Atada a facciones, ciudades, NPCs. Cualquier personaje puede contribuir.
        /// Recompensas van a la cuenta global del jugador.
        /// </summary>
        Global,

        /// <summary>
        /// Única de un personaje específico. Solo ese personaje puede progresar/completar.
        /// Se desbloquea por poseer un personaje, clase, evolución o trait.
        /// Recompensas van al personaje que la completa.
        /// </summary>
        Personal,

        /// <summary>
        /// Empieza como global (visible para todos). Cuando un personaje la acepta,
        /// se bloquea a ese personaje exclusivamente (pasa a comportarse como Personal).
        /// </summary>
        Exclusive
    }
}
