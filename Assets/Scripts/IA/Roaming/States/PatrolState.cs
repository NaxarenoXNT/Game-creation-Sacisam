using UnityEngine;
using System.Collections.Generic;
using World.ChunkSystem;

namespace IA.Roaming
{
    /// <summary>
    /// Estado Patrol: el enemigo sigue waypoints en loop/ping-pong.
    /// Radio de detección ampliado.
    /// </summary>
    public class PatrolState : RoamingState
    {
        private int currentWaypointIndex = 0;
        private bool isPingPongReversing = false;
        private float waypointWaitTimer = 0f;
        private bool isWaiting = false;
        
        private List<Vector3> waypoints;
        private float moveSpeed = 3f;
        private float waypointReachDistance = 1.5f;
        
        public override void OnEnter()
        {
            waypoints = fsm.SpawnConfig.patrolWaypoints;
            
            // Usar velocidad configurada o default
            moveSpeed = fsm.SpawnConfig.patrolSpeed > 0 ? fsm.SpawnConfig.patrolSpeed : 3f;
            
            // Restaurar índice si estaba patrullando antes
            currentWaypointIndex = fsm.SpawnConfig.currentWaypointIndex;
            
            if (waypoints.Count == 0)
            {
                Debug.LogWarning($"[PatrolState] {controller.name} no tiene waypoints configurados");
            }
        }
        
        public override void OnUpdate()
        {
            // Si no hay waypoints, no hacer nada
            if (waypoints.Count == 0) return;
            
            // Si está esperando en un waypoint
            if (isWaiting)
            {
                waypointWaitTimer += Time.deltaTime;
                
                if (waypointWaitTimer >= fsm.SpawnConfig.waypointWaitTime)
                {
                    isWaiting = false;
                    waypointWaitTimer = 0f;
                    AdvanceToNextWaypoint();
                }
                return;
            }
            
            // Moverse hacia el waypoint actual
            Vector3 targetWaypoint = waypoints[currentWaypointIndex];
            Vector3 direction = (targetWaypoint - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, targetWaypoint);
            
            // Mover al enemigo
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Rotar hacia la dirección de movimiento
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
            
            // Si llegó al waypoint
            if (distance <= waypointReachDistance)
            {
                OnWaypointReached();
            }
        }
        
        private void OnWaypointReached()
        {
            // Guardar índice actual
            fsm.SpawnConfig.currentWaypointIndex = currentWaypointIndex;
            
            // Empezar a esperar
            isWaiting = true;
            waypointWaitTimer = 0f;
        }
        
        private void AdvanceToNextWaypoint()
        {
            switch (fsm.SpawnConfig.patrolBehavior)
            {
                case PatrolBehavior.Loop:
                    currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                    break;
                    
                case PatrolBehavior.PingPong:
                    if (!isPingPongReversing)
                    {
                        currentWaypointIndex++;
                        if (currentWaypointIndex >= waypoints.Count)
                        {
                            currentWaypointIndex = waypoints.Count - 2;
                            isPingPongReversing = true;
                        }
                    }
                    else
                    {
                        currentWaypointIndex--;
                        if (currentWaypointIndex < 0)
                        {
                            currentWaypointIndex = 1;
                            isPingPongReversing = false;
                        }
                    }
                    break;
                    
                case PatrolBehavior.Random:
                    int nextIndex;
                    do
                    {
                        nextIndex = Random.Range(0, waypoints.Count);
                    }
                    while (nextIndex == currentWaypointIndex && waypoints.Count > 1);
                    
                    currentWaypointIndex = nextIndex;
                    break;
                    
                case PatrolBehavior.Once:
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= waypoints.Count)
                    {
                        // Terminó la patrulla, volver a Idle
                        currentWaypointIndex = waypoints.Count - 1;
                    }
                    break;
            }
        }
        
        public override RoamingState CheckTransitions()
        {
            var detector = fsm.PlayerDetector;
            
            // Si detecta al jugador → Alerted (radio ampliado en patrulla)
            if (detector.PlayerDetected)
            {
                return fsm.GetAlertedState();
            }
            
            // Si terminó la patrulla en modo Once → Idle
            if (fsm.SpawnConfig.patrolBehavior == PatrolBehavior.Once && 
                currentWaypointIndex >= waypoints.Count - 1 && 
                !isWaiting)
            {
                return fsm.GetIdleState();
            }
            
            return null;
        }
        
        public override void OnExit()
        {
            // Detener movimiento
            var rigidbody = controller.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.velocity = Vector3.zero;
            }
        }
        
        public override string StateName => "Patrol";
    }
}
