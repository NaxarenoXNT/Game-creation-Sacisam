using UnityEngine;

namespace IA.Roaming
{
    /// <summary>
    /// Estado Resting: el enemigo está descansando (sentado, durmiendo, etc).
    /// Radio de detección reducido.
    /// Comportamiento similar a Idle pero con menor vigilancia.
    /// </summary>
    public class RestingState : RoamingState
    {
        private float restTime = 0f;
        private float maxRestTime = 10f; // Tiempo antes de levantarse
        
        public override void OnEnter()
        {
            restTime = 0f;
            
            // Detener cualquier movimiento
            var rigidbody = controller.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
            }
            
            // TODO: Activar animación de descanso cuando se implemente
        }
        
        public override void OnUpdate()
        {
            restTime += Time.deltaTime;
        }
        
        public override RoamingState CheckTransitions()
        {
            var detector = fsm.PlayerDetector;
            
            // Si detecta al jugador (con radio reducido) → Alerted
            if (detector.PlayerDetected)
            {
                return fsm.GetAlertedState();
            }
            
            // Si pasó el tiempo de descanso → Idle o Patrolling
            if (restTime >= maxRestTime)
            {
                if (fsm.SpawnConfig.patrolWaypoints.Count > 0)
                {
                    return fsm.GetPatrolState();
                }
                else
                {
                    return fsm.GetIdleState();
                }
            }
            
            return null;
        }
        
        public override void OnExit()
        {
            // TODO: Desactivar animación de descanso cuando se implemente
        }
        
        public override string StateName => "Resting";
    }
}
