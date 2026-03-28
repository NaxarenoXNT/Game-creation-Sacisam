using System.Collections;
using UnityEngine;
using Managers;
using GameFlow;
using Camera;

namespace World.Travel
{
    /// <summary>
    /// Orquestador central del sistema de viaje rápido (Fast Travel).
    ///
    /// Responsabilidades:
    ///   - Recibir solicitudes de viaje (desde UI via EventBus o directo via API).
    ///   - Validar que el viaje sea posible (estado del juego, restricciones).
    ///   - Ejecutar el pipeline completo de teleport de forma coordinada.
    ///   - Publicar eventos de ciclo de vida para que otros sistemas reaccionen.
    ///
    /// Pipeline (en orden):
    ///   1. Validación (estado, restricciones)
    ///   2. Notificar inicio (EventoTravelIniciado)
    ///   3. Push TravelFlowState → bloquea input
    ///   4. Fade Out
    ///   5. Descargar chunks actuales
    ///   6. Teleportar main character
    ///   7. Reubicar party activo
    ///   8. Cargar chunks en destino (con timeout)
    ///   9. Snap de cámara al nuevo objetivo
    ///  10. Fade In
    ///  11. Pop TravelFlowState → restaura exploración
    ///  12. Notificar finalización (EventoTravelCompletado)
    ///
    /// Integración:
    ///   - Inyecta un IFadeController para fade de pantalla (opcional pero recomendado).
    ///   - Si no se inyecta, el pipeline continúa sin fade (útil en desarrollo).
    ///   - Los handlers de party y chunks tienen implementaciones por defecto y
    ///     pueden reemplazarse mediante inyección de dependencias.
    /// </summary>
    public class TravelManager : MonoBehaviour
    {
        public static TravelManager Instance { get; private set; }

        [Header("Tiempos del Pipeline")]
        [Tooltip("Duración del fade a negro antes de teletransportar (segundos).")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Tooltip("Duración del fade de vuelta desde negro tras el teleport (segundos).")]
        [SerializeField] private float fadeInDuration  = 0.5f;

        [Tooltip("Tiempo máximo de espera para que los chunks del destino se carguen.")]
        [SerializeField] private float chunkLoadTimeout = 8f;

        [Header("Formación del Party")]
        [Tooltip("Radio del arco de formación de los compañeros alrededor del main.")]
        [SerializeField] private float partySpreadRadius = 3f;

        [Header("Estado (solo lectura en Inspector)")]
        [SerializeField] private bool isTraveling;

        // ── Dependencias (inyectables) ───────────────────────────────────────────
        private IFadeController    _fadeController;
        private ITravelPartyHandler _partyHandler;
        private ITravelChunkHandler _chunkHandler;

        // ── Propiedades públicas ─────────────────────────────────────────────────

        /// <summary>True mientras el pipeline de viaje está en ejecución.</summary>
        public bool IsTraveling => isTraveling;

        // ══════════════════════════════════════════════════════════════════════════
        // Unity Lifecycle
        // ══════════════════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Inicializar handlers con implementaciones por defecto si no se inyectaron.
            _partyHandler ??= new TravelPartyHandler(radius: partySpreadRadius);
            _chunkHandler ??= new TravelChunkHandler();

            EventBus.Suscribir<EventoTravelSolicitado>(OnTravelRequested);
        }

