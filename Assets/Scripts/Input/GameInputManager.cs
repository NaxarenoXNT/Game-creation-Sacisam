using UnityEngine;
using UnityEngine.Events;
using System;
using Camera;

namespace GameInput
{
    /// <summary>
    /// Tipos de contexto para el input.
    /// </summary>
    public enum InputContext
    {
        Exploration,    // Movimiento libre
        Combat,         // En combate, seleccionando acciones
        Menu,           // En menú/UI
        Dialogue        // En diálogo
    }
    
    /// <summary>
    /// Datos del input de movimiento.
    /// </summary>
    public struct MovementInput
    {
        public Vector2 Direction;       // WASD normalizado
        public bool HasInput;           // Si hay input activo
        public Vector3 WorldDirection;  // Dirección relativa a la cámara
    }
    
    /// <summary>
    /// Datos del click en el mundo.
    /// </summary>
    public struct WorldClickData
    {
        public Vector3 Position;        // Posición en el mundo
        public bool DidHit;             // Si el raycast pegó algo
        public GameObject HitObject;    // Objeto clickeado
        public RaycastHit RaycastHit;   // Datos completos del raycast
    }
    
    /// <summary>
    /// Manager central de input. Híbrido WASD + Click.
    /// Cambia comportamiento según contexto (exploración vs combate).
    /// </summary>
    public class GameInputManager : MonoBehaviour
    {
        public static GameInputManager Instance { get; private set; }
        
        [Header("Configuración")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [SerializeField] private LayerMask entityLayer;
        [SerializeField] private LayerMask enemyLayer;
        
        [Header("Estado")]
        [SerializeField] private InputContext currentContext = InputContext.Exploration;
        
        // Eventos de Movimiento
        public event Action<MovementInput> OnMovementInput;
        public event Action OnMovementStop;
        
        // Eventos de Click
        public event Action<WorldClickData> OnWorldClick;          // Click en suelo
        public event Action<EntityController> OnEntityClick;       // Click en entidad aliada
        public event Action<EnemyController> OnEnemyClick;         // Click en enemigo
        public event Action OnEmptyClick;                          // Click en nada
        
        // Eventos de Combate
        public event Action OnCancelAction;                        // Escape / Click derecho
        public event Action OnConfirmAction;                       // Enter / Click izquierdo válido
        
        // Eventos de Cámara
        public event Action<float> OnZoomInput;                    // Scroll
        public event Action<float> OnRotateInput;                  // Q/E
        
        // Estado
        private MovementInput lastMovementInput;
        private bool wasMoving;
        
        public InputContext CurrentContext => currentContext;
        public MovementInput LastMovementInput => lastMovementInput;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        
        void Update()
        {
            // No procesar input si estamos en menú o diálogo
            if (currentContext == InputContext.Menu || currentContext == InputContext.Dialogue)
            {
                return;
            }
            
            ProcessMovementInput();
            ProcessClickInput();
            ProcessCancelInput();
        }
        
        /// <summary>
        /// Procesa el input WASD.
        /// </summary>
        private void ProcessMovementInput()
        {
            // Solo procesar movimiento en exploración
            if (currentContext != InputContext.Exploration) return;
            
            // En modo isométrico el movimiento es con el mouse (click-to-move), no WASD
            if (IsometricCameraController.Instance != null &&
                IsometricCameraController.Instance.CurrentMode == CameraMode.Isometric)
                return;
            
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            
            Vector2 rawInput = new Vector2(horizontal, vertical);
            bool hasInput = rawInput.sqrMagnitude > 0.01f;
            
            MovementInput input = new MovementInput
            {
                Direction = hasInput ? rawInput.normalized : Vector2.zero,
                HasInput = hasInput,
                WorldDirection = Vector3.zero
            };
            
            // Calcular dirección relativa a la cámara activa.
            if (hasInput && IsometricCameraController.Instance != null)
            {
                var cam = IsometricCameraController.Instance;
                Vector3 forward = cam.GetMovementForward();
                Vector3 right   = cam.GetMovementRight();

                input.WorldDirection = (right * input.Direction.x + forward * input.Direction.y).normalized;
            }
            else if (hasInput)
            {
                // Sin cámara, usar dirección directa
                input.WorldDirection = new Vector3(input.Direction.x, 0, input.Direction.y).normalized;
            }
            
            lastMovementInput = input;
            
            if (hasInput)
            {
                OnMovementInput?.Invoke(input);
                wasMoving = true;
            }
            else if (wasMoving)
            {
                OnMovementStop?.Invoke();
                wasMoving = false;
            }
        }
        
        /// <summary>
        /// Procesa clicks del mouse.
        /// </summary>
        private void ProcessClickInput()
        {
            // Click izquierdo
            if (Input.GetMouseButtonDown(0))
            {
                ProcessLeftClick();
            }
        }
        
        /// <summary>
        /// Procesa el click izquierdo según contexto.
        /// </summary>
        private void ProcessLeftClick()
        {
            // Verificar si estamos sobre UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return; // Ignorar clicks sobre UI
            }
            
            Ray ray = IsometricCameraController.Instance != null 
                ? IsometricCameraController.Instance.GetMouseRay()
                : UnityEngine.Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // Intentar detectar entidades primero.
            // Si los LayerMasks no están configurados en el Inspector (valor 0)
            // se usa Physics.DefaultRaycastLayers para no bloquear el click.
            LayerMask entityMask = (entityLayer | enemyLayer) != 0
                ? (LayerMask)(entityLayer | enemyLayer)
                : Physics.DefaultRaycastLayers;

            if (Physics.Raycast(ray, out RaycastHit entityHit, 100f, entityMask))
            {
                // GetComponentInParent cubre el caso en que el Collider está
                // en un hijo del GO que tiene EnemyController / EntityController.
                var enemy = entityHit.collider.GetComponentInParent<EnemyController>();
                if (enemy != null)
                {
                    OnEnemyClick?.Invoke(enemy);
                    return;
                }
                
                var entity = entityHit.collider.GetComponentInParent<EntityController>();
                if (entity != null)
                {
                    OnEntityClick?.Invoke(entity);
                    return;
                }
            }
            
            // Intentar detectar suelo
            if (Physics.Raycast(ray, out RaycastHit groundHit, 100f, groundLayer))
            {
                WorldClickData clickData = new WorldClickData
                {
                    Position = groundHit.point,
                    DidHit = true,
                    HitObject = groundHit.collider.gameObject,
                    RaycastHit = groundHit
                };
                
                OnWorldClick?.Invoke(clickData);
                return;
            }
            
            // No pegó a nada
            OnEmptyClick?.Invoke();
        }
        
