using UnityEngine;
using System.Collections.Generic;
using Interfaces;

namespace Managers
{
    /// <summary>
    /// Zona de interés del jugador que detecta enemigos cercanos.
    /// Solo detecta y reporta - no decide quién entra en combate.
    /// Sigue automáticamente al main character cuando cambia.
    /// </summary>
    public class PlayerInterestZone : MonoBehaviour
    {
        [Header("Configuración")]
        [Tooltip("Reglas de combate a usar")]
        [SerializeField] private CombatRules combatRules;
        
        [Tooltip("Si true, sigue automáticamente al main del PlayerPartyManager")]
        [SerializeField] private bool followMainCharacter = true;
        
        [Tooltip("Transform manual del jugador (solo si followMainCharacter = false)")]
        [SerializeField] private Transform playerTransform;
        
        [Header("Optimización")]
        [Tooltip("Frecuencia de escaneo en segundos (menor = más preciso pero más costoso)")]
        [Range(0.1f, 2f)]
        [SerializeField] private float scanInterval = 0.25f;
        
        [Tooltip("Usar trigger collider en lugar de OverlapSphere")]
        [SerializeField] private bool useTriggerMode = false;
        
        // Candidatos actualmente en rango de detección
        private HashSet<ICombatCandidate> candidatesInRange = new HashSet<ICombatCandidate>();
        
        // Candidatos en rango de engagement (más cercanos)
        private HashSet<ICombatCandidate> candidatesInEngagementRange = new HashSet<ICombatCandidate>();
        
        // Cache para evitar allocations
        private Collider[] overlapResults = new Collider[50];
        
        // Timer para escaneo
        private float scanTimer;
        
        // Referencia al encounter manager
        private CombatEncounterManager encounterManager;
        
        // Referencia al party manager
        private PlayerPartyManager partyManager;
        
        /// <summary>Candidatos actualmente detectados.</summary>
        public IReadOnlyCollection<ICombatCandidate> CandidatesInRange => candidatesInRange;
        
        /// <summary>Candidatos en rango de combate.</summary>
        public IReadOnlyCollection<ICombatCandidate> CandidatesInEngagementRange => candidatesInEngagementRange;
        
        /// <summary>Transform actual que se está siguiendo.</summary>
        public Transform CurrentTarget => playerTransform;
        
        private void Awake()
        {
            if (combatRules == null)
            {
                combatRules = Resources.Load<CombatRules>("CombatRules");
                if (combatRules == null)
                {
                    Debug.LogError("[PlayerInterestZone] No se encontró CombatRules. Crea uno en Resources/CombatRules");
                }
            }
        }
        
        private void Start()
        {
            encounterManager = FindFirstObjectByType<CombatEncounterManager>();
            partyManager = PlayerPartyManager.Instance;
            
            // Configurar seguimiento del main
            if (followMainCharacter && partyManager != null)
            {
                // Suscribirse a cambios de main
                partyManager.OnMainChanged += OnMainCharacterChanged;
                
                // Obtener main actual
                UpdateTargetToMain();
            }
            else if (playerTransform == null)
            {
                playerTransform = transform;
            }
            
            if (useTriggerMode)
            {
                SetupTriggerCollider();
            }
            
            Debug.Log($"[PlayerInterestZone] Inicializado. Siguiendo: {playerTransform?.name ?? "NINGUNO"}");
        }
        
        private void OnDestroy()
        {
            // Desuscribirse de eventos
            if (partyManager != null)
            {
                partyManager.OnMainChanged -= OnMainCharacterChanged;
            }
        }
        
        private void Update()
        {
            // Si seguimos al main, verificar que tengamos target válido
            if (followMainCharacter && playerTransform == null)
            {
                UpdateTargetToMain();
            }
            
            if (useTriggerMode || combatRules == null || playerTransform == null) return;
            
            scanTimer += Time.deltaTime;
            if (scanTimer >= scanInterval)
            {
                scanTimer = 0f;
                ScanForCandidates();
            }
        }
        
