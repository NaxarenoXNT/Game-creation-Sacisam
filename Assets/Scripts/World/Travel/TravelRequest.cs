using System;
using UnityEngine;

namespace World.Travel
{
    /// <summary>
    /// Encapsula todos los datos de un viaje rápido solicitado.
    /// Es inmutable tras su creación: cada viaje tiene un único request.
    /// </summary>
    public sealed class TravelRequest
    {
        /// <summary>ID del waypoint destino.</summary>
        public readonly string WaypointId;

        /// <summary>Posición en el mundo a la que se teletransportará el main character.</summary>
        public readonly Vector3 Destination;

        /// <summary>Nombre legible para logs y UI (se usa el waypointId como fallback).</summary>
        public readonly string DisplayName;

        /// <summary>
        /// Callback opcional invocado cuando el viaje se completa con éxito.
        /// Útil para encadenar lógica sin suscribirse al EventBus.
        /// </summary>
        public Action OnCompleted;

        public TravelRequest(string waypointId, Vector3 destination, string displayName = null)
        {
            if (string.IsNullOrWhiteSpace(waypointId))
                throw new ArgumentException("waypointId no puede estar vacío.", nameof(waypointId));

            WaypointId   = waypointId;
            Destination  = destination;
            DisplayName  = string.IsNullOrWhiteSpace(displayName) ? waypointId : displayName;
        }

        /// <summary>
        /// Factory que construye un TravelRequest desde un WaypointMarker colocado en escena.
        /// </summary>
        public static TravelRequest FromWaypoint(WaypointMarker waypoint)
        {
            if (waypoint == null)
                throw new ArgumentNullException(nameof(waypoint));

            return new TravelRequest(waypoint.WaypointId, waypoint.TravelPosition, waypoint.DisplayName);
        }

        public override string ToString() =>
            $"TravelRequest[{WaypointId}] → {Destination} (\"{DisplayName}\")";
    }
}