        private void OnDestroy()
        {
            EventBus.Desuscribir<EventoTravelSolicitado>(OnTravelRequested);

            if (Instance == this)
                Instance = null;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Inyección de Dependencias
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Inyecta el controlador de fade de pantalla.
        /// Ideal llamar desde un bootstrap o Awake de un ScreenFadeController.
        /// </summary>
        public void SetFadeController(IFadeController fadeController)
        {
            _fadeController = fadeController;
            Debug.Log($"[TravelManager] IFadeController registrado: {fadeController?.GetType().Name}");
        }

        /// <summary>Reemplaza la estrategia de reposicionamiento del party.</summary>
        public void SetPartyHandler(ITravelPartyHandler handler) => _partyHandler = handler;

        /// <summary>Reemplaza la estrategia de gestión de chunks.</summary>
        public void SetChunkHandler(ITravelChunkHandler handler) => _chunkHandler = handler;

        // ══════════════════════════════════════════════════════════════════════════
        // API Pública
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Solicita un viaje rápido al destino definido en <paramref name="request"/>.
        /// Si la validación falla, publica EventoTravelCancelado y retorna sin hacer nada.
        /// </summary>
        public void RequestTravel(TravelRequest request)
        {
            if (!ValidateTravel(request, out string reason))
            {
                Debug.LogWarning($"[TravelManager] Viaje rechazado → {reason}");
                EventBus.Publicar(new EventoTravelCancelado { Request = request, Razon = reason });
                return;
            }

            StartCoroutine(TravelPipeline(request));
        }

        /// <summary>
        /// Atajo directo: construye un TravelRequest desde un WaypointMarker y lo envía.
        /// </summary>
        public void TravelTo(WaypointMarker waypoint)
        {
            if (waypoint == null)
            {
                Debug.LogWarning("[TravelManager] TravelTo recibió un waypoint nulo.");
                return;
            }

            if (!waypoint.IsUnlocked)
            {
                Debug.LogWarning($"[TravelManager] El waypoint '{waypoint.WaypointId}' está bloqueado.");
                EventBus.Publicar(new EventoTravelCancelado
                {
                    Request = null,
                    Razon   = $"El waypoint '{waypoint.DisplayName}' no está desbloqueado."
                });
                return;
            }

            RequestTravel(waypoint.BuildRequest());
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Validación
        // ══════════════════════════════════════════════════════════════════════════

        private bool ValidateTravel(TravelRequest request, out string reason)
        {
            if (isTraveling)
            {
                reason = "Ya hay un viaje en progreso.";
                return false;
            }

            if (request == null)
            {
                reason = "TravelRequest nulo.";
                return false;
            }

            var flowController = GameFlowController.Instance;

            // No se puede viajar durante el combate.
            if (flowController != null && flowController.IsInState<CombatFlowState>())
            {
                reason = "No se puede viajar durante el combate.";
                return false;
            }

            // Solo se puede viajar en modo exploración.
            if (flowController != null && !flowController.IsInState<ExplorationFlowState>())
            {
                reason = $"No se puede viajar en el estado actual ({flowController.CurrentState?.GetType().Name}).";
                return false;
            }

            // Se necesita un personaje principal.
            if (PlayerPartyManager.Instance == null || PlayerPartyManager.Instance.MainCharacter == null)
            {
                reason = "No hay personaje principal disponible.";
                return false;
            }

            reason = null;
            return true;
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Pipeline de Teleport
        // ══════════════════════════════════════════════════════════════════════════

        private IEnumerator TravelPipeline(TravelRequest request)
        {
            isTraveling = true;
            Debug.Log($"[TravelManager] ═══ Iniciando viaje: '{request.DisplayName}' → {request.Destination} ═══");

            // ── Paso 1: Notificar inicio ─────────────────────────────────────────
            EventBus.Publicar(new EventoTravelIniciado { Request = request });

            // ── Paso 2: Bloquear input (TravelFlowState) ─────────────────────────
            var travelState = new TravelFlowState();
            GameFlowController.Instance?.Push(travelState);

            // Dar un frame para que el contexto de input se aplique.
            yield return null;

            // ── Paso 3: Fade Out ─────────────────────────────────────────────────
            if (_fadeController != null)
            {
                yield return StartCoroutine(_fadeController.FadeOut(fadeOutDuration));
            }
            else
            {
                Debug.LogWarning("[TravelManager] No hay IFadeController. El viaje continúa sin fade. " +
                                 "Inyecta uno con TravelManager.SetFadeController().");
                yield return new WaitForSecondsRealtime(0.1f);
            }

            // ── Paso 4: Preparar chunks (descargar los actuales) ─────────────────
            _chunkHandler?.PrepareForTravel(request.Destination);

            // ── Paso 5: Teleportar personaje principal ───────────────────────────
            var main = PlayerPartyManager.Instance.MainCharacter;
            TeleportTransform(main.transform, request.Destination);
            Debug.Log($"[TravelManager] Main character teleportado a {request.Destination}.");

            // ── Paso 6: Cargar chunks en destino (antes de reubicar party) ───────
            if (_chunkHandler != null)
            {
                yield return StartCoroutine(
                    _chunkHandler.WaitForChunksLoaded(request.Destination, chunkLoadTimeout));
            }

            // ── Paso 7: Reubicar party activo (raycast con terreno ya cargado) ───
            _partyHandler?.RepositionParty(request.Destination);

            // ── Paso 8: Sincronizar cámara ───────────────────────────────────────
            var camera = IsometricCameraController.Instance;
            if (camera != null)
            {
                camera.SnapToTarget();
                Debug.Log("[TravelManager] Cámara sincronizada con nuevo destino.");
            }

            // Un frame extra para que la cámara consolide su posición antes del fade in.
            yield return null;

            // ── Paso 9: Fade In ──────────────────────────────────────────────────
            if (_fadeController != null)
                yield return StartCoroutine(_fadeController.FadeIn(fadeInDuration));
            else
                yield return new WaitForSecondsRealtime(0.1f);

            // ── Paso 10: Restaurar exploración (Pop TravelFlowState) ─────────────
            GameFlowController.Instance?.Pop();

            // ── Paso 11: Notificar finalización ──────────────────────────────────
            EventBus.Publicar(new EventoTravelCompletado
            {
                Request      = request,
                PosicionFinal = request.Destination
            });

            request.OnCompleted?.Invoke();
            isTraveling = false;

            Debug.Log($"[TravelManager] ═══ Viaje completado: '{request.DisplayName}' ═══");
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Manejadores de EventBus
        // ══════════════════════════════════════════════════════════════════════════

        private void OnTravelRequested(EventoTravelSolicitado evt)
        {
            RequestTravel(evt.Request);
        }

        // ══════════════════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Teleporta un Transform deshabilitando temporalmente el CharacterController
        /// si lo tiene, para que Unity no ignore la asignación de posición.
        /// </summary>
        private static void TeleportTransform(Transform t, Vector3 destination)
        {
            if (t.TryGetComponent<CharacterController>(out var cc))
            {
                cc.enabled = false;
                t.position = destination;
                cc.enabled = true;
            }
            else
            {
                t.position = destination;
            }
        }
    }
}
