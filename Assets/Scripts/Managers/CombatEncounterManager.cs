using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Interfaces;

namespace Managers
{
    /// <summary>
    /// Manager central que orquesta los encuentros de combate.
    /// Decide qué enemigos entran en combate basándose en reglas y condiciones.
    /// Integrado con PlayerPartyManager para obtener el party dinámicamente.
    /// </summary>
    public class CombatEncounterManager : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private CombatRules combatRules;
        
        [Header("Integración")]
        [Tooltip("Si true, obtiene el party del PlayerPartyManager automáticamente")]
        [SerializeField] private bool usePlayerPartyManager = true;
        
        [Header("Referencias Manuales (si usePlayerPartyManager = false)")]
        [Tooltip("Lista manual de EntityControllers del party")]
        [SerializeField] private List<EntityController> manualPartyMembers = new List<EntityController>();
        
        [Header("Estado")]
        [SerializeField] private bool combatInProgress = false;
        [SerializeField] private float lastEncounterTime = -999f;
        
        // Candidatos actuales en rango de engagement
        private HashSet<ICombatCandidate> engagementCandidates = new HashSet<ICombatCandidate>();
        
        // Enemigos actualmente en combate
        private List<ICombatCandidate> enemiesInCombat = new List<ICombatCandidate>();
        
        // Contexto reutilizable
        private CombatContext currentContext = new CombatContext();
        
        // Referencias
        private CombateManager combateManager;
        private PlayerPartyManager partyManager;
        
        // Singleton opcional
        private static CombatEncounterManager _instance;
        public static CombatEncounterManager Instance => _instance;
        
        #region Propiedades Públicas
        
        public bool CombatInProgress => combatInProgress;
        public IReadOnlyList<ICombatCandidate> EnemiesInCombat => enemiesInCombat;
        public CombatRules Rules => combatRules;
        
