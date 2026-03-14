using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Managers;
using Evolution;

namespace CharacterSelection
{
    /// <summary>
    /// Orquestador de la selección de personajes.
    /// Gestiona la creación de personajes, su registro en PlayerPartyManager,
    /// y la transición a la escena de gameplay.
    /// 
    /// Se coloca en la escena CharacterSelection junto al UIDocument.
    /// </summary>
    public class CharacterSelectionManager : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private CharacterSelectionConfig config;

        // Personajes creados durante la selección (aún no en escena de gameplay)
        private readonly List<CharacterCreationData> personajesCreados = new();

        /// <summary>Datos de los personajes creados hasta el momento.</summary>
        public IReadOnlyList<CharacterCreationData> PersonajesCreados => personajesCreados;

        /// <summary>Configuración activa.</summary>
        public CharacterSelectionConfig Config => config;

        /// <summary>Si se puede crear otro personaje.</summary>
        public bool PuedeCrearMas => personajesCreados.Count < config.maxPersonajesInicial;

        /// <summary>Si se cumplen los requisitos mínimos para iniciar.</summary>
        public bool PuedeIniciar => personajesCreados.Count >= config.minPersonajesRequeridos;

        // Eventos para que la UI reaccione
        public event Action<CharacterCreationData> OnPersonajeCreado;
        public event Action<int> OnPersonajeEliminado;
        public event Action OnInicioJuego;

        private void Awake()
        {
            if (config == null)
            {
                config = Resources.Load<CharacterSelectionConfig>("CharacterSelectionConfig");
                if (config == null)
                {
                    Debug.LogError("[CharacterSelection] No se encontró CharacterSelectionConfig. " +
                                   "Asígnalo en el inspector o colócalo en Resources/.");
                    return;
                }
            }
        }

        /// <summary>
        /// Crea un nuevo personaje con la clase y nombre indicados.
        /// No instancia el prefab todavía — eso se hace al cargar la escena de gameplay.
        /// </summary>
        public bool CrearPersonaje(ClaseData clase, string nombre)
        {
            if (clase == null)
            {
                Debug.LogWarning("[CharacterSelection] Clase nula.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                Debug.LogWarning("[CharacterSelection] Nombre vacío.");
                return false;
            }

            if (!PuedeCrearMas)
            {
                Debug.LogWarning($"[CharacterSelection] Máximo de personajes alcanzado ({config.maxPersonajesInicial}).");
                return false;
            }

            var data = new CharacterCreationData
            {
                characterId = Guid.NewGuid().ToString(),
                nombre = nombre,
                clase = clase,
                esMain = personajesCreados.Count == 0 // El primero es el main
            };

            personajesCreados.Add(data);
            OnPersonajeCreado?.Invoke(data);

            Debug.Log($"[CharacterSelection] Personaje creado: {nombre} ({clase.nombreClase}) " +
                      $"[{personajesCreados.Count}/{config.maxPersonajesInicial}]");

            return true;
        }

        /// <summary>
        /// Elimina un personaje creado por su índice.
        /// </summary>
        public bool EliminarPersonaje(int index)
        {
            if (index < 0 || index >= personajesCreados.Count)
                return false;

            personajesCreados.RemoveAt(index);

            // Si eliminamos el main, el nuevo primero pasa a ser main
            if (personajesCreados.Count > 0)
            {
                for (int i = 0; i < personajesCreados.Count; i++)
                    personajesCreados[i].esMain = i == 0;
            }

            OnPersonajeEliminado?.Invoke(index);
            return true;
        }

        /// <summary>
        /// Establece un personaje como main (el primero en controlarse).
        /// </summary>
        public void EstablecerMain(int index)
        {
            if (index < 0 || index >= personajesCreados.Count) return;

            for (int i = 0; i < personajesCreados.Count; i++)
                personajesCreados[i].esMain = i == index;
        }

        /// <summary>
        /// Finaliza la selección: instancia los personajes, registra en PlayerPartyManager
        /// y carga la escena de gameplay.
        /// </summary>
        public void IniciarJuego()
        {
            if (!PuedeIniciar)
            {
                Debug.LogWarning("[CharacterSelection] Faltan personajes para iniciar.");
                return;
            }

            OnInicioJuego?.Invoke();

            // Instanciar personajes como GameObjects persistentes (DontDestroyOnLoad)
            var partyManager = PlayerPartyManager.Instance;
            EntityController mainController = null;

            foreach (var data in personajesCreados)
            {
                var go = Instantiate(config.playerPrefab);
                go.name = $"Player_{data.nombre}";
                DontDestroyOnLoad(go);

                var controller = go.GetComponent<EntityController>();
                if (controller == null)
                {
                    Debug.LogError($"[CharacterSelection] Prefab no tiene EntityController: {config.playerPrefab.name}");
                    Destroy(go);
                    continue;
                }

                // Inicializar con la clase elegida
                controller.Inicializar(data.clase);

                // Registrar en PlayerPartyManager
                partyManager.RegisterCharacter(controller);
                partyManager.AddToActiveParty(controller);

                if (data.esMain)
                    mainController = controller;

                // Crear EvolutionState per-personaje
                var evolutionState = new EvolutionState
                {
                    characterId = controller.CharacterId,
                    nivelJugador = 1,
                    seed = UnityEngine.Random.Range(0, int.MaxValue)
                };

                // Registrar en MissionManager si existe
                var missionManager = FindFirstObjectByType<Missions.MissionManager>();
                if (missionManager != null)
                {
                    missionManager.RegistrarPersonaje(controller.CharacterId, evolutionState);
                }

                Debug.Log($"[CharacterSelection] Instanciado: {data.nombre} ({data.clase.nombreClase})");
            }

            // Establecer main
            if (mainController != null)
            {
                partyManager.SetMainCharacter(mainController);
            }

            // Cargar escena de gameplay
            Debug.Log($"[CharacterSelection] Cargando escena: {config.escenaDestino}");
            SceneManager.LoadScene(config.escenaDestino);
        }
    }

    /// <summary>
    /// Datos temporales de un personaje durante la selección.
    /// Se usa para crear el EntityController al iniciar el juego.
    /// </summary>
    [Serializable]
    public class CharacterCreationData
    {
        public string characterId;
        public string nombre;
        public ClaseData clase;
        public bool esMain;
    }
}
