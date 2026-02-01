using UnityEngine;
using UnityEngine.AI;
using GameInput;
using Managers;

namespace Movement
{
    /// <summary>
    /// Controlador de movimiento del personaje principal.
    /// Soporta WASD + Click-to-move.
    /// Se vincula automáticamente al Main de PlayerPartyManager.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerMovementController : MonoBehaviour
    {
        public static PlayerMovementController Instance { get; private set; }
        
        [Header("Configuración de Movimiento")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float stoppingDistance = 0.1f;
        
        [Header("Click-to-Move")]
        [SerializeField] private bool enableClickToMove = true;
        [SerializeField] private float clickMoveStoppingDistance = 0.5f;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject clickIndicatorPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        // Componentes
        private NavMeshAgent agent;
        private Animator animator;
        
        // Estado
        private EntityController currentMain;
        private Vector3 clickDestination;
        private bool isMovingToClick;
        private bool isWASDMoving;
        
        // Click indicator
        private GameObject clickIndicatorInstance;
        
        // Animator hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        
        public bool IsMoving => isWASDMoving || isMovingToClick;
        public EntityController CurrentMain => currentMain;
        
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            
            ConfigureAgent();
        }
        
        void Start()
        {
            // Vincular con PlayerPartyManager
            if (PlayerPartyManager.Instance != null)
            {
                // Obtener el main actual
                currentMain = PlayerPartyManager.Instance.MainCharacter;
                
                // Posicionar este controlador en el main
                if (currentMain != null)
                {
                    SyncWithMain();
                }
                
                // Suscribirse a cambios
                PlayerPartyManager.Instance.OnMainChanged += OnMainChanged;
            }
            
            // Suscribirse a eventos de input
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.OnMovementInput += OnMovementInput;
                GameInputManager.Instance.OnMovementStop += OnMovementStop;
                GameInputManager.Instance.OnWorldClick += OnWorldClick;
            }
            
            // Crear indicador de click si hay prefab
            if (clickIndicatorPrefab != null)
            {
                clickIndicatorInstance = Instantiate(clickIndicatorPrefab);
                clickIndicatorInstance.SetActive(false);
            }
        }
        
        void OnDestroy()
        {
            if (PlayerPartyManager.Instance != null)
            {
                PlayerPartyManager.Instance.OnMainChanged -= OnMainChanged;
            }
            
            if (GameInputManager.Instance != null)
            {
                GameInputManager.Instance.OnMovementInput -= OnMovementInput;
                GameInputManager.Instance.OnMovementStop -= OnMovementStop;
                GameInputManager.Instance.OnWorldClick -= OnWorldClick;
            }
            
            if (clickIndicatorInstance != null)
            {
                Destroy(clickIndicatorInstance);
            }
            
            if (Instance == this) Instance = null;
        }
        
        void Update()
        {
            if (currentMain == null) return;
            
            // WASD tiene prioridad sobre click-to-move
            if (isWASDMoving)
            {
                isMovingToClick = false;
                HideClickIndicator();
            }
            
            // Verificar si llegamos al destino del click
            if (isMovingToClick && !agent.pathPending)
            {
                if (agent.remainingDistance <= clickMoveStoppingDistance)
                {
                    StopClickMove();
                }
            }
            
            // Sincronizar posición con el Main
            SyncMainPosition();
            
            // Actualizar animaciones
            UpdateAnimations();
        }
        
