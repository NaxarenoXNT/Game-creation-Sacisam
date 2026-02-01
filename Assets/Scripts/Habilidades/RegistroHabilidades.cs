using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Habilidades
{
    /// <summary>
    /// Registro centralizado de todas las habilidades y pasivas del juego.
    /// Usado para cargar/guardar partidas y buscar habilidades por nombre.
    /// Singleton accesible desde cualquier parte del código.
    /// </summary>
    [CreateAssetMenu(fileName = "RegistroHabilidades", menuName = "Combate/Registro de Habilidades")]
    public class RegistroHabilidades : ScriptableObject
    {
        private static RegistroHabilidades _instancia;
        
        [Header("Habilidades Activas")]
        [Tooltip("Todas las habilidades activas del juego")]
        public List<HabilidadData> todasLasHabilidades = new List<HabilidadData>();
        
        [Header("Habilidades Pasivas")]
        [Tooltip("Todas las habilidades pasivas del juego")]
        public List<PasivaData> todasLasPasivas = new List<PasivaData>();

        // Diccionarios para búsqueda rápida (se construyen en runtime)
        private Dictionary<string, HabilidadData> _habilidadesPorNombre;
        private Dictionary<string, PasivaData> _pasivasPorNombre;

        /// <summary>
        /// Instancia singleton. Se carga automáticamente desde Resources.
        /// </summary>
        public static RegistroHabilidades Instancia
        {
            get
            {
                if (_instancia == null)
                {
                    _instancia = Resources.Load<RegistroHabilidades>("RegistroHabilidades");
                    if (_instancia == null)
                    {
                        Debug.LogError("No se encontró RegistroHabilidades en Resources. Crear uno con el menú Combate > Registro de Habilidades y colocarlo en Assets/Resources/");
                    }
                    else
                    {
                        _instancia.InicializarDiccionarios();
                    }
                }
                return _instancia;
            }
        }

        private void OnEnable()
        {
            InicializarDiccionarios();
        }

        private void InicializarDiccionarios()
        {
            _habilidadesPorNombre = new Dictionary<string, HabilidadData>();
            _pasivasPorNombre = new Dictionary<string, PasivaData>();

            foreach (var hab in todasLasHabilidades)
            {
                if (hab != null && !string.IsNullOrEmpty(hab.nombreHabilidad))
                {
                    if (!_habilidadesPorNombre.ContainsKey(hab.nombreHabilidad))
                        _habilidadesPorNombre[hab.nombreHabilidad] = hab;
                    else
                        Debug.LogWarning($"Habilidad duplicada: {hab.nombreHabilidad}");
                }
            }

            foreach (var pas in todasLasPasivas)
            {
                if (pas != null && !string.IsNullOrEmpty(pas.nombrePasiva))
                {
                    if (!_pasivasPorNombre.ContainsKey(pas.nombrePasiva))
                        _pasivasPorNombre[pas.nombrePasiva] = pas;
                    else
                        Debug.LogWarning($"Pasiva duplicada: {pas.nombrePasiva}");
                }
            }
        }

        #region Búsqueda de Habilidades

        /// <summary>
        /// Busca una habilidad por nombre.
        /// </summary>
        public HabilidadData BuscarHabilidad(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return null;
            
            if (_habilidadesPorNombre == null)
                InicializarDiccionarios();
            
            _habilidadesPorNombre.TryGetValue(nombre, out var hab);
            return hab;
        }

        /// <summary>
        /// Busca una pasiva por nombre.
        /// </summary>
        public PasivaData BuscarPasiva(string nombre)
        {
            if (string.IsNullOrEmpty(nombre)) return null;
            
            if (_pasivasPorNombre == null)
                InicializarDiccionarios();
            
            _pasivasPorNombre.TryGetValue(nombre, out var pas);
            return pas;
        }

        /// <summary>
        /// Obtiene habilidades por categoría.
        /// </summary>
        public List<HabilidadData> ObtenerPorCategoria(Flags.CategoriaHabilidad categoria)
        {
            return todasLasHabilidades.Where(h => h != null && h.categoria == categoria).ToList();
        }

        /// <summary>
        /// Obtiene pasivas por categoría.
        /// </summary>
        public List<PasivaData> ObtenerPasivasPorCategoria(CategoriaPasiva categoria)
        {
            return todasLasPasivas.Where(p => p != null && p.categoria == categoria).ToList();
        }

        /// <summary>
        /// Verifica si existe una habilidad.
        /// </summary>
        public bool ExisteHabilidad(string nombre)
        {
            return BuscarHabilidad(nombre) != null;
        }

        /// <summary>
        /// Verifica si existe una pasiva.
        /// </summary>
        public bool ExistePasiva(string nombre)
        {
            return BuscarPasiva(nombre) != null;
        }

        #endregion

        #region Utilidades para Editor

#if UNITY_EDITOR
        /// <summary>
        /// Carga automáticamente todas las habilidades del proyecto.
        /// Solo funciona en el Editor.
        /// </summary>
        [ContextMenu("Cargar Todas las Habilidades")]
        public void CargarTodasLasHabilidades()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:HabilidadData");
            todasLasHabilidades.Clear();
            
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                HabilidadData hab = UnityEditor.AssetDatabase.LoadAssetAtPath<HabilidadData>(path);
                if (hab != null)
                    todasLasHabilidades.Add(hab);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Cargadas {todasLasHabilidades.Count} habilidades");
        }

        /// <summary>
        /// Carga automáticamente todas las pasivas del proyecto.
        /// Solo funciona en el Editor.
        /// </summary>
        [ContextMenu("Cargar Todas las Pasivas")]
        public void CargarTodasLasPasivas()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PasivaData");
            todasLasPasivas.Clear();
            
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                PasivaData pas = UnityEditor.AssetDatabase.LoadAssetAtPath<PasivaData>(path);
                if (pas != null)
                    todasLasPasivas.Add(pas);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"Cargadas {todasLasPasivas.Count} pasivas");
        }

        [ContextMenu("Cargar Todo")]
        public void CargarTodo()
        {
            CargarTodasLasHabilidades();
            CargarTodasLasPasivas();
        }
#endif

        #endregion
    }
}
