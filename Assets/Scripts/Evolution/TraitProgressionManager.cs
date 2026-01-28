using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Evolution
{
    /// <summary>
    /// Manager central para el sistema de traits y cadenas de progresión.
    /// Carga todas las cadenas y traits, y gestiona su disponibilidad.
    /// </summary>
    public class TraitProgressionManager : MonoBehaviour
    {
        private static TraitProgressionManager _instance;
        public static TraitProgressionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TraitProgressionManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("TraitProgressionManager");
                        _instance = go.AddComponent<TraitProgressionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Configuración")]
        [Tooltip("Ruta en Resources donde están las TraitChains")]
        [SerializeField] private string traitChainsPath = "TraitChains";
        
        [Tooltip("Ruta en Resources donde están los Traits individuales")]
        [SerializeField] private string traitsPath = "Traits";

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        // Cache de todas las cadenas cargadas
        private List<TraitChainDefinition> _allChains = new List<TraitChainDefinition>();
        
        // Cache de todos los traits individuales (no parte de cadenas)
        private List<TraitDefinition> _standaloneTraits = new List<TraitDefinition>();
        
        // Referencia al evaluador
        private EvolutionEvaluator _evaluator;

        public IReadOnlyList<TraitChainDefinition> AllChains => _allChains;
        public IReadOnlyList<TraitDefinition> StandaloneTraits => _standaloneTraits;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            _evaluator = new EvolutionEvaluator();
            LoadAllContent();
        }

        /// <summary>
        /// Carga todas las cadenas y traits desde Resources.
        /// </summary>
        public void LoadAllContent()
        {
            // Cargar cadenas
            _allChains = Resources.LoadAll<TraitChainDefinition>(traitChainsPath).ToList();
            
            // Cargar traits individuales
            _standaloneTraits = Resources.LoadAll<TraitDefinition>(traitsPath).ToList();

            if (logDebug)
            {
                Debug.Log($"[TraitProgressionManager] Cargadas {_allChains.Count} cadenas, " +
                          $"{_standaloneTraits.Count} traits individuales.");
            }
        }

        /// <summary>
        /// Obtiene todos los traits individuales disponibles para una clase y estado dados.
        /// </summary>
        public List<TraitDefinition> GetTraitsDisponibles(EvolutionState state, ClaseData claseActual)
        {
            return _evaluator.FiltrarTraitsDisponibles(_standaloneTraits, state, claseActual);
        }

        /// <summary>
        /// Obtiene información de cadenas con nodos disponibles.
        /// </summary>
        public List<(TraitChainDefinition chain, int nodoIndex)> GetCadenasConNodosDisponibles(EvolutionState state, ClaseData claseActual)
        {
            var resultado = new List<(TraitChainDefinition, int)>();

            foreach (var chain in _allChains)
            {
                int nodoIndex = GetNodoDisponibleDeCadena(chain, state, claseActual);
                if (nodoIndex >= 0)
                {
                    resultado.Add((chain, nodoIndex));
                }
            }

            return resultado;
        }

        /// <summary>
        /// Obtiene el siguiente trait disponible de una cadena específica.
        /// Retorna el índice del nodo disponible, o -1 si no hay ninguno.
        /// </summary>
        public int GetNodoDisponibleDeCadena(TraitChainDefinition chain, EvolutionState state, ClaseData claseActual)
        {
            // Verificar clases bloqueadas
            if (claseActual != null && chain.clasesBloqueadas.Contains(claseActual))
                return -1;

            // Verificar exclusiones globales
            if (chain.exclusionesGlobales.Any(excl => excl != null && state.traitStacks.ContainsKey(excl.id)))
                return -1;

            // Encontrar el siguiente nodo no completado
            for (int i = 0; i < chain.nodos.Count; i++)
            {
                string traitId = chain.GetTraitId(i);
                
                // Si ya tiene este trait, pasar al siguiente
                if (state.traitStacks.ContainsKey(traitId))
                    continue;

                // Verificar si cumple las condiciones para este nodo
                if (chain.CumpleCondicionesNodo(i, state))
                {
                    return i;
                }

                // Si no cumple condiciones del nodo actual, no puede acceder a los siguientes
                break;
            }

            return -1;
        }

        /// <summary>
        /// Obtiene el progreso de una cadena específica.
        /// </summary>
        public ChainProgressInfo GetProgresoDeCapena(TraitChainDefinition chain, EvolutionState state)
        {
            var info = new ChainProgressInfo
            {
                chain = chain,
                nodosCompletados = 0,
                totalNodos = chain.nodos.Count,
                puedeDesbloquearEvolucion = false
            };

            for (int i = 0; i < chain.nodos.Count; i++)
            {
                if (state.traitStacks.ContainsKey(chain.GetTraitId(i)))
                {
                    info.nodosCompletados++;
                }
            }

            // Verificar si puede desbloquear la evolución final
            if (chain.evolucionFinal != null && info.nodosCompletados == info.totalNodos)
            {
                info.puedeDesbloquearEvolucion = _evaluator.CumpleTodasLasCondiciones(
                    chain.condicionesEvolucionFinal, state);
            }

            return info;
        }

        /// <summary>
        /// Obtiene el progreso de todas las cadenas.
        /// </summary>
        public List<ChainProgressInfo> GetProgresoTodasLasCadenas(EvolutionState state)
        {
            return _allChains.Select(chain => GetProgresoDeCapena(chain, state)).ToList();
        }

        /// <summary>
        /// Registra que un trait ha sido obtenido.
        /// </summary>
        public void RegistrarTraitObtenido(string traitId, EvolutionState state)
        {
            if (!state.traitStacks.ContainsKey(traitId))
            {
                state.traitStacks[traitId] = 1;
            }
            else
            {
                // Para traits stackeables
                state.traitStacks[traitId]++;
            }

            if (logDebug)
            {
                Debug.Log($"[TraitProgressionManager] Trait obtenido: {traitId}");
            }
        }

        /// <summary>
        /// Obtiene un trait individual por su ID.
        /// </summary>
        public TraitDefinition GetTraitById(string traitId)
        {
            return _standaloneTraits.FirstOrDefault(t => t.id == traitId);
        }

        /// <summary>
        /// Obtiene la cadena a la que pertenece un trait.
        /// </summary>
        public TraitChainDefinition GetCadenaDeTrait(string traitId)
        {
            foreach (var chain in _allChains)
            {
                for (int i = 0; i < chain.nodos.Count; i++)
                {
                    if (chain.GetTraitId(i) == traitId)
                    {
                        return chain;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Obtiene el índice del nodo en su cadena.
        /// </summary>
        public int GetIndiceEnCadena(string traitId)
        {
            foreach (var chain in _allChains)
            {
                for (int i = 0; i < chain.nodos.Count; i++)
                {
                    if (chain.GetTraitId(i) == traitId)
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
    }

    /// <summary>
    /// Información de progreso de una cadena.
    /// </summary>
    public class ChainProgressInfo
    {
        public TraitChainDefinition chain;
        public int nodosCompletados;
        public int totalNodos;
        public bool puedeDesbloquearEvolucion;

        public float Progreso => totalNodos > 0 ? (float)nodosCompletados / totalNodos : 0f;
        public bool CadenaCompleta => nodosCompletados >= totalNodos;
    }
}
