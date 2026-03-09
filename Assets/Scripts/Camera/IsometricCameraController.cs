using UnityEngine;
using Managers;

namespace Camera
{
    
    public class IsometricCameraController : MonoBehaviour
    {
        public static IsometricCameraController Instance { get; private set; }

        [Header("Configuración")]
        [SerializeField] private CameraSettings settings;

        [Header("Objetivo Manual (Fallback)")]
        [Tooltip("Si no hay PlayerPartyManager, seguir a este transform")]
        [SerializeField] private Transform manualTarget;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;

        private CameraMode currentMode;
        private CameraMode requestedMode;
        private bool inCombat = false;

        private bool isTransitioning = false;
        private float transitionTimer = 0f;
        private Vector3 transitionStartPos;
        private Quaternion transitionStartRot;

        private Transform currentTarget;

        // Estado isométrico
        private float isoZoom;
        private float isoTargetZoom;
        private float isoYaw;
        private float isoTargetYaw;
        private Vector3 isoCurrentPos;
        private Vector3 isoCurrentLookAt;   // punto suavizado al que mira la cámara
        private bool isoIsRotating;
        private float isoLastMouseX;

        // Estado tercera persona
        private float tpZoom;
        private float tpTargetZoom;
        private float tpYaw;
        private float tpTargetYaw;
        private float tpPitch;
        private float tpTargetPitch;
        private Vector3 tpCurrentPos;
        private float tpLastMouseX;         
        private float tpLastMouseY;

        // ── Cache ────────────────────────────────────────────────────────
        private UnityEngine.Camera mainCamera;

        // ── Propiedades públicas ─────────────────────────────────────────
        public CameraMode CurrentMode    => currentMode;
        public bool        InCombat      => inCombat;
        public Transform   CurrentTarget => currentTarget;
        /// <summary>Yaw actual del modo activo (usado por GameInputManager para orientar el movimiento).</summary>
        public float       CurrentYaw    => currentMode == CameraMode.Isometric ? isoYaw : tpYaw;

        // ══════════════════════════════════════════════════════════════════
        // Unity Lifecycle
        // ══════════════════════════════════════════════════════════════════

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            mainCamera = GetComponent<UnityEngine.Camera>();
            if (mainCamera == null)
                mainCamera = GetComponentInChildren<UnityEngine.Camera>();

            InitValues();
        }

        void Start()
        {
            EventBus.Suscribir<EventoCombateIniciado>(OnCombateIniciado);
            EventBus.Suscribir<EventoCombateFinalizado>(OnCombateFinalizado);

            if (PlayerPartyManager.Instance != null)
            {
                currentTarget = PlayerPartyManager.Instance.MainTransform;
                PlayerPartyManager.Instance.OnMainChanged += OnMainCharacterChanged;
                Debug.Log($"[Camera] Siguiendo a: {PlayerPartyManager.Instance.MainCharacter?.Nombre_Entidad ?? "null"}");
            }
            else if (manualTarget != null)
            {
                currentTarget = manualTarget;
                Debug.Log($"[Camera] Modo manual, objetivo: {manualTarget.name}");
            }
            else
            {
                Debug.LogWarning("[Camera] ⚠ No hay objetivo para seguir.");
            }

            if (currentTarget != null)
                SnapToTarget();
        }

        void OnDestroy()
        {
            EventBus.Desuscribir<EventoCombateIniciado>(OnCombateIniciado);
            EventBus.Desuscribir<EventoCombateFinalizado>(OnCombateFinalizado);

            if (PlayerPartyManager.Instance != null)
                PlayerPartyManager.Instance.OnMainChanged -= OnMainCharacterChanged;

            if (Instance == this)
                Instance = null;
        }

