using UnityEngine;

namespace World.Travel
{
    /// <summary>
    /// ScriptableObject que define los datos estáticos de un waypoint.
    /// Úsalo como semilla para WaypointMarker o para registrar waypoints
    /// que no tienen representación física en la escena.
    ///
    /// Crear en: Assets > Create > Saclisam > World > Waypoint Data
    /// </summary>
    [CreateAssetMenu(fileName = "WaypointData", menuName = "Saclisam/World/Waypoint Data")]
    public class WaypointDataSO : ScriptableObject
    {
        [Tooltip("Identificador único. Debe ser estable entre sesiones (se usa en guardado/desbloqueo).")]
        public string waypointId;

        [Tooltip("Nombre que verá el jugador en la UI del mapa.")]
        public string displayName;

        [Tooltip("Posición en el mundo. Ignorada si el WaypointMarker usa useTransformPosition = true.")]
        public Vector3 worldPosition;

        [Tooltip("Si el waypoint está disponible para viajar desde el inicio.")]
        public bool isUnlocked = false;

        [Tooltip("Descripción opcional para tooltips de UI.")]
        [TextArea(2, 4)]
        public string description;
    }
}
