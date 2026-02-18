using UnityEngine;
using System.Collections.Generic;
using Interfaces;
using System.Linq;

namespace Managers
{
    /// <summary>
    /// Sistema de cola para enemigos que esperan entrar en combate.
    /// Cuando un enemigo muere, el siguiente de la cola entra automáticamente.
    /// </summary>
    public class EnemyReinforcementQueue : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Si true, los enemigos en la cola se acercan al combate")]
        [SerializeField] private bool moveQueuedEnemiesToCombat = true;
        
        [Tooltip("Velocidad de movimiento de enemigos en cola")]
        [SerializeField] private float movementSpeed = 2f;
        
        // Cola de enemigos esperando
        private Queue<EnemyController> reinforcementQueue = new Queue<EnemyController>();
        
        // Enemigos actualmente moviéndose hacia el combate
        private List<EnemyController> movingReinforcements = new List<EnemyController>();
        
        // Referencias
        private CombatEncounterManager encounterManager;
        private CombateManager combateManager;
        
        // Estado
        private Vector3 combatPosition;
        private bool isActive = false;
        
        // Propiedades
        public int QueueSize => reinforcementQueue.Count;
        public bool HasReinforcements => reinforcementQueue.Count > 0;
        
        private void Awake()
        {
            encounterManager = GetComponent<CombatEncounterManager>();
            if (encounterManager == null)
            {
                encounterManager = FindFirstObjectByType<CombatEncounterManager>();
            }
        }
        
        private void Start()
        {
            combateManager = FindFirstObjectByType<CombateManager>();
            
            // Suscribirse a eventos de combate
            EventBus.Suscribir<EventoCombateIniciado>(OnCombatStarted);
            EventBus.Suscribir<EventoCombateFinalizado>(OnCombatEnded);
            EventBus.Suscribir<EventoEnemigoDerrotado>(OnEnemyDefeated);
        }
        
        private void OnDestroy()
        {
            EventBus.Desuscribir<EventoCombateIniciado>(OnCombatStarted);
            EventBus.Desuscribir<EventoCombateFinalizado>(OnCombatEnded);
            EventBus.Desuscribir<EventoEnemigoDerrotado>(OnEnemyDefeated);
        }
        
