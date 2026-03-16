using System;
using System.Collections.Generic;
using CharacterSelection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFlow
{
    /// <summary>
    /// Estado de selección de personaje.
    /// Se activa al iniciar una partida nueva (sin guardado previo).
    /// Transiciona a ExplorationFlowState cuando el jugador confirma su selección.
    /// </summary>
    public class CharacterSelectionFlowState : IGameFlowState
    {
        public bool BlocksLowerStates => true;

        public IEnumerable<Type> AllowedTransitions => new[]
        {
            typeof(ExplorationFlowState),
        };

        private CharacterSelectionManager _manager;
        private bool _loadedAdditively;

        public void Enter()
        {
            Debug.Log("[CharacterSelectionFlow] → Enter: Activando selección de personaje");

            // Buscar manager en la escena actual (si ya estamos en la escena de selección)
            _manager = UnityEngine.Object.FindFirstObjectByType<CharacterSelectionManager>();

            if (_manager != null)
            {
                _manager.OnInicioJuego += OnJuegoIniciado;
            }
            else
            {
                // Cargar la escena de selección de forma aditiva
                _loadedAdditively = true;
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.LoadScene("CharacterSelection", LoadSceneMode.Additive);
            }
        }

        public void Exit()
        {
            Debug.Log("[CharacterSelectionFlow] ← Exit: Saliendo de selección de personaje");

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded -= OnGameplaySceneLoaded;

            if (_manager != null)
                _manager.OnInicioJuego -= OnJuegoIniciado;

            // Descargar la escena de selección si fue cargada aditivamente y aún existe
            if (_loadedAdditively)
            {
                var scene = SceneManager.GetSceneByName("CharacterSelection");
                if (scene.isLoaded)
                {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "CharacterSelection") return;

            SceneManager.sceneLoaded -= OnSceneLoaded;

            _manager = UnityEngine.Object.FindFirstObjectByType<CharacterSelectionManager>();

            if (_manager != null)
            {
                _manager.OnInicioJuego += OnJuegoIniciado;
            }
            else
            {
                Debug.LogError("[CharacterSelectionFlow] No se encontró CharacterSelectionManager en la escena cargada.");
            }
        }

        private void OnJuegoIniciado()
        {
            // IniciarJuego() carga la escena de gameplay con LoadScene (single).
            // Deferimos la transición del flow state hasta que la nueva escena esté lista,
            // para que ExplorationFlowState.Enter() encuentre los sistemas del gameplay activos.
            SceneManager.sceneLoaded += OnGameplaySceneLoaded;
        }

        private void OnGameplaySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnGameplaySceneLoaded;
            // La escena de CharacterSelection ya fue destruida por el LoadScene single.
            _loadedAdditively = false;
            GameFlowController.Instance.Replace(new ExplorationFlowState());
        }
    }
}
