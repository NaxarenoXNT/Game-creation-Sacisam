using UnityEngine;
using Managers;

namespace IA.Roaming
{
    /// <summary>
    /// Estado Chasing: el enemigo persigue al jugador activamente.
    /// Cuando llega lo suficientemente cerca, inicia el combate.
    /// </summary>
    public class ChasingState : RoamingState
    {
        private float chaseSpeed = 5f;
        private float combatEngageDistance = 3f; // Distancia para iniciar combate
        private float maxChaseTime = 15f; // Tiempo máximo persiguiendo antes de desistir
        
        private float chaseTimer = 0f;
        private Vector3 lastKnownPosition;
        
        private CombatEncounterManager encounterManager;
        
        public override void Initialize(EnemyRoamingFSM fsm, EnemyController controller)
        {
            base.Initialize(fsm, controller);
            
            // Obtener encounter manager
            encounterManager = CombatEncounterManager.Instance;
            if (encounterManager == null)
            {
                encounterManager = Object.FindFirstObjectByType<CombatEncounterManager>();
            }
        }
        
        public override void OnEnter()
        {
            chaseTimer = 0f;
            
            // Usar velocidad personalizada si está configurada
            chaseSpeed = fsm.SpawnConfig.patrolSpeed > 0 ? fsm.SpawnConfig.patrolSpeed * 1.5f : 5f;
            
            var detector = fsm.PlayerDetector;
            if (detector.PlayerTransform != null)
            {
                lastKnownPosition = detector.PlayerTransform.position;
            }
            
            Debug.Log($"[Chasing] {controller.name} comenzó a perseguir al jugador");
        }
        
        public override void OnUpdate()
        {
            chaseTimer += Time.deltaTime;
            
            var detector = fsm.PlayerDetector;
            
            // Actualizar última posición conocida si ve al jugador
            if (detector.PlayerDetected && detector.PlayerTransform != null)
            {
                lastKnownPosition = detector.PlayerTransform.position;
            }
            
            // Calcular dirección hacia el jugador
            Vector3 direction = (lastKnownPosition - transform.position).normalized;
            direction.y = 0; // Mantener en el plano horizontal
            
            float distanceToTarget = Vector3.Distance(transform.position, lastKnownPosition);
            
            // Si está lo suficientemente cerca para combate, no acercarse más
            if (distanceToTarget <= combatEngageDistance)
            {
                // Intentar iniciar combate
                TryInitiateCombat();
                return;
            }
            
            // Moverse hacia el jugador
            transform.position += direction * chaseSpeed * Time.deltaTime;
            
            // Rotar hacia el jugador
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
            }
        }
        
        private void TryInitiateCombat()
        {
            if (encounterManager == null)
            {
                Debug.LogWarning($"[Chasing] {controller.name} no puede iniciar combate: CombatEncounterManager no encontrado");
                return;
            }
            
            // Notificar al encounter manager que este enemigo quiere iniciar combate
            encounterManager.RequestCombatFromEnemy(controller);
            
            Debug.Log($"[Chasing] {controller.name} solicitó inicio de combate");
        }
        
        public override RoamingState CheckTransitions()
        {
            var detector = fsm.PlayerDetector;
            
            // Si perdió al jugador completamente (fuera de rango de persecución)
            if (!detector.PlayerInChaseRange)
            {
                Debug.Log($"[Chasing] {controller.name} perdió al jugador, volviendo a Alerted");
                return fsm.GetAlertedState();
            }
            
            // Si pasó mucho tiempo persiguiendo, desistir
            if (chaseTimer >= maxChaseTime)
            {
                Debug.Log($"[Chasing] {controller.name} desistió de perseguir (timeout)");
                
                // Volver a patrullar o idle
                if (fsm.SpawnConfig.patrolWaypoints.Count > 0)
                {
                    return fsm.GetPatrolState();
                }
                else
                {
                    return fsm.GetIdleState();
                }
            }
            
            // Si entró en combate, el FSM se pausará desde el EnemyController
            
            return null;
        }
        
        public override void OnExit()
        {
            Debug.Log($"[Chasing] {controller.name} dejó de perseguir");
        }
        
        public override string StateName => "Chasing";
    }
}
