using UnityEngine;
using UnityEngine.AI;
using Managers;
using System.Collections.Generic;

namespace Movement
{
    /// <summary>
    /// Componente que hace que un miembro del party siga al Main.
    /// Usa NavMeshAgent con separación para evitar clipping.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PartyFollower : MonoBehaviour
    {
        [Header("Configuración de Seguimiento")]
        [Tooltip("Distancia mínima al objetivo antes de detenerse")]
        [SerializeField] private float stopDistance = 2f;
        
        [Tooltip("Distancia a la que comienza a seguir")]
        [SerializeField] private float followDistance = 3f;
        
        [Tooltip("Velocidad de movimiento")]
        [SerializeField] private float moveSpeed = 4.5f;
        
        [Tooltip("Velocidad de rotación")]
        [SerializeField] private float rotationSpeed = 360f;
        
        [Header("Separación Anti-Clipping")]
        [Tooltip("Radio de separación de otros seguidores")]
        [SerializeField] private float separationRadius = 1.5f;
        
        [Tooltip("Fuerza de separación")]
        [SerializeField] private float separationForce = 2f;
        
        [Tooltip("Layers a evitar para separación")]
        [SerializeField] private LayerMask followerLayer;
        
        [Header("Formación Básica")]
        [Tooltip("Índice en la formación (0 = más cerca del main)")]
        [SerializeField] private int formationIndex = 0;
        
        [Tooltip("Offset lateral según formación")]
        [SerializeField] private float lateralOffset = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;
        
        // Componentes
        private NavMeshAgent agent;
        private Animator animator;
        private EntityController entityController;
        
        // Estado
        private Transform followTarget;
        private Vector3 lastTargetPosition;
        private bool isFollowing;
        private bool isEnabled = true;
        
        // Cache de otros seguidores
        private static List<PartyFollower> allFollowers = new List<PartyFollower>();
        
        // Animator hashes
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        
        public bool IsFollowing => isFollowing;
        public EntityController EntityController => entityController;
        public int FormationIndex 
        { 
            get => formationIndex;
            set => formationIndex = value;
        }
        
        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            entityController = GetComponent<EntityController>();
            
            ConfigureAgent();
        }
        
        void OnEnable()
        {
            allFollowers.Add(this);
        }
        
        void OnDisable()
        {
            allFollowers.Remove(this);
        }
        
        void Start()
        {
            // Obtener el target inicial
            UpdateFollowTarget();
            
            // Suscribirse a cambios de Main
            if (PlayerPartyManager.Instance != null)
            {
                PlayerPartyManager.Instance.OnMainChanged += OnMainChanged;
            }
        }
        
        void OnDestroy()
        {
            if (PlayerPartyManager.Instance != null)
            {
                PlayerPartyManager.Instance.OnMainChanged -= OnMainChanged;
            }
        }
        
        void Update()
        {
            if (!isEnabled) return;
            if (followTarget == null) 
            {
                UpdateFollowTarget();
                return;
            }
            
            UpdateFollowing();
            UpdateAnimations();
        }
        