        /// <summary>
        /// Configura el NavMeshAgent con los valores iniciales.
        /// </summary>
        private void ConfigureAgent()
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = rotationSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.updateRotation = true;
            agent.updatePosition = true;
        }
        
        /// <summary>
        /// Sincroniza la posición del controlador con el Main.
        /// </summary>
        private void SyncWithMain()
        {
            if (currentMain == null) return;
            
            transform.position = currentMain.transform.position;
            transform.rotation = currentMain.transform.rotation;
            
            // Warpar el NavMeshAgent
            if (agent.isOnNavMesh)
            {
                agent.Warp(currentMain.transform.position);
            }
        }
        
        /// <summary>
        /// Actualiza la posición del Main para que siga al controlador.
        /// </summary>
        private void SyncMainPosition()
        {
            if (currentMain == null) return;
            
            // El Main sigue la posición del controlador
            currentMain.transform.position = transform.position;
            currentMain.transform.rotation = transform.rotation;
        }
        
        /// <summary>
        /// Callback de input WASD.
        /// </summary>
        private void OnMovementInput(MovementInput input)
        {
            if (currentMain == null) return;
            if (!agent.isOnNavMesh) return;
            
            isWASDMoving = true;
            
            // Cancelar cualquier path de click-to-move
            if (isMovingToClick)
            {
                agent.ResetPath();
                isMovingToClick = false;
                HideClickIndicator();
            }
            
            // Calcular velocidad deseada
            Vector3 desiredVelocity = input.WorldDirection * moveSpeed;
            
            // Usar el NavMeshAgent para moverse
            agent.velocity = desiredVelocity;
            
            // Rotar hacia la dirección de movimiento
            if (input.WorldDirection.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(input.WorldDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        
        /// <summary>
        /// Callback cuando se detiene el input WASD.
        /// </summary>
        private void OnMovementStop()
        {
            isWASDMoving = false;
            
            if (!isMovingToClick && agent.isOnNavMesh)
            {
                agent.velocity = Vector3.zero;
                agent.ResetPath();
            }
        }
        
        /// <summary>
        /// Callback de click en el mundo.
        /// </summary>
        private void OnWorldClick(WorldClickData clickData)
        {
            if (!enableClickToMove) return;
            if (currentMain == null) return;
            if (!agent.isOnNavMesh) return;
            
            // Solo en exploración
            if (GameInputManager.Instance != null && 
                GameInputManager.Instance.CurrentContext != InputContext.Exploration)
            {
                return;
            }
            
            MoveToPosition(clickData.Position);
        }
        
        /// <summary>
        /// Mueve al personaje a una posición específica.
        /// </summary>
        public void MoveToPosition(Vector3 destination)
        {
            if (!agent.isOnNavMesh) return;
            
            // Encontrar punto válido en NavMesh
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                clickDestination = hit.position;
                isMovingToClick = true;
                isWASDMoving = false;
                
                agent.SetDestination(clickDestination);
                
                ShowClickIndicator(clickDestination);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[PlayerMovement] Click-to-move: {clickDestination}");
                }
            }
        }
        
        /// <summary>
        /// Detiene el movimiento click-to-move.
        /// </summary>
        private void StopClickMove()
        {
            isMovingToClick = false;
            
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
            
            HideClickIndicator();
        }
        
        /// <summary>
        /// Muestra el indicador de click.
        /// </summary>
        private void ShowClickIndicator(Vector3 position)
        {
            if (clickIndicatorInstance == null) return;
            
            clickIndicatorInstance.transform.position = position + Vector3.up * 0.05f;
            clickIndicatorInstance.SetActive(true);
        }
        
        /// <summary>
        /// Oculta el indicador de click.
        /// </summary>
        private void HideClickIndicator()
        {
            if (clickIndicatorInstance != null)
            {
                clickIndicatorInstance.SetActive(false);
            }
        }
        
        /// <summary>
        /// Actualiza los parámetros del Animator.
        /// </summary>
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            float speed = agent.velocity.magnitude / moveSpeed;
            
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsMovingHash, IsMoving && speed > 0.1f);
        }
        
        /// <summary>
        /// Callback cuando cambia el Main.
        /// </summary>
        private void OnMainChanged(EntityController oldMain, EntityController newMain)
        {
            currentMain = newMain;
            
            if (newMain != null)
            {
                SyncWithMain();
                Debug.Log($"[PlayerMovement] Nuevo Main: {newMain.Nombre_Entidad}");
            }
            
            // Cancelar movimiento actual
            StopAllMovement();
        }
        
        /// <summary>
        /// Detiene todo tipo de movimiento.
        /// </summary>
        public void StopAllMovement()
        {
            isWASDMoving = false;
            isMovingToClick = false;
            
            if (agent.isOnNavMesh)
            {
                agent.velocity = Vector3.zero;
                agent.ResetPath();
            }
            
            HideClickIndicator();
        }
        
        /// <summary>
        /// Teletransporta al personaje a una posición.
        /// </summary>
        public void TeleportTo(Vector3 position)
        {
            StopAllMovement();
            
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                transform.position = hit.position;
                SyncMainPosition();
            }
        }
        
        /// <summary>
        /// Habilita o deshabilita el movimiento.
        /// </summary>
        public void SetMovementEnabled(bool enabled)
        {
            agent.isStopped = !enabled;
            
            if (!enabled)
            {
                StopAllMovement();
            }
        }
        
        void OnDrawGizmosSelected()
        {
            if (!showDebugInfo) return;
            if (agent == null) return;
            
            // Dibujar destino
            if (agent.hasPath)
            {
                Gizmos.color = Color.green;
                Vector3[] corners = agent.path.corners;
                
                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
                
                Gizmos.DrawWireSphere(agent.destination, 0.3f);
            }
            
            // Dibujar velocidad
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, agent.velocity);
        }
    }
}
