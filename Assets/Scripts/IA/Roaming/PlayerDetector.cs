using UnityEngine;
using Managers;
using World.ChunkSystem;

namespace IA.Roaming
{
    /// <summary>
    /// Detecta al jugador dentro del radio de detección del enemigo.
    /// Calcula distancia, dirección y línea de visión.
    /// </summary>
    public class PlayerDetector
    {
        private EnemyController controller;
        private EnemySpawnConfig config;
        
        // Referencias cacheadas
        private Transform enemyTransform;
        private Transform playerTransform;
        private PlayerPartyManager partyManager;
        
        // Estado de detección
        private bool playerDetected = false;
        private bool playerInChaseRange = false;
        private float distanceToPlayer = float.MaxValue;
        private Vector3 lastKnownPlayerPosition;
        
        // Configuración
        private float detectionRadius = 15f;
        private float chaseRadius = 25f;
        private float detectionInterval = 0.3f; // Check cada 0.3s para optimizar
        
        // Timer
        private float detectionTimer = 0f;
        
        // Propiedades públicas
        public bool PlayerDetected => playerDetected;
        public bool PlayerInChaseRange => playerInChaseRange;
        public float DistanceToPlayer => distanceToPlayer;
        public Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
        public Transform PlayerTransform => playerTransform;
        
        /// <summary>
        /// Constructor del detector.
        /// </summary>
        public PlayerDetector(EnemyController controller, EnemySpawnConfig config)
        {
            this.controller = controller;
            this.config = config;
            this.enemyTransform = controller.transform;
            
            // Obtener radios de configuración (prioridad: SpawnConfig > EnemigoData > Default)
            detectionRadius = config.detectionRadius > 0 
                ? config.detectionRadius 
                : (controller.DatosEnemigo?.radioDeteccion > 0 
                    ? controller.DatosEnemigo.radioDeteccion 
                    : 15f);
            
            chaseRadius = config.chaseRadius > 0 
                ? config.chaseRadius 
                : (controller.DatosEnemigo?.radioPersecucion > 0 
                    ? controller.DatosEnemigo.radioPersecucion 
                    : detectionRadius * 1.5f);
            
            // Intentar obtener el player
            FindPlayer();
        }
        
        /// <summary>
        /// Busca al jugador (main character).
        /// </summary>
        private void FindPlayer()
        {
            partyManager = PlayerPartyManager.Instance;
            
            if (partyManager != null && partyManager.MainCharacter != null)
            {
                playerTransform = partyManager.MainCharacter.transform;
            }
            else
            {
                // Fallback: buscar por tag
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }
        }
        
        /// <summary>
        /// Actualiza la detección. Llamar en Update().
        /// </summary>
        public void Update()
        {
            detectionTimer += Time.deltaTime;
            
            // Solo verificar cada X segundos
            if (detectionTimer < detectionInterval)
                return;
            
            detectionTimer = 0f;
            
            // Verificar que tenemos player
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null)
                {
                    playerDetected = false;
                    return;
                }
            }
            
            // Calcular distancia
            distanceToPlayer = Vector3.Distance(enemyTransform.position, playerTransform.position);
            
            // Actualizar detección
            bool wasDetected = playerDetected;
            playerDetected = distanceToPlayer <= detectionRadius;
            playerInChaseRange = distanceToPlayer <= chaseRadius;
            
            // Guardar última posición conocida
            if (playerDetected)
            {
                lastKnownPlayerPosition = playerTransform.position;
            }
            
            // Log cuando detecta/pierde al jugador
            if (playerDetected && !wasDetected)
            {
                Debug.Log($"[Detector] {controller.name} detectó al jugador a {distanceToPlayer:F1}m");
            }
            else if (!playerDetected && wasDetected)
            {
                Debug.Log($"[Detector] {controller.name} perdió al jugador");
            }
        }
        
        /// <summary>
        /// Obtiene la dirección hacia el jugador (normalizada).
        /// </summary>
        public Vector3 GetDirectionToPlayer()
        {
            if (playerTransform == null) return Vector3.zero;
            
            return (playerTransform.position - enemyTransform.position).normalized;
        }
        
        /// <summary>
        /// Verifica si hay línea de visión hacia el jugador.
        /// </summary>
        public bool HasLineOfSight()
        {
            if (playerTransform == null) return false;
            
            Vector3 direction = playerTransform.position - enemyTransform.position;
            float distance = direction.magnitude;
            
            // Raycast desde la posición del enemigo hacia el jugador
            if (Physics.Raycast(enemyTransform.position + Vector3.up, direction.normalized, out RaycastHit hit, distance))
            {
                // Si golpea al jugador, hay línea de visión
                return hit.transform == playerTransform || hit.transform.CompareTag("Player");
            }
            
            return false;
        }
        
        /// <summary>
        /// Obtiene el radio de detección actual basado en el estado.
        /// </summary>
        public float GetCurrentDetectionRadius(EnemyAIState currentState)
        {
            return currentState switch
            {
                EnemyAIState.Patrolling => detectionRadius * 1.2f, // Radio ampliado
                EnemyAIState.Resting => detectionRadius * 0.7f,    // Radio reducido
                EnemyAIState.Alerted => detectionRadius * 1.5f,    // Radio máximo
                _ => detectionRadius
            };
        }
        
        /// <summary>
        /// Resetea el estado del detector.
        /// </summary>
        public void Reset()
        {
            playerDetected = false;
            playerInChaseRange = false;
            distanceToPlayer = float.MaxValue;
            detectionTimer = 0f;
        }
        
        /// <summary>
        /// Dibuja gizmos de debug.
        /// </summary>
        public void DrawGizmos()
        {
            if (enemyTransform == null) return;
            
            // Radio de detección
            Gizmos.color = playerDetected ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(enemyTransform.position, detectionRadius);
            
            // Radio de persecución
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(enemyTransform.position, chaseRadius);
            
            // Línea hacia el jugador si está detectado
            if (playerDetected && playerTransform != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(enemyTransform.position, playerTransform.position);
            }
        }
    }
}
