using UnityEngine;
using World.Travel;

namespace Managers
{
    // =================================================================
    // =================== EVENTOS DE TRAVEL ===========================
    // =================================================================

    /// <summary>
    /// El jugador (o la UI) solicita un viaje rápido a un waypoint.
    /// Cualquier sistema puede publicar este evento para iniciar el pipeline.
    /// </summary>
    public struct EventoTravelSolicitado : IEvento
    {
        /// <summary>Datos del viaje solicitado.</summary>
        public TravelRequest Request;
    }

    /// <summary>
    /// El TravelManager validó la solicitud y el viaje comenzó formalmente.
    /// UI puede reaccionar mostrando pantalla de carga.
    /// </summary>
    public struct EventoTravelIniciado : IEvento
    {
        /// <summary>Datos del viaje en curso.</summary>
        public TravelRequest Request;
    }

    /// <summary>
    /// El pipeline de viaje finalizó y el jugador está en el destino.
    /// </summary>
    public struct EventoTravelCompletado : IEvento
    {
        /// <summary>Datos del viaje realizado.</summary>
        public TravelRequest Request;

        /// <summary>Posición final del personaje principal.</summary>
        public Vector3 PosicionFinal;
    }

    /// <summary>
    /// El viaje fue cancelado antes de completarse (fallo de validación o error).
    /// </summary>
    public struct EventoTravelCancelado : IEvento
    {
        /// <summary>Datos del viaje rechazado.</summary>
        public TravelRequest Request;

        /// <summary>Motivo del rechazo (para logs y feedback de UI).</summary>
        public string Razon;
    }

    /// <summary>
    /// Un waypoint fue desbloqueado para el jugador.
    /// La UI del mapa puede suscribirse para mostrarlo disponible.
    /// </summary>
    public struct EventoWaypointDesbloqueado : IEvento
    {
        /// <summary>ID del waypoint desbloqueado.</summary>
        public string WaypointId;

        /// <summary>Nombre legible del waypoint.</summary>
        public string NombreWaypoint;
    }
}
