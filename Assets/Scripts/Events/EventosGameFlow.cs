using GameFlow;

namespace Managers
{
    // =================================================================
    // ================ EVENTOS DE GAME FLOW ===========================
    // =================================================================

    /// <summary>
    /// Se publica cada vez que el GameFlowController cambia de estado activo.
    /// Los sistemas pueden suscribirse para reaccionar al cambio de modo global.
    /// </summary>
    public struct EventoGameFlowChanged : IEvento
    {
        /// <summary>La instancia del nuevo estado activo.</summary>
        public IGameFlowState NuevoEstado;

        /// <summary>Nombre del tipo para logs/debug (ej: "CombatFlowState").</summary>
        public string TipoEstado;
    }
}
