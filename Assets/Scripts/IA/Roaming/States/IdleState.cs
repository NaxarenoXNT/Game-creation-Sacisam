using UnityEngine;

namespace IA.Roaming
{
    /// <summary>
    /// Estado Idle: el enemigo está estático en su posición.
    /// Detecta al jugador y puede transicionar a Alerted o Patrolling.
    /// </summary>
    public class IdleState : RoamingState
    {
        private float idleTime = 0f;
        private float maxIdleTime = 5f; // Tiempo antes de empezar a patrullar (si tiene waypoints)
        
        public override void OnEnter()
        {
            idleTime = 0f;
            
            // Detener cualquier movimiento
            var rigidbody = controller.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.velocity = Vector3.zero;
            }
        }
        
        public override void OnUpdate()
        {
            idleTime += Time.deltaTime;
        }
        
        public override RoamingState CheckTransitions()
        {
            var detector = fsm.PlayerDetector;
            
            // Si detecta al jugador → Alerted
            if (detector.PlayerDetected)
            {
                return fsm.GetAlertedState();
            }
            
            // Si tiene waypoints y pasó suficiente tiempo → Patrolling
            if (fsm.SpawnConfig.patrolWaypoints.Count > 0 && idleTime >= maxIdleTime)
            {
                return fsm.GetPatrolState();
            }
            
            return null; // Mantener en Idle
        }
        
        public override string StateName => "Idle";
    }
}