        /// <summary>
        /// Configura el NavMeshAgent.
        /// </summary>
        private void ConfigureAgent()
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = rotationSpeed;
            agent.stoppingDistance = stopDistance;
            agent.updateRotation = true;
            agent.autoBraking = true;
            agent.avoidancePriority = 50 + formationIndex; // Prioridad según formación
        }
        
        /// <summary>
        /// Actualiza el objetivo a seguir.
        /// </summary>
        private void UpdateFollowTarget()
        {
            // No seguir si este personaje ES el Main
            if (entityController != null && 
                PlayerPartyManager.Instance != null &&
                PlayerPartyManager.Instance.MainCharacter == entityController)
            {
                followTarget = null;
                isFollowing = false;
                return;
            }
            
            // Seguir al controlador de movimiento del Main
            if (PlayerMovementController.Instance != null)
            {
                followTarget = PlayerMovementController.Instance.transform;
            }
            else if (PlayerPartyManager.Instance?.MainTransform != null)
            {
                followTarget = PlayerPartyManager.Instance.MainTransform;
            }
        }
        
        /// <summary>
        /// Actualiza la lógica de seguimiento.
        /// </summary>
        private void UpdateFollowing()
        {
            if (!agent.isOnNavMesh) return;
            
            Vector3 targetPos = followTarget.position;
            float distanceToTarget = Vector3.Distance(transform.position, targetPos);
            
            // Calcular posición de formación
            Vector3 formationOffset = CalculateFormationOffset();
            Vector3 desiredPosition = targetPos + formationOffset;
            
            // Aplicar separación de otros seguidores
            Vector3 separation = CalculateSeparation();
            desiredPosition += separation;
            
            // Verificar si necesita moverse
            float distanceToDesired = Vector3.Distance(transform.position, desiredPosition);
            
            if (distanceToDesired > followDistance)
            {
                // Comenzar a seguir
                isFollowing = true;
                
                if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                
                lastTargetPosition = targetPos;
            }
            else if (distanceToDesired < stopDistance)
            {
                // Llegó, detenerse
                if (isFollowing)
                {
                    agent.ResetPath();
                    isFollowing = false;
                }
            }
            else if (isFollowing)
            {
                // Seguir actualizando si el target se movió significativamente
                if (Vector3.Distance(lastTargetPosition, targetPos) > 0.5f)
                {
                    if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                    lastTargetPosition = targetPos;
                }
            }
        }
        
        /// <summary>
        /// Calcula el offset de formación basado en el índice.
        /// </summary>
        private Vector3 CalculateFormationOffset()
        {
            if (followTarget == null) return Vector3.zero;
            
            // Dirección opuesta al movimiento del target
            Vector3 backDirection = -followTarget.forward;
            
            // Alternar izquierda/derecha según índice
            float side = (formationIndex % 2 == 0) ? -1f : 1f;
            int row = formationIndex / 2;
            
            // Offset lateral y hacia atrás
            Vector3 lateral = followTarget.right * side * lateralOffset;
            Vector3 back = backDirection * (stopDistance + row * 1.5f);
            
            return lateral + back;
        }
        
        /// <summary>
        /// Calcula la fuerza de separación de otros seguidores.
        /// </summary>
        private Vector3 CalculateSeparation()
        {
            Vector3 separation = Vector3.zero;
            int neighborCount = 0;
            
            foreach (var other in allFollowers)
            {
                if (other == this) continue;
                if (!other.isActiveAndEnabled) continue;
                
                float distance = Vector3.Distance(transform.position, other.transform.position);
                
                if (distance < separationRadius && distance > 0.01f)
                {
                    // Dirección de escape
                    Vector3 awayDirection = (transform.position - other.transform.position).normalized;
                    
                    // Fuerza inversamente proporcional a la distancia
                    float strength = (separationRadius - distance) / separationRadius;
                    separation += awayDirection * strength;
                    
                    neighborCount++;
                }
            }
            
            if (neighborCount > 0)
            {
                separation /= neighborCount;
                separation *= separationForce;
            }
            
            return separation;
        }
        
        /// <summary>
        /// Actualiza las animaciones.
        /// </summary>
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            float speed = agent.velocity.magnitude / moveSpeed;
            
            animator.SetFloat(SpeedHash, speed);
            animator.SetBool(IsMovingHash, isFollowing && speed > 0.1f);
        }
        
        /// <summary>
        /// Callback cuando cambia el Main.
        /// </summary>
        private void OnMainChanged(EntityController oldMain, EntityController newMain)
        {
            // Si este se convirtió en Main, dejar de seguir
            if (newMain == entityController)
            {
                followTarget = null;
                isFollowing = false;
                
                if (agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }
            }
            else
            {
                // Actualizar target
                UpdateFollowTarget();
            }
        }
        
        /// <summary>
        /// Habilita o deshabilita el seguimiento.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            
            if (!enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                isFollowing = false;
            }
        }
        
        /// <summary>
        /// Teletransporta al seguidor cerca del target.
        /// </summary>
        public void TeleportNearTarget()
        {
            if (followTarget == null) return;
            
            Vector3 desiredPosition = followTarget.position + CalculateFormationOffset();
            
            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                transform.position = hit.position;
            }
        }
        
        /// <summary>
        /// Establece la velocidad de movimiento.
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
            agent.speed = speed;
        }
        
        void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;
            
            // Radio de separación
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, separationRadius);
            
            // Stop distance
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, stopDistance);
            
            // Follow distance
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, followDistance);
            
            // Línea al target
            if (followTarget != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, followTarget.position);
                
                // Posición de formación
                Vector3 formPos = followTarget.position + CalculateFormationOffset();
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(formPos, 0.3f);
            }
        }
    }
}
