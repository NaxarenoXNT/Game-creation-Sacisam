using UnityEngine;
using Managers;

namespace World.Travel
{
    /// <summary>
    /// Componente de escena que representa un punto de viaje rápido en el mundo.
    ///
    /// Coloca este MonoBehaviour en cualquier GameObject que actúe como waypoint.
    /// Si <see cref="useTransformPosition"/> está activo, la posición de destino es la
    /// del propio Transform; de lo contrario, usa la worldPosition del ScriptableObject.
    ///
    /// El WaypointMarker se auto-registra en el WaypointRegistry al despertar y se
    /// desregistra al destruirse.
    /// </summary>
    public class WaypointMarker : MonoBehaviour
    {
        [Header("Datos del Waypoint")]
        [SerializeField] private WaypointDataSO data;

        [Tooltip("Si está activo, la posición de destino es la del Transform de este GameObject " +
                 "(ignora WaypointDataSO.worldPosition). Recomendado en la mayoría de casos.")]
        [SerializeField] private bool useTransformPosition = true;

        // ── Propiedades públicas ─────────────────────────────────────────────────

        /// <summary>ID único del waypoint. Usa el nombre del GameObject como fallback.</summary>
        public string WaypointId   => data != null && !string.IsNullOrWhiteSpace(data.waypointId)
                                          ? data.waypointId
                                          : gameObject.name;

        /// <summary>Nombre legible para la UI.</summary>
        public string DisplayName  => data != null && !string.IsNullOrWhiteSpace(data.displayName)
                                          ? data.displayName
                                          : gameObject.name;

        /// <summary>Descripción para tooltips de UI.</summary>
        public string Description  => data?.description ?? string.Empty;

        /// <summary>Posición real a la que se teletransportará el jugador.</summary>
        public Vector3 TravelPosition => useTransformPosition
                                             ? transform.position
                                             : (data != null ? data.worldPosition : transform.position);

        /// <summary>Si el jugador puede usar este waypoint como destino.</summary>
        public bool IsUnlocked
        {
            get  => data != null && data.isUnlocked;
            private set { if (data != null) data.isUnlocked = value; }
        }

        // ── Unity Lifecycle ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            WaypointRegistry.Register(this);
        }

        private void OnDisable()
        {
            WaypointRegistry.Unregister(this);
        }

        // ── API Pública ──────────────────────────────────────────────────────────

        /// <summary>
        /// Desbloquea este waypoint y notifica al resto del juego.
        /// Llama esto desde cualquier sistema de progresión (quests, exploración, etc.).
        /// </summary>
        public void Unlock()
        {
            if (IsUnlocked) return;

            IsUnlocked = true;

            EventBus.Publicar(new EventoWaypointDesbloqueado
            {
                WaypointId     = WaypointId,
                NombreWaypoint = DisplayName
            });

            Debug.Log($"[WaypointMarker] Waypoint desbloqueado: '{DisplayName}' ({WaypointId})");
        }

        /// <summary>
        /// Construye un TravelRequest hacia este waypoint listo para pasarle al TravelManager.
        /// </summary>
        public TravelRequest BuildRequest() => TravelRequest.FromWaypoint(this);

        // ── Gizmos ───────────────────────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = IsUnlocked ? Color.cyan : Color.gray;
            Gizmos.DrawWireSphere(TravelPosition, 1f);
            Gizmos.DrawIcon(TravelPosition + Vector3.up * 2f, "d_NavMeshAgent Icon", true);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(TravelPosition, 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                TravelPosition + Vector3.up * 2.5f,
                $"{DisplayName}\n[{WaypointId}] {(IsUnlocked ? "✓" : "🔒")}");
#endif
        }
    }
}