        void LateUpdate()
        {
            if (currentTarget == null || settings == null) return;

            HandleModeToggleInput();

            if (isTransitioning)
            {
                UpdateTransition();
                return;
            }

            switch (currentMode)
            {
                case CameraMode.Isometric:
                    HandleIsoInput();
                    UpdateIsoZoom();
                    UpdateIsoRotation();
                    UpdateIsoPosition();
                    break;

                case CameraMode.ThirdPerson:
                    HandleTpInput();
                    UpdateTpZoom();
                    UpdateTpRotation();
                    UpdateTpPosition();
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Inicialización
        // ══════════════════════════════════════════════════════════════════

        private void InitValues()
        {
            if (settings == null)
            {
                isoZoom       = isoTargetZoom = 12f;
                isoYaw        = isoTargetYaw  = 45f;
                tpZoom        = tpTargetZoom  = 6f;
                tpYaw         = tpTargetYaw   = 0f;
                tpPitch       = tpTargetPitch = 18f;
                currentMode   = requestedMode = CameraMode.ThirdPerson;
                return;
            }

            isoZoom       = isoTargetZoom = settings.defaultZoomDistance;
            isoYaw        = isoTargetYaw  = settings.initialYawAngle;
            tpZoom        = tpTargetZoom  = settings.tpDistance;
            tpYaw         = tpTargetYaw   = 0f;
            tpPitch       = tpTargetPitch = settings.tpPitchAngle;
            currentMode   = requestedMode = settings.defaultMode;
        }

        // ══════════════════════════════════════════════════════════════════
        // Eventos de combate
        // ══════════════════════════════════════════════════════════════════

        private void OnCombateIniciado(EventoCombateIniciado evt)
        {
            inCombat = true;
            Debug.Log("[Camera] ⚔ Combate iniciado → forzando modo Isométrico.");
            SetMode(CameraMode.Isometric, smooth: true);
        }

        private void OnCombateFinalizado(EventoCombateFinalizado evt)
        {
            inCombat = false;
            Debug.Log("[Camera] ✔ Combate finalizado → modo libre.");
            SetMode(settings != null ? settings.defaultMode : CameraMode.ThirdPerson, smooth: true);
        }

        // ══════════════════════════════════════════════════════════════════
        // Toggle de modo (fuera de combate)
        // ══════════════════════════════════════════════════════════════════

        private void HandleModeToggleInput()
        {
            if (inCombat) return;
            if (settings == null) return;
            if (isTransitioning) return;

            if (Input.GetKeyDown(settings.toggleModeKey))
            {
                CameraMode next = currentMode == CameraMode.Isometric
                    ? CameraMode.ThirdPerson
                    : CameraMode.Isometric;
                SetMode(next, smooth: true);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Cambio de modo
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Solicita un cambio de modo. Si smooth=true hace transición interpolada.
        /// En combate solo se acepta Isometric.
        /// </summary>
        public void SetMode(CameraMode mode, bool smooth = false)
        {
            if (inCombat && mode != CameraMode.Isometric)
            {
                Debug.LogWarning("[Camera] No se puede cambiar a ThirdPerson durante el combate.");
                return;
            }

            if (mode == currentMode && !isTransitioning) return;

            requestedMode = mode;

            if (mode == CameraMode.ThirdPerson && settings != null && settings.tpSnapBehindOnEnter && currentTarget != null)
            {
                Vector3 forward = currentTarget.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.001f)
                {
                    tpYaw = tpTargetYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg + 180f;
                }
            }

            // Evitar saltos de input por delta grande al entrar al modo.
            if (mode == CameraMode.ThirdPerson)
            {
                tpLastMouseX = Input.mousePosition.x;
                tpLastMouseY = Input.mousePosition.y;

                if (settings != null)
                {
                    tpPitch = tpTargetPitch = Mathf.Clamp(tpPitch, settings.tpMinPitchAngle, settings.tpMaxPitchAngle);
                }
            }

            if (smooth && settings != null && settings.modeTransitionDuration > 0f)
            {
                transitionStartPos = transform.position;
                transitionStartRot = transform.rotation;
                transitionTimer    = 0f;
                isTransitioning    = true;
                currentMode        = mode;
            }
            else
            {
                currentMode     = mode;
                isTransitioning = false;
                SnapToTarget();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Transición
        // ══════════════════════════════════════════════════════════════════

        private void UpdateTransition()
        {
            float duration = settings != null ? settings.modeTransitionDuration : 0.4f;
            transitionTimer += Time.deltaTime;
            float t = Mathf.Clamp01(transitionTimer / duration);
            t = t * t * (3f - 2f * t); // smoothstep

            Vector3    targetPos;
            Quaternion targetRot;
            CalculateDesiredTransform(out targetPos, out targetRot);

            transform.position = Vector3.Lerp(transitionStartPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(transitionStartRot, targetRot, t);

            if (transitionTimer >= duration)
            {
                transform.position = targetPos;
                transform.rotation = targetRot;
                isTransitioning    = false;

                if (currentMode == CameraMode.Isometric)
                    isoCurrentPos = targetPos;
                else
                    tpCurrentPos = targetPos;
            }
        }

        /// <summary>Calcula la posición y rotación ideal para el modo activo sin suavizado.</summary>
        private void CalculateDesiredTransform(out Vector3 pos, out Quaternion rot)
        {
            if (currentMode == CameraMode.Isometric)
            {
                pos = CalcIsoPosition();
                rot = CalcIsoRotation();
            }
            else
            {
                pos = CalcTpPosition();
                rot = CalcTpRotation();
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // ── MODO ISOMÉTRICO ───────────────────────────────────────────────
        // ══════════════════════════════════════════════════════════════════

        private void HandleIsoInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                isoTargetZoom -= scroll * settings.zoomSpeed;
                isoTargetZoom  = Mathf.Clamp(isoTargetZoom, settings.minZoomDistance, settings.maxZoomDistance);
            }

            if (!settings.allowRotation) return;

            // Q/E rotación
            if (Input.GetKey(KeyCode.Q)) isoTargetYaw -= settings.rotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) isoTargetYaw += settings.rotationSpeed * Time.deltaTime;

            // Mouse rotación isométrica: requiere mantener click derecho (comportamiento original)
            if (settings.mouseRotation)
            {
                if (Input.GetMouseButtonDown(1)) { isoIsRotating = true;  isoLastMouseX = Input.mousePosition.x; }
                if (Input.GetMouseButtonUp(1))   { isoIsRotating = false; }

                if (isoIsRotating)
                {
                    float dx = Input.mousePosition.x - isoLastMouseX;
                    isoTargetYaw += dx * settings.mouseRotationSensitivity;
                    isoLastMouseX = Input.mousePosition.x;
                }
            }

            if (isoTargetYaw >  360f) isoTargetYaw -= 360f;
            if (isoTargetYaw <    0f) isoTargetYaw += 360f;
        }

        private void UpdateIsoZoom()
        {
            isoZoom = Mathf.Lerp(isoZoom, isoTargetZoom, Time.deltaTime * settings.zoomSmoothing);
        }

        private void UpdateIsoRotation()
        {
            isoYaw = Mathf.LerpAngle(isoYaw, isoTargetYaw, Time.deltaTime * settings.zoomSmoothing);
        }

        private void UpdateIsoPosition()
        {
            Vector3 desired = CalcIsoPosition();
            Vector3 desiredLookAt = IsoTargetPoint();

            isoCurrentPos    = Vector3.Lerp(isoCurrentPos, desired, Time.deltaTime * settings.followSmoothing);
            isoCurrentLookAt = Vector3.Lerp(isoCurrentLookAt, desiredLookAt, Time.deltaTime * settings.followSmoothing);

            transform.position = isoCurrentPos;

            Vector3 lookDir = isoCurrentLookAt - isoCurrentPos;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir);
        }

        private Vector3 CalcIsoPosition()
        {
            float pitchRad = settings.pitchAngle * Mathf.Deg2Rad;
            float yawRad   = isoYaw * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            ) * isoZoom;

            return IsoTargetPoint() + offset;
        }

        private Quaternion CalcIsoRotation()
        {
            Vector3 pos = CalcIsoPosition();
            if ((IsoTargetPoint() - pos).sqrMagnitude < 0.0001f) return transform.rotation;
            return Quaternion.LookRotation(IsoTargetPoint() - pos);
        }

        private Vector3 IsoTargetPoint()
        {
            Vector3 p = currentTarget.position + Vector3.up * settings.targetHeightOffset;
            if (settings.useBounds)
            {
                p.x = Mathf.Clamp(p.x, settings.boundsMin.x, settings.boundsMax.x);
                p.z = Mathf.Clamp(p.z, settings.boundsMin.y, settings.boundsMax.y);
            }
            return p;
        }

        // ══════════════════════════════════════════════════════════════════
        // ── MODO TERCERA PERSONA ──────────────────────────────────────────
        // ══════════════════════════════════════════════════════════════════

        private void HandleTpInput()
        {
            // Zoom scroll
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                tpTargetZoom -= scroll * settings.tpZoomSpeed;
                tpTargetZoom  = Mathf.Clamp(tpTargetZoom, settings.tpMinDistance, settings.tpMaxDistance);
            }

            // Q/E rotación orbital
            if (Input.GetKey(KeyCode.Q)) tpTargetYaw -= settings.tpRotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) tpTargetYaw += settings.tpRotationSpeed * Time.deltaTime;

            // Mouse look (yaw/pitch) sin necesidad de mantener click.
            bool allowYaw   = settings.tpMouseRotation;
            bool allowPitch = settings.tpMousePitch;
            if (allowYaw || allowPitch)
            {
                float dx = Input.mousePosition.x - tpLastMouseX;
                float dy = Input.mousePosition.y - tpLastMouseY;

                if (allowYaw && Mathf.Abs(dx) > 0.01f)
                    tpTargetYaw += dx * settings.tpMouseRotationSensitivity;

                if (allowPitch && Mathf.Abs(dy) > 0.01f)
                {
                    // Mouse arriba = dy>0 → reducir depresión (mirar más "arriba")
                    tpTargetPitch -= dy * settings.tpMousePitchSensitivity;
                    tpTargetPitch = Mathf.Clamp(tpTargetPitch, settings.tpMinPitchAngle, settings.tpMaxPitchAngle);
                }
            }
            // Siempre actualizamos la posición guardada del mouse
            tpLastMouseX = Input.mousePosition.x;
            tpLastMouseY = Input.mousePosition.y;

            if (tpTargetYaw >  360f) tpTargetYaw -= 360f;
            if (tpTargetYaw <    0f) tpTargetYaw += 360f;
        }

        private void UpdateTpZoom()
        {
            tpZoom = Mathf.Lerp(tpZoom, tpTargetZoom, Time.deltaTime * settings.tpZoomSmoothing);
        }

        private void UpdateTpRotation()
        {
            // FIX: usa tpRotationSmoothing dedicado, separado del tpZoomSmoothing
            tpYaw = Mathf.LerpAngle(tpYaw, tpTargetYaw, Time.deltaTime * settings.tpRotationSmoothing);

            // Suavizado del pitch
            tpPitch = Mathf.Lerp(tpPitch, tpTargetPitch, Time.deltaTime * settings.tpPitchSmoothing);
        }

        private void UpdateTpPosition()
        {
            Vector3 desired = CalcTpPosition();
            tpCurrentPos    = Vector3.Lerp(tpCurrentPos, desired, Time.deltaTime * settings.tpFollowSmoothing);

            // ── Anti-clip: raycast desde el foco hacia la posición deseada ──
            Vector3 focusPoint = currentTarget.position + Vector3.up * settings.tpTargetHeightOffset;
            Vector3 dir = tpCurrentPos - focusPoint;
            float dist = dir.magnitude;

            if (dist > 0.01f)
            {
                // Usar una esfera pequeña para evitar raspar paredes
                if (Physics.SphereCast(focusPoint, 0.25f, dir.normalized, out RaycastHit hit, dist,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    // Acercar la cámara justo antes del obstáculo
                    tpCurrentPos = focusPoint + dir.normalized * (hit.distance - 0.1f);
                }
            }

            transform.position = tpCurrentPos;
            transform.rotation = CalcTpRotation();
        }

        private Vector3 CalcTpPosition()
        {
            float pitchRad = tpPitch * Mathf.Deg2Rad;
            float yawRad   = tpYaw * Mathf.Deg2Rad;

            Vector3 dir = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            );

            Vector3 focusPoint = currentTarget.position + Vector3.up * settings.tpTargetHeightOffset;
            return focusPoint + dir * tpZoom;
        }

        private Quaternion CalcTpRotation()
        {
            Vector3 focusPoint = currentTarget.position + Vector3.up * settings.tpTargetHeightOffset;
            Vector3 dir        = focusPoint - CalcTpPosition();
            if (dir.sqrMagnitude < 0.0001f) return transform.rotation;
            return Quaternion.LookRotation(dir);
        }

        // ══════════════════════════════════════════════════════════════════
        // API Pública
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Posiciona la cámara inmediatamente sin suavizado en el modo actual.</summary>
        public void SnapToTarget()
        {
            if (currentTarget == null || settings == null) return;

            if (currentMode == CameraMode.Isometric)
            {
                isoCurrentPos      = CalcIsoPosition();
                isoCurrentLookAt   = IsoTargetPoint();
                transform.position = isoCurrentPos;
                transform.LookAt(isoCurrentLookAt);
            }
            else
            {
                tpCurrentPos       = CalcTpPosition();
                transform.position = tpCurrentPos;
                transform.rotation = CalcTpRotation();
            }
        }

        /// <summary>Cambia el objetivo seguido por la cámara.</summary>
        public void SetTarget(Transform newTarget) => currentTarget = newTarget;

        /// <summary>Ajusta el zoom del modo isométrico programáticamente.</summary>
        public void SetIsoZoom(float zoom, bool instant = false)
        {
            isoTargetZoom = settings != null
                ? Mathf.Clamp(zoom, settings.minZoomDistance, settings.maxZoomDistance)
                : zoom;
            if (instant) isoZoom = isoTargetZoom;
        }

        /// <summary>Ajusta el zoom del modo tercera persona programáticamente.</summary>
        public void SetTpZoom(float zoom, bool instant = false)
        {
            tpTargetZoom = settings != null
                ? Mathf.Clamp(zoom, settings.tpMinDistance, settings.tpMaxDistance)
                : zoom;
            if (instant) tpZoom = tpTargetZoom;
        }

        /// <summary>Ajusta la rotación del modo activo programáticamente.</summary>
        public void SetRotation(float yaw, bool instant = false)
        {
            if (currentMode == CameraMode.Isometric)
            {
                isoTargetYaw = yaw;
                if (instant) isoYaw = yaw;
            }
            else
            {
                tpTargetYaw = yaw;
                if (instant) tpYaw = yaw;
            }
        }

        /// <summary>Resetea la cámara a los valores por defecto del SO.</summary>
        public void ResetCamera()
        {
            if (settings == null) return;
            isoTargetZoom = settings.defaultZoomDistance;
            isoTargetYaw  = settings.initialYawAngle;
            tpTargetZoom  = settings.tpDistance;
        }

        /// <summary>Convierte posición del mundo a coordenadas de pantalla.</summary>
        public Vector3 WorldToScreenPoint(Vector3 worldPosition)
            => mainCamera != null ? mainCamera.WorldToScreenPoint(worldPosition) : Vector3.zero;

        /// <summary>Ray desde la posición del mouse.</summary>
        public Ray GetMouseRay()
            => mainCamera != null ? mainCamera.ScreenPointToRay(Input.mousePosition) : default;

        /// <summary>
        /// Vector XZ "adelante" relativo al yaw activo, para orientar el input WASD.
        /// ISO: relativo a posición XZ de la cámara. TP: dirección en que mira la cámara.
        /// Fórmula: (-sin(yaw), 0, -cos(yaw)) — con yaw=0° W mueve en -Z (hacia dentro de pantalla).
        /// </summary>
        public Vector3 GetMovementForward()
        {
            float yawRad = CurrentYaw * Mathf.Deg2Rad;
            return new Vector3(-Mathf.Sin(yawRad), 0f, -Mathf.Cos(yawRad));
        }

        /// <summary>
        /// Vector XZ "derecha" relativo al yaw activo, para orientar el input WASD.
        /// La cámara offset = (sin(yaw), …, cos(yaw)), mira hacia el target → su
        /// derecha real es (-cos(yaw), 0, sin(yaw)).
        /// </summary>
        public Vector3 GetMovementRight()
        {
            float yawRad = CurrentYaw * Mathf.Deg2Rad;
            return new Vector3(-Mathf.Cos(yawRad), 0f, Mathf.Sin(yawRad));
        }

        // ══════════════════════════════════════════════════════════════════
        // Callbacks internos
        // ══════════════════════════════════════════════════════════════════

        private void OnMainCharacterChanged(EntityController oldMain, EntityController newMain)
        {
            if (newMain != null)
            {
                currentTarget = newMain.transform;
                Debug.Log($"[Camera] Nuevo objetivo: {newMain.Nombre_Entidad}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // Gizmos
        // ══════════════════════════════════════════════════════════════════

        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || currentTarget == null || settings == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);

            Gizmos.color = currentMode == CameraMode.Isometric ? Color.cyan : Color.green;
            float hOffset = currentMode == CameraMode.Isometric ? settings.targetHeightOffset : settings.tpTargetHeightOffset;
            Gizmos.DrawWireSphere(currentTarget.position + Vector3.up * hOffset, 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"[Camera] {currentMode}{(inCombat ? " (COMBATE)" : "")}");
#endif

            if (currentMode == CameraMode.Isometric && settings.useBounds)
            {
                Gizmos.color = Color.red;
                Vector3 center = new Vector3(
                    (settings.boundsMin.x + settings.boundsMax.x) / 2f, 0f,
                    (settings.boundsMin.y + settings.boundsMax.y) / 2f);
                Vector3 size = new Vector3(
                    settings.boundsMax.x - settings.boundsMin.x, 1f,
                    settings.boundsMax.y - settings.boundsMin.y);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}