        #region Main Character Tracking
        
        /// <summary>
        /// Callback cuando cambia el main character.
        /// </summary>
        private void OnMainCharacterChanged(EntityController oldMain, EntityController newMain)
        {
            if (!followMainCharacter) return;
            
            Transform oldTransform = playerTransform;
            playerTransform = newMain?.transform;
            
            // Limpiar candidatos al cambiar de main (cambia la ubicación de referencia)
            ClearAllCandidates();
            
            Debug.Log($"[PlayerInterestZone] 👑 Target cambiado: {oldTransform?.name ?? "null"} → {playerTransform?.name ?? "null"}");
            
            // Forzar escaneo inmediato en la nueva ubicación
            if (playerTransform != null)
            {
                ForceScan();
            }
        }
        
        /// <summary>
        /// Actualiza el target al main actual del PlayerPartyManager.
        /// </summary>
        private void UpdateTargetToMain()
        {
            if (partyManager == null)
            {
                partyManager = PlayerPartyManager.Instance;
            }
            
            if (partyManager != null && partyManager.MainTransform != null)
            {
                playerTransform = partyManager.MainTransform;
            }
        }
        
        /// <summary>
        /// Establece manualmente el target a seguir.
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (target == playerTransform) return;
            
            playerTransform = target;
            ClearAllCandidates();
            
            Debug.Log($"[PlayerInterestZone] Target establecido manualmente: {target?.name ?? "null"}");
        }
        
        #endregion
        
        /// <summary>
        /// Escanea el área buscando candidatos de combate.
        /// </summary>
        private void ScanForCandidates()
        {
            Vector3 position = playerTransform.position;
            
            // Escanear radio de detección
            int count = Physics.OverlapSphereNonAlloc(
                position, 
                combatRules.detectionRadius, 
                overlapResults, 
                combatRules.enemyLayers
            );
            
            // Set temporal para detectar quién salió
            var currentScan = new HashSet<ICombatCandidate>();
            var currentEngagement = new HashSet<ICombatCandidate>();
            
            for (int i = 0; i < count; i++)
            {
                var candidate = overlapResults[i].GetComponent<ICombatCandidate>();
                if (candidate == null) continue;
                
                currentScan.Add(candidate);
                
                // Verificar si está en rango de engagement
                float distance = Vector3.Distance(position, candidate.CandidateTransform.position);
                if (distance <= combatRules.engagementRadius)
                {
                    currentEngagement.Add(candidate);
                }
            }
            
            // Detectar nuevos candidatos en rango de detección
            foreach (var candidate in currentScan)
            {
                if (!candidatesInRange.Contains(candidate))
                {
                    OnCandidateEnterDetectionRange(candidate);
                }
            }
            
            // Detectar candidatos que salieron del rango de detección
            var exited = new List<ICombatCandidate>();
            foreach (var candidate in candidatesInRange)
            {
                if (!currentScan.Contains(candidate))
                {
                    exited.Add(candidate);
                }
            }
            foreach (var candidate in exited)
            {
                OnCandidateExitDetectionRange(candidate);
            }
            
            // Detectar nuevos en rango de engagement
            foreach (var candidate in currentEngagement)
            {
                if (!candidatesInEngagementRange.Contains(candidate))
                {
                    OnCandidateEnterEngagementRange(candidate);
                }
            }
            
            // Detectar los que salieron del rango de engagement
            var exitedEngagement = new List<ICombatCandidate>();
            foreach (var candidate in candidatesInEngagementRange)
            {
                if (!currentEngagement.Contains(candidate))
                {
                    exitedEngagement.Add(candidate);
                }
            }
            foreach (var candidate in exitedEngagement)
            {
                OnCandidateExitEngagementRange(candidate);
            }
            
            candidatesInRange = currentScan;
            candidatesInEngagementRange = currentEngagement;
        }
        
        #region Callbacks
        