        /// <summary>
        /// Procesa input de cancelación.
        /// </summary>
        private void ProcessCancelInput()
        {
            // Escape o click derecho (sin rotación activa)
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancelAction?.Invoke();
            }
            
            // Click derecho en combate = cancelar selección
            if (currentContext == InputContext.Combat && Input.GetMouseButtonDown(1))
            {
                // Solo si no estamos rotando la cámara
                if (!Input.GetMouseButton(1))
                {
                    OnCancelAction?.Invoke();
                }
            }
        }
        
        /// <summary>
        /// Cambia el contexto de input.
        /// </summary>
        public void SetContext(InputContext context)
        {
            if (currentContext == context) return;
            
            InputContext oldContext = currentContext;
            currentContext = context;
            
            Debug.Log($"[GameInputManager] Contexto: {oldContext} → {context}");
            
            // Si dejamos de explorar, detener movimiento
            if (oldContext == InputContext.Exploration && wasMoving)
            {
                OnMovementStop?.Invoke();
                wasMoving = false;
            }
        }
        
        /// <summary>
        /// Verifica si una posición del mundo está visible en pantalla.
        /// </summary>
        public bool IsPositionOnScreen(Vector3 worldPosition)
        {
            if (IsometricCameraController.Instance == null) return false;
            
            Vector3 screenPos = IsometricCameraController.Instance.WorldToScreenPoint(worldPosition);
            return screenPos.z > 0 && 
                   screenPos.x >= 0 && screenPos.x <= Screen.width &&
                   screenPos.y >= 0 && screenPos.y <= Screen.height;
        }
        
        /// <summary>
        /// Obtiene la posición del mouse en el mundo (sobre el suelo).
        /// </summary>
        public bool TryGetMouseWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;
            
            Ray ray = IsometricCameraController.Instance != null 
                ? IsometricCameraController.Instance.GetMouseRay()
                : UnityEngine.Camera.main?.ScreenPointToRay(Input.mousePosition) ?? default;
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                worldPosition = hit.point;
                return true;
            }
            
            return false;
        }
    }
}
