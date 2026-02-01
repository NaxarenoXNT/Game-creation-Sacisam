using UnityEngine;
using Managers;

namespace Camera
{
    /// <summary>
    /// Controlador de cámara isométrica que sigue al Main character.
    /// Se integra con PlayerPartyManager para seguir dinámicamente al personaje principal.
    /// </summary>
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
        
        // Estado actual
        private Transform currentTarget;
        private float currentZoom;
        private float targetZoom;
        private float currentYaw;
        private float targetYaw;
        private Vector3 currentPosition;
        
        // Input
        private bool isRotating;
        private float lastMouseX;
        
        // Cache
        private UnityEngine.Camera mainCamera;
        
        public Transform CurrentTarget => currentTarget;
        public float CurrentZoom => currentZoom;
        public float CurrentYaw => currentYaw;
        
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
            {
                mainCamera = GetComponentInChildren<UnityEngine.Camera>();
            }
            
            // Valores iniciales
            if (settings != null)
            {
                currentZoom = settings.defaultZoomDistance;
                targetZoom = settings.defaultZoomDistance;
                currentYaw = settings.initialYawAngle;
                targetYaw = settings.initialYawAngle;
            }
            else
            {
                currentZoom = 12f;
                targetZoom = 12f;
                currentYaw = 45f;
                targetYaw = 45f;
            }
        }
        
        void Start()
        {
            // Intentar obtener el Main de PlayerPartyManager
            if (PlayerPartyManager.Instance != null)
            {
                currentTarget = PlayerPartyManager.Instance.MainTransform;
                PlayerPartyManager.Instance.OnMainChanged += OnMainCharacterChanged;
                
                Debug.Log($"[IsometricCamera] Siguiendo a: {PlayerPartyManager.Instance.MainCharacter?.Nombre_Entidad ?? "null"}");
            }
            else if (manualTarget != null)
            {
                currentTarget = manualTarget;
                Debug.Log($"[IsometricCamera] Modo manual, siguiendo a: {manualTarget.name}");
            }
            else
            {
                Debug.LogWarning("[IsometricCamera] No hay objetivo para seguir!");
            }
            
            // Posicionar inmediatamente
            if (currentTarget != null)
            {
                SnapToTarget();
            }
        }
        
        void OnDestroy()
        {
            if (PlayerPartyManager.Instance != null)
            {
                PlayerPartyManager.Instance.OnMainChanged -= OnMainCharacterChanged;
            }
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        void LateUpdate()
        {
            if (currentTarget == null) return;
            if (settings == null) return;
            
            HandleInput();
            UpdateZoom();
            UpdateRotation();
            UpdatePosition();
        }
        
        /// <summary>
        /// Procesa el input de rotación y zoom.
        /// </summary>
        private void HandleInput()
        {
            // Zoom con scroll
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                targetZoom -= scrollDelta * settings.zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, settings.minZoomDistance, settings.maxZoomDistance);
            }
            
            if (!settings.allowRotation) return;
            
            // Rotación con Q/E
            if (Input.GetKey(KeyCode.Q))
            {
                targetYaw -= settings.rotationSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                targetYaw += settings.rotationSpeed * Time.deltaTime;
            }
            
            // Rotación con mouse (click derecho)
            if (settings.mouseRotation)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    isRotating = true;
                    lastMouseX = Input.mousePosition.x;
                }
                
                if (Input.GetMouseButtonUp(1))
                {
                    isRotating = false;
                }
                
                if (isRotating)
                {
                    float deltaX = Input.mousePosition.x - lastMouseX;
                    targetYaw += deltaX * settings.mouseRotationSensitivity;
                    lastMouseX = Input.mousePosition.x;
                }
            }
            
            // Normalizar yaw
            if (targetYaw > 360f) targetYaw -= 360f;
            if (targetYaw < 0f) targetYaw += 360f;
        }
        
        /// <summary>
        /// Suaviza el cambio de zoom.
        /// </summary>
        private void UpdateZoom()
        {
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * settings.zoomSmoothing);
        }
        
        /// <summary>
        /// Suaviza la rotación.
        /// </summary>
        private void UpdateRotation()
        {
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * settings.zoomSmoothing);
        }
        
        /// <summary>
        /// Actualiza la posición de la cámara.
        /// </summary>
        private void UpdatePosition()
        {
            // Punto objetivo con offset de altura
            Vector3 targetPoint = currentTarget.position + Vector3.up * settings.targetHeightOffset;
            
            // Aplicar límites si están activos
            if (settings.useBounds)
            {
                targetPoint.x = Mathf.Clamp(targetPoint.x, settings.boundsMin.x, settings.boundsMax.x);
                targetPoint.z = Mathf.Clamp(targetPoint.z, settings.boundsMin.y, settings.boundsMax.y);
            }
            
            // Calcular posición de la cámara en coordenadas esféricas
            float pitchRad = settings.pitchAngle * Mathf.Deg2Rad;
            float yawRad = currentYaw * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            ) * currentZoom;
            
            Vector3 desiredPosition = targetPoint + offset;
            
            // Suavizar movimiento
            currentPosition = Vector3.Lerp(currentPosition, desiredPosition, Time.deltaTime * settings.followSmoothing);
            
            // Aplicar
            transform.position = currentPosition;
            transform.LookAt(targetPoint);
        }
        
        /// <summary>
        /// Posiciona la cámara inmediatamente sin suavizado.
        /// </summary>
        public void SnapToTarget()
        {
            if (currentTarget == null || settings == null) return;
            
            Vector3 targetPoint = currentTarget.position + Vector3.up * settings.targetHeightOffset;
            
            float pitchRad = settings.pitchAngle * Mathf.Deg2Rad;
            float yawRad = currentYaw * Mathf.Deg2Rad;
            
            Vector3 offset = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            ) * currentZoom;
            
            currentPosition = targetPoint + offset;
            transform.position = currentPosition;
            transform.LookAt(targetPoint);
        }
        
        /// <summary>
        /// Callback cuando cambia el Main en PlayerPartyManager.
        /// </summary>
        private void OnMainCharacterChanged(EntityController oldMain, EntityController newMain)
        {
            if (newMain != null)
            {
                currentTarget = newMain.transform;
                Debug.Log($"[IsometricCamera] Nuevo objetivo: {newMain.Nombre_Entidad}");
            }
        }
        
        /// <summary>
        /// Cambia el objetivo manualmente.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            currentTarget = newTarget;
        }
        
        /// <summary>
        /// Ajusta el zoom programáticamente.
        /// </summary>
        public void SetZoom(float zoom, bool instant = false)
        {
            targetZoom = Mathf.Clamp(zoom, settings.minZoomDistance, settings.maxZoomDistance);
            if (instant)
            {
                currentZoom = targetZoom;
            }
        }
        
        /// <summary>
        /// Ajusta la rotación programáticamente.
        /// </summary>
        public void SetRotation(float yaw, bool instant = false)
        {
            targetYaw = yaw;
            if (instant)
            {
                currentYaw = targetYaw;
            }
        }
        
        /// <summary>
        /// Resetea la cámara a valores por defecto.
        /// </summary>
        public void ResetCamera()
        {
            if (settings == null) return;
            
            targetZoom = settings.defaultZoomDistance;
            targetYaw = settings.initialYawAngle;
        }
        
        /// <summary>
        /// Convierte una posición del mundo a posición de pantalla.
        /// Útil para posicionar UI sobre entidades.
        /// </summary>
        public Vector3 WorldToScreenPoint(Vector3 worldPosition)
        {
            return mainCamera != null ? mainCamera.WorldToScreenPoint(worldPosition) : Vector3.zero;
        }
        
        /// <summary>
        /// Obtiene un rayo desde la posición del mouse hacia el mundo.
        /// </summary>
        public Ray GetMouseRay()
        {
            return mainCamera != null ? mainCamera.ScreenPointToRay(Input.mousePosition) : default;
        }
        
        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos || currentTarget == null) return;
            
            // Dibujar línea al objetivo
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
            
            // Dibujar punto objetivo
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget.position + Vector3.up * (settings?.targetHeightOffset ?? 1.5f), 0.3f);
            
            // Dibujar límites si están activos
            if (settings != null && settings.useBounds)
            {
                Gizmos.color = Color.red;
                Vector3 center = new Vector3(
                    (settings.boundsMin.x + settings.boundsMax.x) / 2f,
                    0,
                    (settings.boundsMin.y + settings.boundsMax.y) / 2f
                );
                Vector3 size = new Vector3(
                    settings.boundsMax.x - settings.boundsMin.x,
                    1f,
                    settings.boundsMax.y - settings.boundsMin.y
                );
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