        private void OnCandidateEnterDetectionRange(ICombatCandidate candidate)
        {
            candidatesInRange.Add(candidate);
            
            // Publicar evento
            EventBus.Publicar(new EventoCandidatoDetectado
            {
                Candidato = candidate,
                EnRangoEngagement = false
            });
            
            if (combatRules.showDebugGizmos)
            {
                Debug.Log($"[InterestZone] Candidato detectado: {candidate.CandidateId}");
            }
        }
        
        private void OnCandidateExitDetectionRange(ICombatCandidate candidate)
        {
            candidatesInRange.Remove(candidate);
            candidatesInEngagementRange.Remove(candidate);
            
            // Publicar evento
            EventBus.Publicar(new EventoCandidatoFueraDeRango
            {
                Candidato = candidate
            });
            
            if (combatRules.showDebugGizmos)
            {
                Debug.Log($"[InterestZone] Candidato fuera de rango: {candidate.CandidateId}");
            }
        }
        
        private void OnCandidateEnterEngagementRange(ICombatCandidate candidate)
        {
            candidatesInEngagementRange.Add(candidate);
            
            // Publicar evento
            EventBus.Publicar(new EventoCandidatoEnRangoCombate
            {
                Candidato = candidate
            });
            
            // Notificar al encounter manager
            encounterManager?.OnCandidateInEngagementRange(candidate);
            
            if (combatRules.showDebugGizmos)
            {
                Debug.Log($"[InterestZone] Candidato en rango de combate: {candidate.CandidateId}");
            }
        }
        
        private void OnCandidateExitEngagementRange(ICombatCandidate candidate)
        {
            candidatesInEngagementRange.Remove(candidate);
            
            // Publicar evento  
            EventBus.Publicar(new EventoCandidatoSalioRangoCombate
            {
                Candidato = candidate
            });
            
            // Notificar al encounter manager
            encounterManager?.OnCandidateLeftEngagementRange(candidate);
            
            if (combatRules.showDebugGizmos)
            {
                Debug.Log($"[InterestZone] Candidato salió de rango de combate: {candidate.CandidateId}");
            }
        }
        
        #endregion
        
        #region Trigger Mode
        
        private void SetupTriggerCollider()
        {
            // Crear collider de detección
            var detectionCollider = gameObject.AddComponent<SphereCollider>();
            detectionCollider.isTrigger = true;
            detectionCollider.radius = combatRules.detectionRadius;
            
            // Necesitaríamos un segundo collider para engagement
            // Por simplicidad, el modo trigger solo usa un rango
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!useTriggerMode) return;
            
            var candidate = other.GetComponent<ICombatCandidate>();
            if (candidate != null)
            {
                OnCandidateEnterDetectionRange(candidate);
                OnCandidateEnterEngagementRange(candidate);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!useTriggerMode) return;
            
            var candidate = other.GetComponent<ICombatCandidate>();
            if (candidate != null)
            {
                OnCandidateExitEngagementRange(candidate);
                OnCandidateExitDetectionRange(candidate);
            }
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            if (combatRules == null || !combatRules.showDebugGizmos) return;
            
            Vector3 pos = playerTransform != null ? playerTransform.position : transform.position;
            
            // Radio de detección
            Gizmos.color = combatRules.detectionColor;
            Gizmos.DrawWireSphere(pos, combatRules.detectionRadius);
            
            // Radio de engagement
            Gizmos.color = combatRules.engagementColor;
            Gizmos.DrawWireSphere(pos, combatRules.engagementRadius);
        }
        
        #endregion
        
        /// <summary>
        /// Fuerza un escaneo inmediato.
        /// </summary>
        public void ForceScan()
        {
            ScanForCandidates();
        }
        
        /// <summary>
        /// Limpia todos los candidatos detectados.
        /// </summary>
        public void ClearAllCandidates()
        {
            candidatesInRange.Clear();
            candidatesInEngagementRange.Clear();
        }
    }
}
