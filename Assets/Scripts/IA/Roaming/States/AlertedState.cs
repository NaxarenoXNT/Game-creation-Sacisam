using UnityEngine;

namespace IA.Roaming
{
    /// <summary>
    /// Estado Alerted: el enemigo detectó al jugador pero aún no lo persigue.
    /// Se queda mirando hacia donde detectó al jugador, esperando que se acerque más.
    /// Radio de detección al máximo.
    /// </summary>
    public class AlertedState : RoamingState
    {
        private float alertTime = 0f;
        private float alertDuration = 2f; // Tiempo en alerta antes de perseguir
        private float maxAlertTime = 10f; // Tiempo máximo antes de volver a idle
        
        private Vector3 alertPosition; // Posición donde detectó al jugador
        
        public override void OnEnter()
        {
            alertTime = 0f;
            alertPosition = transform.position;
            
            // Guardar última posición conocida del jugador
            var detector = fsm.PlayerDetector;
            if (detector.PlayerTransform != null)
            {
                alertPosition = detector.LastKnownPlayerPosition;
            }
            
            // Detener movimiento
            var rigidbody = controller.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Vector3.zero;
            }
        }
        
        public override void OnUpdate()
        {
            alertTime += Time.deltaTime;
            
            var detector = fsm.PlayerDetector;
            
            // Rotar hacia la última posición conocida del jugador
            if (detector.PlayerTransform != null)
            {
                Vector3 directionToPlayer = (detector.PlayerTransform.position - transform.position).normalized;
                directionToPlayer.y = 0; // Mantener horizontal
                
                if (directionToPlayer != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 3f);
                }
            }
        }
        
        public override RoamingState CheckTransitions()
        {
            var detector = fsm.PlayerDetector;
            
            // Si el jugador está en rango de persecución → Chasing
            if (detector.PlayerInChaseRange && alertTime >= alertDuration)
            {
                return fsm.GetChasingState();
            }
            
            // Si perdió al jugador y pasó mucho tiempo → volver a estado anterior
            if (!detector.PlayerDetected && alertTime >= maxAlertTime)
            {
                // Volver a patrullar si tiene waypoints, sino a Idle
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
        
        public override string StateName => "Alerted";
    }
}
