using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Managers
{
    /// <summary>
    /// Sistema de refuerzos que gestiona la llegada de personajes aliados durante el combate.
    /// Trabaja en conjunto con CombateManager y PlayerPartyManager.
    /// </summary>
    public class ReinforcementSystem : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Mostrar mensajes de debug")]
        [SerializeField] private bool debugMode = true;
        
        // Refuerzos pendientes de llegar
        private List<PendingReinforcement> pendingReinforcements = new List<PendingReinforcement>();
        
        // Turno actual del combate
        private int currentCombatTurn = 0;
        
        // Referencias
        private CombateManager combateManager;
        private PlayerPartyManager partyManager;
        
        // Singleton
        private static ReinforcementSystem _instance;
        public static ReinforcementSystem Instance => _instance;
        
        /// <summary>Refuerzos pendientes de llegar.</summary>
        public IReadOnlyList<PendingReinforcement> PendingReinforcements => pendingReinforcements;
        
        /// <summary>Cantidad de refuerzos en camino.</summary>
        public int PendingCount => pendingReinforcements.Count;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        private void Start()
        {
            combateManager = FindFirstObjectByType<CombateManager>();
            partyManager = PlayerPartyManager.Instance;
            
            // Suscribirse a eventos
            EventBus.Suscribir<EventoTurnoIniciado>(OnTurnStarted);
            EventBus.Suscribir<EventoCombateFinalizado>(OnCombatEnded);
            EventBus.Suscribir<EventoEncounterIniciado>(OnEncounterStarted);
        }
        
        private void OnDestroy()
        {
            EventBus.Desuscribir<EventoTurnoIniciado>(OnTurnStarted);
            EventBus.Desuscribir<EventoCombateFinalizado>(OnCombatEnded);
            EventBus.Desuscribir<EventoEncounterIniciado>(OnEncounterStarted);
            
            if (_instance == this)
                _instance = null;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnEncounterStarted(EventoEncounterIniciado evento)
        {
            currentCombatTurn = 0;
            pendingReinforcements.Clear();
            
            if (debugMode)
            {
                Debug.Log("[ReinforcementSystem] Combate iniciado, sistema de refuerzos listo.");
            }
        }
        
        private void OnTurnStarted(EventoTurnoIniciado evento)
        {
            currentCombatTurn = evento.NumeroTurno;
            
            // Verificar si hay refuerzos que lleguen este turno
            ProcessArrivingReinforcements();
        }
        
        private void OnCombatEnded(EventoCombateFinalizado evento)
        {
            // Limpiar refuerzos pendientes
            if (pendingReinforcements.Count > 0)
            {
                if (debugMode)
                {
                    Debug.Log($"[ReinforcementSystem] Combate terminado. {pendingReinforcements.Count} refuerzos cancelados.");
                }
                
                pendingReinforcements.Clear();
            }
            
            currentCombatTurn = 0;
        }
        
        #endregion
        
        #region Solicitud de Refuerzos
        
        /// <summary>
        /// Solicita refuerzos para el combate actual.
        /// Llamar desde el jugador o UI.
        /// </summary>
        public void RequestReinforcements(Vector3 combatPosition, int maxToRequest = 0)
        {
            if (partyManager == null)
            {
                Debug.LogError("[ReinforcementSystem] PlayerPartyManager no disponible.");
                return;
            }
            
            var available = partyManager.RequestReinforcements(combatPosition, maxToRequest);
            
            foreach (var reinforcement in available)
            {
                ScheduleReinforcement(reinforcement);
            }
            
            // Publicar evento
            if (available.Count > 0)
            {
                // Extraer EntityControllers de ReinforcementInfo
                var controllers = available.Select(r => r.Character).ToList();
                
                EventBus.Publicar(new EventoRefuerzosSolicitados
                {
                    CantidadSolicitada = available.Count,
                    Refuerzos = controllers,
                    RefuerzosDisponibles = controllers,
                    PosicionCombate = combatPosition
                });
            }
        }
        
        /// <summary>
        /// Solicita un refuerzo específico.
        /// </summary>
        public bool RequestSpecificReinforcement(EntityController character, Vector3 combatPosition)
        {
            if (partyManager == null || character == null) return false;
            
            // Verificar que el personaje sea elegible
            if (!partyManager.IsOwnedByPlayer(character)) return false;
            if (partyManager.IsInActiveParty(character)) return false;
            if (!character.EstaVivo()) return false;
            
            // Verificar si ya está pendiente
            if (pendingReinforcements.Any(p => p.Character == character))
            {
                Debug.LogWarning($"[ReinforcementSystem] {character.Nombre_Entidad} ya está en camino.");
                return false;
            }
            
            // Obtener posición del personaje
            Vector3 sourcePosition;
            var stationedInfo = partyManager.GetStationedInfo(character);
            sourcePosition = stationedInfo?.Location ?? character.transform.position;
            
            float distance = Vector3.Distance(combatPosition, sourcePosition);
            int turnsToArrive = Mathf.Max(1, Mathf.CeilToInt(distance / 20f)); // 20 unidades por turno
            
            var reinforcement = new ReinforcementInfo
            {
                Character = character,
                Distance = distance,
                TurnsToArrive = turnsToArrive,
                SourcePosition = sourcePosition
            };
            
            ScheduleReinforcement(reinforcement);
            
            return true;
        }
        
        /// <summary>
        /// Programa la llegada de un refuerzo.
        /// </summary>
        private void ScheduleReinforcement(ReinforcementInfo reinforcement)
        {
            // Verificar que no esté ya programado
            if (pendingReinforcements.Any(p => p.Character == reinforcement.Character))
            {
                return;
            }
            
            int arrivalTurn = currentCombatTurn + reinforcement.TurnsToArrive;
            
            var pending = new PendingReinforcement
            {
                Character = reinforcement.Character,
                ArrivalTurn = arrivalTurn,
                SourcePosition = reinforcement.SourcePosition,
                Distance = reinforcement.Distance
            };
            
            pendingReinforcements.Add(pending);
            
            // Ordenar por turno de llegada
            pendingReinforcements.Sort((a, b) => a.ArrivalTurn.CompareTo(b.ArrivalTurn));
            
            if (debugMode)
            {
                Debug.Log($"[ReinforcementSystem] 🏃 {reinforcement.Character.Nombre_Entidad} en camino, llegada en turno {arrivalTurn} ({reinforcement.TurnsToArrive} turnos)");
            }
            
            // Publicar evento
            EventBus.Publicar(new EventoRefuerzoProgramado
            {
                Personaje = reinforcement.Character,
                TurnoLlegada = arrivalTurn,
                TurnosRestantes = reinforcement.TurnsToArrive
            });
        }
        
        #endregion
        
        #region Procesamiento de Llegadas
        
        /// <summary>
        /// Procesa los refuerzos que llegan en el turno actual.
        /// </summary>
        private void ProcessArrivingReinforcements()
        {
            var arriving = pendingReinforcements
                .Where(p => p.ArrivalTurn <= currentCombatTurn)
                .ToList();
            
            foreach (var reinforcement in arriving)
            {
                // Remover de pendientes
                pendingReinforcements.Remove(reinforcement);
                
                // Verificar que sigue vivo y es válido
                if (reinforcement.Character == null || !reinforcement.Character.EstaVivo())
                {
                    Debug.LogWarning($"[ReinforcementSystem] Refuerzo inválido o muerto, saltando...");
                    continue;
                }
                
                // Agregar al combate
                AddReinforcementToCombat(reinforcement.Character);
            }
        }
        
        /// <summary>
        /// Agrega un refuerzo al combate activo.
        /// </summary>
        private void AddReinforcementToCombat(EntityController character)
        {
            if (debugMode)
            {
                Debug.Log($"\n[ReinforcementSystem] ⚔️ ¡{character.Nombre_Entidad} llegó al combate!");
            }
            
            // Agregar al party activo
            if (partyManager != null)
            {
                partyManager.AddToActiveParty(character);
            }
            
            // Notificar al CombateManager
            if (combateManager != null)
            {
                combateManager.AgregarAliadoAlCombate(character);
            }
            
            // Publicar evento
            EventBus.Publicar(new EventoRefuerzoLlegado
            {
                Personaje = character,
                TurnoLlegada = currentCombatTurn
            });
        }
        
        #endregion
        
        #region Cancelación
        
        /// <summary>
        /// Cancela un refuerzo específico.
        /// </summary>
        public bool CancelReinforcement(EntityController character)
        {
            var pending = pendingReinforcements.FirstOrDefault(p => p.Character == character);
            
            if (pending.Character != null)
            {
                pendingReinforcements.Remove(pending);
                
                if (debugMode)
                {
                    Debug.Log($"[ReinforcementSystem] ❌ Refuerzo de {character.Nombre_Entidad} cancelado.");
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Cancela todos los refuerzos pendientes.
        /// </summary>
        public void CancelAllReinforcements()
        {
            int count = pendingReinforcements.Count;
            pendingReinforcements.Clear();
            
            if (debugMode && count > 0)
            {
                Debug.Log($"[ReinforcementSystem] ❌ {count} refuerzos cancelados.");
            }
        }
        
        #endregion
        
        #region Consultas
        
        /// <summary>
        /// Obtiene los turnos restantes para que llegue un refuerzo.
        /// </summary>
        public int GetTurnsUntilArrival(EntityController character)
        {
            var pending = pendingReinforcements.FirstOrDefault(p => p.Character == character);
            
            if (pending.Character != null)
            {
                return Mathf.Max(0, pending.ArrivalTurn - currentCombatTurn);
            }
            
            return -1; // No está en camino
        }
        
        /// <summary>
        /// Verifica si un personaje está en camino.
        /// </summary>
        public bool IsEnRoute(EntityController character)
        {
            return pendingReinforcements.Any(p => p.Character == character);
        }
        
        /// <summary>
        /// Obtiene el próximo refuerzo en llegar.
        /// </summary>
        public PendingReinforcement? GetNextReinforcement()
        {
            if (pendingReinforcements.Count == 0) return null;
            return pendingReinforcements[0];
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Mostrar Refuerzos Pendientes")]
        private void DebugShowPending()
        {
            Debug.Log("=== REFUERZOS PENDIENTES ===");
            Debug.Log($"Turno actual: {currentCombatTurn}");
            Debug.Log($"Pendientes: {pendingReinforcements.Count}");
            
            foreach (var pending in pendingReinforcements)
            {
                int turnsLeft = pending.ArrivalTurn - currentCombatTurn;
                Debug.Log($"   • {pending.Character.Nombre_Entidad}: llegada turno {pending.ArrivalTurn} ({turnsLeft} turnos)");
            }
        }
        
        #endregion
    }
    
    #region Estructuras
    
    /// <summary>
    /// Información de un refuerzo pendiente.
    /// </summary>
    [System.Serializable]
    public struct PendingReinforcement
    {
        public EntityController Character;
        public int ArrivalTurn;
        public Vector3 SourcePosition;
        public float Distance;
    }
    
    #endregion
}
