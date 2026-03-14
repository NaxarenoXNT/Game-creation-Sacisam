using Managers;

namespace Missions
{
    // =================================================================
    // =================== EVENTOS DE MISIONES =========================
    // =================================================================

    /// <summary>
    /// Una misión pasó de Locked a Available (condiciones de desbloqueo cumplidas).
    /// </summary>
    public struct EventoMisionDisponible : IEvento
    {
        public MissionDefinitionSO Mision;
    }

    /// <summary>
    /// El jugador aceptó una misión (Available → Active).
    /// </summary>
    public struct EventoMisionAceptada : IEvento
    {
        public MissionInstance Instancia;
    }

    /// <summary>
    /// Un objetivo de una misión activa avanzó en progreso.
    /// </summary>
    public struct EventoMisionProgreso : IEvento
    {
        public MissionInstance Instancia;
        public int IndiceObjetivo;
        public float ProgresoAnterior;
        public float ProgresoNuevo;
    }

    /// <summary>
    /// Un objetivo específico se completó.
    /// </summary>
    public struct EventoObjetivoCompletado : IEvento
    {
        public MissionInstance Instancia;
        public int IndiceObjetivo;
    }

    /// <summary>
    /// Todos los objetivos obligatorios de la misión están completos.
    /// </summary>
    public struct EventoMisionCompletada : IEvento
    {
        public MissionInstance Instancia;
        public MissionRewards Recompensas;
    }

    /// <summary>
    /// La misión falló.
    /// </summary>
    public struct EventoMisionFallida : IEvento
    {
        public MissionInstance Instancia;
        public string Razon;
    }
}
