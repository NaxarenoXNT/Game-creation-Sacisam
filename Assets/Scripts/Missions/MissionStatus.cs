namespace Missions
{
    /// <summary>
    /// Estados posibles de una misión en el ciclo de vida.
    /// </summary>
    public enum MissionStatus
    {
        /// <summary>La misión existe pero las condiciones de desbloqueo no están cumplidas.</summary>
        Locked,

        /// <summary>Las condiciones de desbloqueo se cumplen. El jugador puede aceptarla.</summary>
        Available,

        /// <summary>El jugador aceptó la misión. Los objetivos se rastrean activamente.</summary>
        Active,

        /// <summary>Todos los objetivos obligatorios están completos.</summary>
        Completed,

        /// <summary>La misión falló (por límite de tiempo, muerte de NPC, etc.).</summary>
        Failed
    }
}
