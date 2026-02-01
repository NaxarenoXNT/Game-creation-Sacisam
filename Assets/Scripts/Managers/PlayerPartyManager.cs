using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Managers
{
    /// <summary>
    /// Gestiona todos los personajes del jugador: main, party activo, refuerzos y estacionados.
    /// Singleton central para el sistema de personajes.
    /// </summary>
    public class PlayerPartyManager : MonoBehaviour
    {
        private static PlayerPartyManager _instance;
        public static PlayerPartyManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayerPartyManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("PlayerPartyManager");
                        _instance = go.AddComponent<PlayerPartyManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [Header("Configuración")]
        [Tooltip("Máximo de personajes que puede tener el jugador")]
        [SerializeField] private int maxOwnedCharacters = 20;
        
        [Tooltip("Máximo de personajes en party activo (siguen al main)")]
        [SerializeField] private int maxActivePartySize = 5;
        
        [Tooltip("Distancia máxima para considerar un personaje como refuerzo disponible")]
        [SerializeField] private float maxReinforcementDistance = 100f;
        
        [Tooltip("Unidades de distancia por turno de espera para refuerzos")]
        [SerializeField] private float distancePerTurn = 20f;
        
        [Header("Estado Actual")]
        [SerializeField] private EntityController mainCharacter;
        
        [SerializeField] private List<EntityController> activeParty = new List<EntityController>();
        
        [SerializeField] private List<EntityController> allOwnedCharacters = new List<EntityController>();
        
        // Personajes estacionados (hibernando) con su ubicación
        private Dictionary<EntityController, StationedCharacterInfo> stationedCharacters = new Dictionary<EntityController, StationedCharacterInfo>();
        
        #region Propiedades Públicas
        
        /// <summary>Personaje actualmente controlado por el jugador.</summary>
        public EntityController MainCharacter => mainCharacter;
        
        /// <summary>Transform del main (para sistemas de detección).</summary>
        public Transform MainTransform => mainCharacter?.transform;
        
        /// <summary>Party activo que sigue al main.</summary>
        public IReadOnlyList<EntityController> ActiveParty => activeParty;
        
        /// <summary>Todos los personajes del jugador.</summary>
        public IReadOnlyList<EntityController> AllOwnedCharacters => allOwnedCharacters;
        
        /// <summary>Cantidad de personajes en el party activo.</summary>
        public int ActivePartyCount => activeParty.Count;
        
        /// <summary>Cantidad total de personajes.</summary>
        public int TotalCharacterCount => allOwnedCharacters.Count;
        
        /// <summary>Si hay espacio para más personajes.</summary>
        public bool CanAddMoreCharacters => allOwnedCharacters.Count < maxOwnedCharacters;
        
        /// <summary>Si hay espacio en el party activo.</summary>
        public bool CanAddToActiveParty => activeParty.Count < maxActivePartySize;
        
        #endregion
        
        #region Eventos
        
        /// <summary>Disparado cuando cambia el main character.</summary>
        public event System.Action<EntityController, EntityController> OnMainChanged; // (oldMain, newMain)
        
        /// <summary>Disparado cuando alguien entra al party activo.</summary>
        public event System.Action<EntityController> OnCharacterJoinedParty;
        
        /// <summary>Disparado cuando alguien sale del party activo.</summary>
        public event System.Action<EntityController> OnCharacterLeftParty;
        
        /// <summary>Disparado cuando se registra un nuevo personaje.</summary>
        public event System.Action<EntityController> OnCharacterRegistered;
        
        /// <summary>Disparado cuando un personaje es estacionado.</summary>
        public event System.Action<EntityController, Vector3> OnCharacterStationed;
        
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
            DontDestroyOnLoad(gameObject);
        }
        
        #endregion
        
        #region Registro de Personajes
        
        /// <summary>
        /// Registra un nuevo personaje como propiedad del jugador.
        /// Llamar cuando el jugador crea o desbloquea un personaje.
        /// </summary>
        public bool RegisterCharacter(EntityController character)
        {
            if (character == null)
            {
                Debug.LogWarning("[PlayerPartyManager] Intento de registrar personaje nulo.");
                return false;
            }
            
            if (allOwnedCharacters.Contains(character))
            {
                Debug.LogWarning($"[PlayerPartyManager] {character.Nombre_Entidad} ya está registrado.");
                return false;
            }
            
            if (!CanAddMoreCharacters)
            {
                Debug.LogWarning($"[PlayerPartyManager] Límite de personajes alcanzado ({maxOwnedCharacters}).");
                return false;
            }
            
            allOwnedCharacters.Add(character);
            
            // Marcar como personaje del jugador
            character.SetPlayerOwned(true);
            
            OnCharacterRegistered?.Invoke(character);
            
            // Publicar evento
            EventBus.Publicar(new EventoPersonajeRegistrado { Personaje = character });
            
            Debug.Log($"[PlayerPartyManager] ✨ Personaje registrado: {character.Nombre_Entidad}");
            
            // Si es el primer personaje, automáticamente es el main
            if (mainCharacter == null)
            {
                SetMainCharacter(character);
            }
            
            return true;
        }
        
        /// <summary>
        /// Desregistra un personaje (muerte permanente, abandono, etc.).
        /// </summary>
        public bool UnregisterCharacter(EntityController character)
        {
            if (character == null || !allOwnedCharacters.Contains(character))
                return false;
            
            // No puede desregistrar al main si es el único
            if (character == mainCharacter && allOwnedCharacters.Count == 1)
            {
                Debug.LogWarning("[PlayerPartyManager] No se puede desregistrar al único personaje.");
                return false;
            }
            
            // Si es el main, cambiar a otro
            if (character == mainCharacter)
            {
                var newMain = allOwnedCharacters.FirstOrDefault(c => c != character && c.EstaVivo());
                if (newMain != null)
                {
                    SetMainCharacter(newMain);
                }
            }
            
            // Remover de todas las listas
            activeParty.Remove(character);
            stationedCharacters.Remove(character);
            allOwnedCharacters.Remove(character);
            
            character.SetPlayerOwned(false);
            
            Debug.Log($"[PlayerPartyManager] 💨 Personaje desregistrado: {character.Nombre_Entidad}");
            
            return true;
        }
        
        #endregion
        
        #region Main Character
        
        /// <summary>
        /// Establece un personaje como el main (controlado por el jugador).
        /// </summary>
        public bool SetMainCharacter(EntityController character)
        {
            if (character == null)
            {
                Debug.LogWarning("[PlayerPartyManager] Intento de establecer main nulo.");
                return false;
            }
            
            if (!allOwnedCharacters.Contains(character))
            {
                Debug.LogWarning($"[PlayerPartyManager] {character.Nombre_Entidad} no está registrado.");
                return false;
            }
            
            if (character == mainCharacter)
            {
                return true; // Ya es el main
            }
            
            EntityController oldMain = mainCharacter;
            mainCharacter = character;
            
            // Asegurar que el main esté en el party activo
            if (!activeParty.Contains(character))
            {
                // Si el party está lleno, hacer espacio
                if (!CanAddToActiveParty && activeParty.Count > 0)
                {
                    // Remover al viejo main del party si hay que hacer espacio
                    if (oldMain != null && activeParty.Contains(oldMain))
                    {
                        RemoveFromActiveParty(oldMain, stationAtCurrentLocation: true);
                    }
                }
                
                AddToActiveParty(character);
            }
            
            // Notificar cambio
            OnMainChanged?.Invoke(oldMain, character);
            
            // Publicar evento al EventBus
            EventBus.Publicar(new EventoMainCambiado
            {
                MainAnterior = oldMain,
                NuevoMain = character
            });
            
            Debug.Log($"[PlayerPartyManager] 👑 Nuevo main: {character.Nombre_Entidad}" + 
                     (oldMain != null ? $" (antes: {oldMain.Nombre_Entidad})" : ""));
            
            return true;
        }
        
        #endregion
        
        #region Party Activo
        
        /// <summary>
        /// Agrega un personaje al party activo (seguirá al main).
        /// </summary>
        public bool AddToActiveParty(EntityController character)
        {
            if (character == null) return false;
            
            if (!allOwnedCharacters.Contains(character))
            {
                Debug.LogWarning($"[PlayerPartyManager] {character.Nombre_Entidad} no está registrado.");
                return false;
            }
            
            if (activeParty.Contains(character))
            {
                return true; // Ya está en el party
            }
            
            if (!CanAddToActiveParty)
            {
                Debug.LogWarning($"[PlayerPartyManager] Party activo lleno ({maxActivePartySize}).");
                return false;
            }
            
            // Remover de estacionados si estaba
            stationedCharacters.Remove(character);
            
            activeParty.Add(character);
            
            OnCharacterJoinedParty?.Invoke(character);
            
            EventBus.Publicar(new EventoPersonajeUnidoParty { Personaje = character });
            
            Debug.Log($"[PlayerPartyManager] ➕ {character.Nombre_Entidad} se unió al party activo.");
            
            return true;
        }
        
        /// <summary>
        /// Remueve un personaje del party activo.
        /// </summary>
        public bool RemoveFromActiveParty(EntityController character, bool stationAtCurrentLocation = false)
        {
            if (character == null) return false;
            
            if (!activeParty.Contains(character))
            {
                return false;
            }
            
            // No puede remover al main del party
            if (character == mainCharacter)
            {
                Debug.LogWarning("[PlayerPartyManager] No se puede remover al main del party activo.");
                return false;
            }
            
            activeParty.Remove(character);
            
            // Estacionar si se solicita
            if (stationAtCurrentLocation)
            {
                StationCharacter(character, character.transform.position);
            }
            
            OnCharacterLeftParty?.Invoke(character);
            
            EventBus.Publicar(new EventoPersonajeSalioParty { Personaje = character });
            
            Debug.Log($"[PlayerPartyManager] ➖ {character.Nombre_Entidad} salió del party activo.");
            
            return true;
        }
        
        #endregion
        
        #region Personajes Estacionados
        
        /// <summary>
        /// Estaciona un personaje en una ubicación (hibernación).
        /// </summary>
        public void StationCharacter(EntityController character, Vector3 location, string locationName = "")
        {
            if (character == null || !allOwnedCharacters.Contains(character)) return;
            
            // Remover del party activo si está
            if (activeParty.Contains(character) && character != mainCharacter)
            {
                activeParty.Remove(character);
            }
            
            stationedCharacters[character] = new StationedCharacterInfo
            {
                Location = location,
                LocationName = locationName,
                StationedTime = Time.time
            };
            
            OnCharacterStationed?.Invoke(character, location);
            
            Debug.Log($"[PlayerPartyManager] 🏠 {character.Nombre_Entidad} estacionado en {(string.IsNullOrEmpty(locationName) ? location.ToString() : locationName)}");
        }
        
        /// <summary>
        /// Obtiene la información de un personaje estacionado.
        /// </summary>
        public StationedCharacterInfo? GetStationedInfo(EntityController character)
        {
            if (stationedCharacters.TryGetValue(character, out var info))
            {
                return info;
            }
            return null;
        }
        
        /// <summary>
        /// Verifica si un personaje está estacionado.
        /// </summary>
        public bool IsStationed(EntityController character)
        {
            return stationedCharacters.ContainsKey(character);
        }
        
        #endregion
        
        #region Sistema de Refuerzos
        
        /// <summary>
        /// Verifica si hay personajes estacionados que podrían ser refuerzos.
        /// </summary>
        public bool HayRefuerzosDisponibles()
        {
            // Hay refuerzos si tenemos personajes fuera del party activo y vivos
            foreach (var character in allOwnedCharacters)
            {
                if (!activeParty.Contains(character) && character != mainCharacter && character.EstaVivo())
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Obtiene personajes disponibles como refuerzos (cercanos pero no en party).
        /// </summary>
        public List<ReinforcementInfo> GetAvailableReinforcements(Vector3 combatPosition)
        {
            var reinforcements = new List<ReinforcementInfo>();
            
            foreach (var character in allOwnedCharacters)
            {
                // Saltar si ya está en party activo
                if (activeParty.Contains(character)) continue;
                
                // Saltar si está muerto
                if (!character.EstaVivo()) continue;
                
                // Obtener posición (del personaje o de donde está estacionado)
                Vector3 characterPosition;
                
                if (stationedCharacters.TryGetValue(character, out var stationedInfo))
                {
                    characterPosition = stationedInfo.Location;
                }
                else
                {
                    characterPosition = character.transform.position;
                }
                
                float distance = Vector3.Distance(combatPosition, characterPosition);
                
                // Verificar si está en rango
                if (distance > maxReinforcementDistance) continue;
                
                // Calcular turnos de llegada
                int turnsToArrive = Mathf.CeilToInt(distance / distancePerTurn);
                turnsToArrive = Mathf.Max(1, turnsToArrive); // Mínimo 1 turno
                
                reinforcements.Add(new ReinforcementInfo
                {
                    Character = character,
                    Distance = distance,
                    TurnsToArrive = turnsToArrive,
                    SourcePosition = characterPosition
                });
            }
            
            // Ordenar por turnos de llegada (más cercanos primero)
            reinforcements.Sort((a, b) => a.TurnsToArrive.CompareTo(b.TurnsToArrive));
            
            return reinforcements;
        }
        
        /// <summary>
        /// Solicita refuerzos para un combate.
        /// </summary>
        public List<ReinforcementInfo> RequestReinforcements(Vector3 combatPosition, int maxReinforcements = 0)
        {
            var available = GetAvailableReinforcements(combatPosition);
            
            if (maxReinforcements > 0)
            {
                available = available.Take(maxReinforcements).ToList();
            }
            
            if (available.Count > 0)
            {
                Debug.Log($"[PlayerPartyManager] 📣 Refuerzos solicitados: {available.Count} personajes disponibles");
                
                foreach (var reinforcement in available)
                {
                    Debug.Log($"   • {reinforcement.Character.Nombre_Entidad}: {reinforcement.Distance:F1}m, llegada en {reinforcement.TurnsToArrive} turnos");
                }
            }
            
            return available;
        }
        
        #endregion
        
        #region Utilidades
        
        /// <summary>
        /// Obtiene todos los personajes vivos en el party activo.
        /// </summary>
        public List<EntityController> GetAlivePartyMembers()
        {
            return activeParty.Where(c => c != null && c.EstaVivo()).ToList();
        }
        
        /// <summary>
        /// Obtiene el nivel promedio del party activo.
        /// </summary>
        public float GetPartyAverageLevel()
        {
            var alive = GetAlivePartyMembers();
            if (alive.Count == 0) return 1;
            return (float)alive.Average(c => c.Nivel_Entidad);
        }
        
        /// <summary>
        /// Verifica si un personaje pertenece al jugador.
        /// </summary>
        public bool IsOwnedByPlayer(EntityController character)
        {
            return allOwnedCharacters.Contains(character);
        }
        
        /// <summary>
        /// Verifica si un personaje está en el party activo.
        /// </summary>
        public bool IsInActiveParty(EntityController character)
        {
            return activeParty.Contains(character);
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Debug: Mostrar Estado")]
        private void DebugShowState()
        {
            Debug.Log("=== PLAYER PARTY MANAGER STATE ===");
            Debug.Log($"Main: {mainCharacter?.Nombre_Entidad ?? "NINGUNO"}");
            Debug.Log($"Party activo ({activeParty.Count}/{maxActivePartySize}):");
            
            foreach (var member in activeParty)
            {
                string isMain = member == mainCharacter ? " [MAIN]" : "";
                Debug.Log($"   • {member.Nombre_Entidad} Nv.{member.Nivel_Entidad}{isMain}");
            }
            
            Debug.Log($"Estacionados ({stationedCharacters.Count}):");
            foreach (var kvp in stationedCharacters)
            {
                Debug.Log($"   • {kvp.Key.Nombre_Entidad} en {kvp.Value.LocationName}");
            }
            
            Debug.Log($"Total personajes: {allOwnedCharacters.Count}/{maxOwnedCharacters}");
        }
        
        #endregion
    }
    
    #region Estructuras de Datos
    
    /// <summary>
    /// Información de un personaje estacionado/hibernando.
    /// </summary>
    [System.Serializable]
    public struct StationedCharacterInfo
    {
        public Vector3 Location;
        public string LocationName;
        public float StationedTime;
    }
    
    /// <summary>
    /// Información de un refuerzo disponible.
    /// </summary>
    public struct ReinforcementInfo
    {
        public EntityController Character;
        public float Distance;
        public int TurnsToArrive;
        public Vector3 SourcePosition;
    }
    
    #endregion
}
