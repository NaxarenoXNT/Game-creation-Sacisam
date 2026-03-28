using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace World.Travel
{
    /// <summary>
    /// Registro global de waypoints activos en la escena.
    /// Los WaypointMarker se auto-registran en OnEnable y se desregistran en OnDisable,
    /// por lo que este registro refleja siempre el estado actual del mundo cargado.
    ///
    /// Los sistemas de UI (mapa, menú de viaje) consultan este registro para mostrar
    /// los destinos disponibles sin ningún acoplamiento directo a la escena.
    /// </summary>
    public static class WaypointRegistry
    {
        private static readonly Dictionary<string, WaypointMarker> _waypoints =
            new Dictionary<string, WaypointMarker>();

        // ── Registro ─────────────────────────────────────────────────────────────

        /// <summary>Registra un waypoint. Llamado automáticamente por WaypointMarker.OnEnable.</summary>
        public static void Register(WaypointMarker marker)
        {
            if (marker == null) return;

            if (_waypoints.ContainsKey(marker.WaypointId))
            {
                Debug.LogWarning($"[WaypointRegistry] WaypointId duplicado: '{marker.WaypointId}'. " +
                                 $"Solo se conserva el primero registrado.");
                return;
            }

            _waypoints[marker.WaypointId] = marker;
        }

        /// <summary>Desregistra un waypoint. Llamado automáticamente por WaypointMarker.OnDisable.</summary>
        public static void Unregister(WaypointMarker marker)
        {
            if (marker == null) return;

            if (_waypoints.TryGetValue(marker.WaypointId, out var stored) && stored == marker)
                _waypoints.Remove(marker.WaypointId);
        }

        // ── Consultas ────────────────────────────────────────────────────────────

        /// <summary>Devuelve el WaypointMarker con el ID indicado, o null si no existe.</summary>
        public static WaypointMarker Get(string waypointId)
        {
            _waypoints.TryGetValue(waypointId, out var marker);
            return marker;
        }

        /// <summary>Todos los waypoints activos en la escena (desbloqueados y bloqueados).</summary>
        public static IReadOnlyCollection<WaypointMarker> All => _waypoints.Values;

        /// <summary>Solo los waypoints desbloqueados y disponibles para viajar.</summary>
        public static IEnumerable<WaypointMarker> Unlocked =>
            _waypoints.Values.Where(w => w.IsUnlocked);

        /// <summary>Total de waypoints registrados actualmente.</summary>
        public static int Count => _waypoints.Count;

        /// <summary>Elimina todos los waypoints del registro (útil al cambiar de escena).</summary>
        public static void Clear() => _waypoints.Clear();
    }
}