        /// <summary>Obtiene el party actual (del PlayerPartyManager o manual).</summary>
        public IReadOnlyList<EntityController> PartyMembers
        {
            get
            {
                if (usePlayerPartyManager && partyManager != null)
                {
                    return partyManager.ActiveParty;
                }
                return manualPartyMembers;
            }
        }
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            if (combatRules == null)
            {
                combatRules = Resources.Load<CombatRules>("CombatRules");
            }
        }
        
        private void Start()
        {
            combateManager = FindFirstObjectByType<CombateManager>();
            partyManager = PlayerPartyManager.Instance;
            
            // Suscribirse a eventos
            EventBus.Suscribir<EventoCombateFinalizado>(OnCombatFinished);
            
            // Suscribirse a cambios de party si usamos el manager
            if (usePlayerPartyManager && partyManager != null)
            {
                partyManager.OnMainChanged += OnMainChanged;
                partyManager.OnCharacterJoinedParty += OnPartyMemberJoined;
                partyManager.OnCharacterLeftParty += OnPartyMemberLeft;
            }
        }
        
        private void OnDestroy()
        {
            EventBus.Desuscribir<EventoCombateFinalizado>(OnCombatFinished);
            
            if (partyManager != null)
            {
                partyManager.OnMainChanged -= OnMainChanged;
                partyManager.OnCharacterJoinedParty -= OnPartyMemberJoined;
                partyManager.OnCharacterLeftParty -= OnPartyMemberLeft;
            }
            
            if (_instance == this)
                _instance = null;
        }
        
        #endregion
        
        #region Event Handlers del Party
        
        private void OnMainChanged(EntityController oldMain, EntityController newMain)
        {
            Debug.Log($"[EncounterManager] Main cambiado: {oldMain?.Nombre_Entidad ?? "null"} → {newMain?.Nombre_Entidad ?? "null"}");
            
            // Si hay combate en progreso y el nuevo main no está en combate, podría agregarse
            // Por ahora solo logueamos el cambio
        }
        
        private void OnPartyMemberJoined(EntityController member)
        {
            Debug.Log($"[EncounterManager] Nuevo miembro en party: {member.Nombre_Entidad}");
            
            // Si hay combate activo, podría unirse
        }
        
        private void OnPartyMemberLeft(EntityController member)
        {
            Debug.Log($"[EncounterManager] Miembro salió del party: {member.Nombre_Entidad}");
        }
        
        #endregion
        
        #region Gestión del Party (Legacy/Manual)
        
        /// <summary>
        /// Registra un miembro del party manualmente.
        /// </summary>
        public void RegisterPartyMember(EntityController member)
        {
            if (!usePlayerPartyManager && member != null && !manualPartyMembers.Contains(member))
            {
                manualPartyMembers.Add(member);
                Debug.Log($"[EncounterManager] Party member registrado: {member.Nombre_Entidad}");
            }
        }
        
        /// <summary>
        /// Remueve un miembro del party manualmente.
        /// </summary>
        public void UnregisterPartyMember(EntityController member)
        {
            if (!usePlayerPartyManager)
            {
                manualPartyMembers.Remove(member);
            }
        }
        
        #endregion
        
        #region Callbacks de PlayerInterestZone
        
        /// <summary>
        /// Llamado cuando un candidato entra en rango de engagement.
        /// </summary>
        public void OnCandidateInEngagementRange(ICombatCandidate candidate)
        {
            if (candidate == null) return;
            
            engagementCandidates.Add(candidate);
            
            // Evaluar si debe iniciar/unirse a combate
            if (combatRules.autoStartCombat)
            {
                EvaluateEncounter();
            }
        }
        
        /// <summary>
        /// Llamado cuando un candidato sale del rango de engagement.
        /// </summary>
        public void OnCandidateLeftEngagementRange(ICombatCandidate candidate)
        {
            if (candidate == null) return;
            
            engagementCandidates.Remove(candidate);
            
            // Si estaba en combate y salió del rango, podría huir
            // Por ahora solo lo removemos si no hay combate activo
            if (!combatInProgress)
            {
                enemiesInCombat.Remove(candidate);
            }
        }
        
        #endregion
        
        #region Evaluación de Encuentros
        
        /// <summary>
        /// Evalúa si debe iniciar un encuentro o agregar enemigos al actual.
        /// </summary>
        public void EvaluateEncounter()
        {
            // Verificar cooldown
            if (Time.time - lastEncounterTime < combatRules.encounterCooldown)
                return;
            
            // Actualizar contexto
            UpdateContext();
            
            // Filtrar candidatos que pueden unirse
            var validCandidates = FilterValidCandidates();
            
            if (validCandidates.Count == 0)
                return;
            
            // Priorizar y limitar
            var selectedCandidates = PrioritizeAndLimit(validCandidates);
            
            if (selectedCandidates.Count == 0)
                return;
            
            // Verificar mínimo de aliados
            int aliveParty = PartyMembers.Count(p => p != null && p.EstaVivo());
            if (aliveParty < combatRules.minAlliesRequired)
            {
                Debug.Log("[EncounterManager] No hay suficientes aliados vivos para combate");
                return;
            }
            
            // Iniciar o actualizar combate
            if (!combatInProgress)
            {
                StartEncounter(selectedCandidates);
            }
            else
            {
                // Agregar enemigos al combate existente
                AddEnemiesToCombat(selectedCandidates);
            }
        }
        
        /// <summary>
        /// Actualiza el contexto de combate con el estado actual.
        /// </summary>
        private void UpdateContext()
        {
            currentContext.CombatInProgress = combatInProgress;
            currentContext.CurrentEnemyCount = enemiesInCombat.Count;
            currentContext.PartyAliveCount = PartyMembers.Count(p => p != null && p.EstaVivo());
            
            // Calcular nivel promedio del party
            var alivePlayers = PartyMembers.Where(p => p != null && p.EstaVivo()).ToList();
            currentContext.PartyAverageLevel = alivePlayers.Count > 0 
                ? Mathf.RoundToInt((float)alivePlayers.Average(p => p.Nivel_Entidad))
                : 1;
            
            // Posición (usar el primer miembro vivo)
            var firstAlive = alivePlayers.FirstOrDefault();
            currentContext.PlayerPosition = firstAlive != null 
                ? firstAlive.transform.position 
                : Vector3.zero;
        }
        
        /// <summary>
        /// Filtra candidatos que cumplen las condiciones para combate.
        /// </summary>
        private List<ICombatCandidate> FilterValidCandidates()
        {
            var valid = new List<ICombatCandidate>();
            
            foreach (var candidate in engagementCandidates)
            {
                // Ya está en combate
                if (enemiesInCombat.Contains(candidate))
                    continue;
                
                // Verificar condiciones del candidato
                if (!candidate.CanJoinCombat(currentContext))
                    continue;
                
                // Verificar línea de visión si es requerida
                if (combatRules.requireLineOfSight)
                {
                    if (!HasLineOfSight(currentContext.PlayerPosition, candidate.CandidateTransform.position))
                        continue;
                }
                
                // Verificar diferencia de nivel
                if (combatRules.maxLevelDifference > 0)
                {
                    var enemyController = candidate as EnemyController;
                    if (enemyController != null)
                    {
                        int levelDiff = Mathf.Abs(enemyController.Nivel_Entidad - currentContext.PartyAverageLevel);
                        if (levelDiff > combatRules.maxLevelDifference)
                            continue;
                    }
                }
                
                valid.Add(candidate);
            }
            
            return valid;
        }
        
        /// <summary>
        /// Ordena y limita los candidatos según las reglas.
        /// </summary>
        private List<ICombatCandidate> PrioritizeAndLimit(List<ICombatCandidate> candidates)
        {
            // Ordenar según priorización configurada
            IEnumerable<ICombatCandidate> ordered = combatRules.prioritization switch
            {
                EnemyPrioritization.ByDistance => candidates.OrderBy(c => 
                    Vector3.Distance(currentContext.PlayerPosition, c.CandidateTransform.position)),
                    
                EnemyPrioritization.ByLevel => candidates.OrderByDescending(c => 
                    (c as EnemyController)?.Nivel_Entidad ?? 0),
                    
                EnemyPrioritization.ByLevelAscending => candidates.OrderBy(c => 
                    (c as EnemyController)?.Nivel_Entidad ?? 0),
                    
                EnemyPrioritization.ByPriority => candidates.OrderByDescending(c => c.CombatPriority),
                
                EnemyPrioritization.Random => candidates.OrderBy(_ => Random.value),
                
                _ => candidates
            };
            
            // Priorizar aggro si está configurado
            if (combatRules.prioritizeAggro)
            {
                ordered = ordered.OrderByDescending(c => c.CombatPriority > 0 ? 1 : 0)
                                 .ThenBy(c => ordered.ToList().IndexOf(c));
            }
            
            // Limitar cantidad
            int maxToAdd = combatRules.maxEnemiesPerEncounter > 0 
                ? combatRules.maxEnemiesPerEncounter - enemiesInCombat.Count
                : int.MaxValue;
            
            return ordered.Take(Mathf.Max(0, maxToAdd)).ToList();
        }
        
        /// <summary>
        /// Verifica línea de visión entre dos puntos.
        /// </summary>
        private bool HasLineOfSight(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;
            
            return !Physics.Raycast(from, direction.normalized, distance, combatRules.lineOfSightBlockers);
        }
        
        #endregion
        
        #region Control de Combate
        
        /// <summary>
        /// Inicia un nuevo encuentro de combate.
        /// </summary>
        private void StartEncounter(List<ICombatCandidate> enemies)
        {
            combatInProgress = true;
            lastEncounterTime = Time.time;
            
            // Notificar a los enemigos
            foreach (var enemy in enemies)
            {
                enemy.OnSelectedForCombat();
                enemiesInCombat.Add(enemy);
            }
            
            // Obtener EnemyControllers para el CombateManager
            var enemyControllers = enemies
                .OfType<EnemyController>()
                .ToList();
            
            // Obtener party válido
            var validParty = PartyMembers
                .Where(p => p != null && p.EstaVivo())
                .Take(combatRules.maxAlliesPerEncounter > 0 ? combatRules.maxAlliesPerEncounter : int.MaxValue)
                .ToList();
            
            // Publicar evento de inicio
            EventBus.Publicar(new EventoEncounterIniciado
            {
                Party = validParty,
                Enemigos = enemyControllers
            });
            
            // Iniciar combate en el CombateManager
            if (combateManager != null)
            {
                combateManager.IniciarCombateConEntidades(validParty, enemyControllers);
            }
            
            Debug.Log($"[EncounterManager] ⚔️ Encuentro iniciado: {validParty.Count} aliados vs {enemyControllers.Count} enemigos");
        }
        
        /// <summary>
        /// Agrega enemigos a un combate en progreso.
        /// </summary>
        private void AddEnemiesToCombat(List<ICombatCandidate> enemies)
        {
            foreach (var enemy in enemies)
            {
                enemy.OnSelectedForCombat();
                enemiesInCombat.Add(enemy);
            }
            
            var enemyControllers = enemies.OfType<EnemyController>().ToList();
            
            // Notificar al CombateManager para agregar enemigos
            if (combateManager != null && enemyControllers.Count > 0)
            {
                combateManager.AgregarEnemigosAlCombate(enemyControllers);
            }
            
            // Publicar evento
            EventBus.Publicar(new EventoEnemigosAgregados
            {
                NuevosEnemigos = enemyControllers
            });
            
            Debug.Log($"[EncounterManager] 👹 +{enemyControllers.Count} enemigos se unieron al combate");
        }
        
        /// <summary>
        /// Callback cuando el combate termina.
        /// </summary>
        private void OnCombatFinished(EventoCombateFinalizado evento)
        {
            // Notificar a todos los enemigos que estaban en combate
            foreach (var enemy in enemiesInCombat)
            {
                enemy.OnRemovedFromCombat();
            }
            
            enemiesInCombat.Clear();
            combatInProgress = false;
            
            Debug.Log($"[EncounterManager] Encuentro finalizado. Victoria: {evento.Victoria}");
        }
        
        /// <summary>
        /// Fuerza el inicio de un encuentro con candidatos específicos.
        /// </summary>
        public void ForceStartEncounter(List<ICombatCandidate> enemies)
        {
            if (combatInProgress)
            {
                AddEnemiesToCombat(enemies);
            }
            else
            {
                StartEncounter(enemies);
            }
        }
        
        /// <summary>
        /// Fuerza el fin del encuentro actual.
        /// </summary>
        public void ForceEndEncounter(bool victory)
        {
            EventBus.Publicar(new EventoCombateFinalizado
            {
                Victoria = victory,
                XPGanada = 0,
                OroGanado = 0
            });
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Mostrar Estado")]
        private void DebugShowState()
        {
            Debug.Log("=== ENCOUNTER MANAGER STATE ===");
            Debug.Log($"Combat in progress: {combatInProgress}");
            Debug.Log($"Party members: {PartyMembers.Count} (usando {(usePlayerPartyManager ? "PlayerPartyManager" : "lista manual")})");
            Debug.Log($"Candidates in engagement range: {engagementCandidates.Count}");
            Debug.Log($"Enemies in combat: {enemiesInCombat.Count}");
            
            foreach (var enemy in enemiesInCombat)
            {
                Debug.Log($"  - {enemy.CandidateId}");
            }
            
            if (partyManager != null)
            {
                Debug.Log($"Main character: {partyManager.MainCharacter?.Nombre_Entidad ?? "None"}");
            }
        }
        
        [ContextMenu("Debug: Forzar Evaluación")]
        private void DebugForceEvaluate()
        {
            lastEncounterTime = -999f; // Reset cooldown
            EvaluateEncounter();
        }
        
        #endregion
    }
}