        private void Update()
        {
            if (!isActive || !moveQueuedEnemiesToCombat) return;
            
            // Mover enemigos en cola hacia la posición de combate
            for (int i = movingReinforcements.Count - 1; i >= 0; i--)
            {
                var enemy = movingReinforcements[i];
                
                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                {
                    movingReinforcements.RemoveAt(i);
                    continue;
                }
                
                // Mover hacia la posición de combate
                Vector3 direction = (combatPosition - enemy.transform.position).normalized;
                enemy.transform.position += direction * movementSpeed * Time.deltaTime;
                
                // Rotar hacia la dirección
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, targetRotation, Time.deltaTime * 5f);
                }
            }
        }
        
        /// <summary>
        /// Agrega un enemigo a la cola de espera.
        /// </summary>
        public void EnqueueEnemy(EnemyController enemy)
        {
            if (enemy == null || reinforcementQueue.Contains(enemy))
                return;
            
            reinforcementQueue.Enqueue(enemy);
            
            // Si está configurado, agregarlo a la lista de movimiento
            if (moveQueuedEnemiesToCombat)
            {
                movingReinforcements.Add(enemy);
            }
            
            Debug.Log($"[ReinforcementQueue] {enemy.Nombre_Entidad} agregado a la cola (total: {reinforcementQueue.Count})");
        }
        
        /// <summary>
        /// Agrega múltiples enemigos a la cola.
        /// </summary>
        public void EnqueueEnemies(IEnumerable<EnemyController> enemies)
        {
            foreach (var enemy in enemies)
            {
                EnqueueEnemy(enemy);
            }
        }
        
        /// <summary>
        /// Intenta sacar un enemigo de la cola y agregarlo al combate.
        /// </summary>
        public bool TryReinforceCombat()
        {
            if (!HasReinforcements)
                return false;
            
            if (encounterManager == null || combateManager == null)
            {
                Debug.LogWarning("[ReinforcementQueue] No se puede reforzar: managers no disponibles");
                return false;
            }
            
            // Sacar el siguiente enemigo de la cola
            var reinforcement = reinforcementQueue.Dequeue();
            
            // Verificar que siga válido
            if (reinforcement == null || !reinforcement.gameObject.activeInHierarchy || !reinforcement.EstaVivo())
            {
                Debug.LogWarning($"[ReinforcementQueue] Refuerzo inválido, intentando con el siguiente...");
                return TryReinforceCombat(); // Recursivo para el siguiente
            }
            
            // Quitar de lista de movimiento
            movingReinforcements.Remove(reinforcement);
            
            // Agregar al combate
            bool success = encounterManager.TryAddEnemyToCombat(reinforcement);
            
            if (success)
            {
                Debug.Log($"✅ [ReinforcementQueue] {reinforcement.Nombre_Entidad} entró como refuerzo (quedan {reinforcementQueue.Count})");
            }
            else
            {
                Debug.LogWarning($"⚠️ [ReinforcementQueue] No se pudo agregar {reinforcement.Nombre_Entidad} al combate");
                // Re-encolar si falló
                reinforcementQueue.Enqueue(reinforcement);
            }
            
            return success;
        }
        
        /// <summary>
        /// Limpia la cola de refuerzos.
        /// </summary>
        public void Clear()
        {
            reinforcementQueue.Clear();
            movingReinforcements.Clear();
            isActive = false;
            
            Debug.Log("[ReinforcementQueue] Cola limpiada");
        }
        
        /// <summary>
        /// Obtiene todos los enemigos en la cola (sin sacarlos).
        /// </summary>
        public List<EnemyController> GetQueuedEnemies()
        {
            return reinforcementQueue.ToList();
        }
        
        #region Event Handlers
        
        private void OnCombatStarted(EventoCombateIniciado evento)
        {
            isActive = true;
            
            // Calcular posición de combate basándose en el party
            if (evento.Jugadores != null && evento.Jugadores.Count > 0)
            {
                // Usar la posición del primer jugador como referencia
                var firstPlayer = evento.Jugadores[0];
                if (firstPlayer is EntityController controller)
                {
                    combatPosition = controller.transform.position;
                }
            }
            
            Debug.Log($"[ReinforcementQueue] Sistema activado en posición {combatPosition}");
        }
        
        private void OnCombatEnded(EventoCombateFinalizado evento)
        {
            // Limpiar la cola al terminar el combate
            Clear();
            
            Debug.Log("[ReinforcementQueue] Sistema desactivado (combate terminado)");
        }
        
        private void OnEnemyDefeated(EventoEnemigoDerrotado evento)
        {
            // Cuando un enemigo muere, intentar traer uno de la cola
            if (isActive && HasReinforcements && combateManager != null && combateManager.CombateActivo)
            {
                Debug.Log($"[ReinforcementQueue] Enemigo derrotado, trayendo refuerzo...");
                TryReinforceCombat();
            }
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Mostrar Cola")]
        private void DebugShowQueue()
        {
            Debug.Log($"=== ENEMY REINFORCEMENT QUEUE ===");
            Debug.Log($"Activo: {isActive}");
            Debug.Log($"Posición de combate: {combatPosition}");
            Debug.Log($"Enemigos en cola ({reinforcementQueue.Count}):");
            
            int index = 1;
            foreach (var enemy in reinforcementQueue)
            {
                if (enemy != null)
                {
                    Debug.Log($"   {index}. {enemy.Nombre_Entidad} (Nv.{enemy.Nivel_Entidad})");
                    index++;
                }
            }
            
            Debug.Log($"Enemigos moviéndose ({movingReinforcements.Count}):");
            foreach (var enemy in movingReinforcements)
            {
                if (enemy != null)
                {
                    float distance = Vector3.Distance(enemy.transform.position, combatPosition);
                    Debug.Log($"   • {enemy.Nombre_Entidad} - Distancia: {distance:F1}m");
                }
            }
        }
        
        #endregion
    }
}
