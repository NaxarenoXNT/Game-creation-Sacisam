using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Padres;
using Interfaces;
using Managers;

namespace Habilidades
{
    /// <summary>
    /// Gestiona las habilidades activas de una entidad.
    /// Cada entidad (jugador/enemigo) tiene su propio GestorHabilidades.
    /// Permite agregar, remover y consultar habilidades dinámicamente.
    /// </summary>
    [System.Serializable]
    public class GestorHabilidades
    {
        // Lista de habilidades que posee la entidad
        [SerializeField]
        private List<HabilidadData> habilidades = new List<HabilidadData>();
        
        // Cooldowns de las habilidades
        private GestorCooldowns gestorCooldowns = new GestorCooldowns();
        
        // Referencia al portador
        private IEntidadCombate portador;

        // Límite de habilidades equipadas (0 = sin límite)
        private int limiteHabilidades = 0;

        #region Eventos
        
        /// <summary>
        /// Evento disparado cuando se agrega una habilidad.
        /// </summary>
        public event Action<HabilidadData> OnHabilidadAgregada;

        /// <summary>
        /// Evento disparado cuando se remueve una habilidad.
        /// </summary>
        public event Action<HabilidadData> OnHabilidadRemovida;

        /// <summary>
        /// Evento disparado cuando cambia la lista de habilidades.
        /// </summary>
        public event Action OnHabilidadesCambiadas;

        /// <summary>
        /// Evento disparado cuando una habilidad se usa.
        /// </summary>
        public event Action<HabilidadData> OnHabilidadUsada;

        #endregion

        #region Constructores

        public GestorHabilidades(IEntidadCombate portador, int limite = 0)
        {
            this.portador = portador;
            this.limiteHabilidades = limite;
        }

        /// <summary>
        /// Constructor con habilidades iniciales (desde ClaseData/EnemigoData).
        /// </summary>
        public GestorHabilidades(IEntidadCombate portador, IEnumerable<HabilidadData> habilidadesIniciales, int limite = 0)
            : this(portador, limite)
        {
            if (habilidadesIniciales != null)
            {
                foreach (var hab in habilidadesIniciales)
                {
                    AgregarHabilidad(hab, notificar: false);
                }
            }
        }

        #endregion

        #region Agregar/Remover Habilidades

        /// <summary>
        /// Agrega una habilidad al repertorio.
        /// </summary>
        /// <returns>True si se agregó correctamente.</returns>
        public bool AgregarHabilidad(HabilidadData habilidad, bool notificar = true)
        {
            if (habilidad == null) return false;
            
            // Verificar si ya la tiene
            if (habilidades.Contains(habilidad))
            {
                Debug.LogWarning($"La entidad ya posee la habilidad '{habilidad.nombreHabilidad}'");
                return false;
            }

            // Verificar límite
            if (limiteHabilidades > 0 && habilidades.Count >= limiteHabilidades)
            {
                Debug.LogWarning($"Límite de habilidades alcanzado ({limiteHabilidades})");
                return false;
            }

            // Verificar restricciones de facción
            if (habilidad.faccionesProhibidas.Contains(portador.TipoEntidad))
            {
                Debug.LogWarning($"La facción {portador.TipoEntidad} no puede usar '{habilidad.nombreHabilidad}'");
                return false;
            }

            habilidades.Add(habilidad);
            
            if (notificar)
            {
                OnHabilidadAgregada?.Invoke(habilidad);
                OnHabilidadesCambiadas?.Invoke();
                
                // Publicar al EventBus para sistema de evolución
                EventBus.Publicar(new EventoHabilidadDesbloqueada
                {
                    Entidad = portador,
                    Habilidad = habilidad
                });
                
                Debug.Log($"✨ Habilidad '{habilidad.nombreHabilidad}' aprendida");
            }
            
            return true;
        }

        /// <summary>
        /// Remueve una habilidad del repertorio.
        /// </summary>
        public bool RemoverHabilidad(HabilidadData habilidad)
        {
            if (habilidad == null) return false;
            if (!habilidades.Contains(habilidad)) return false;

            habilidades.Remove(habilidad);
            OnHabilidadRemovida?.Invoke(habilidad);
            OnHabilidadesCambiadas?.Invoke();
            
            // Publicar al EventBus para sistema de evolución
            EventBus.Publicar(new EventoHabilidadRemovida
            {
                Entidad = portador,
                Habilidad = habilidad
            });
            
            Debug.Log($"💨 Habilidad '{habilidad.nombreHabilidad}' olvidada");
            return true;
        }

        /// <summary>
        /// Remueve una habilidad por nombre.
        /// </summary>
        public bool RemoverHabilidad(string nombreHabilidad)
        {
            var hab = habilidades.Find(h => h.nombreHabilidad == nombreHabilidad);
            return RemoverHabilidad(hab);
        }

        /// <summary>
        /// Reemplaza una habilidad por otra.
        /// </summary>
        public bool ReemplazarHabilidad(HabilidadData vieja, HabilidadData nueva)
        {
            int index = habilidades.IndexOf(vieja);
            if (index == -1) return false;
            if (nueva == null) return false;

            habilidades[index] = nueva;
            OnHabilidadRemovida?.Invoke(vieja);
            OnHabilidadAgregada?.Invoke(nueva);
            OnHabilidadesCambiadas?.Invoke();
            
            return true;
        }

        /// <summary>
        /// Limpia todas las habilidades.
        /// </summary>
        public void LimpiarHabilidades()
        {
            habilidades.Clear();
            gestorCooldowns = new GestorCooldowns();
            OnHabilidadesCambiadas?.Invoke();
        }

        #endregion

        #region Consultas

        /// <summary>
        /// Obtiene todas las habilidades.
        /// </summary>
        public IReadOnlyList<HabilidadData> ObtenerTodas()
        {
            return habilidades.AsReadOnly();
        }

        /// <summary>
        /// Obtiene habilidades disponibles (sin cooldown y con recursos suficientes).
        /// </summary>
        public List<HabilidadData> ObtenerDisponibles(IEntidadCombate objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
        {
            return habilidades.Where(h => 
                gestorCooldowns.EstaDisponible(h) && 
                h.EsViable(portador, objetivo, aliados, enemigos)
            ).ToList();
        }

        /// <summary>
        /// Obtiene habilidades por categoría.
        /// </summary>
        public List<HabilidadData> ObtenerPorCategoria(Flags.CategoriaHabilidad categoria)
        {
            return habilidades.Where(h => h.categoria == categoria).ToList();
        }

        /// <summary>
        /// Verifica si tiene una habilidad específica.
        /// </summary>
        public bool TieneHabilidad(HabilidadData habilidad)
        {
            return habilidades.Contains(habilidad);
        }

        /// <summary>
        /// Verifica si tiene una habilidad por nombre.
        /// </summary>
        public bool TieneHabilidad(string nombreHabilidad)
        {
            return habilidades.Exists(h => h.nombreHabilidad == nombreHabilidad);
        }

        /// <summary>
        /// Obtiene una habilidad por nombre.
        /// </summary>
        public HabilidadData ObtenerPorNombre(string nombreHabilidad)
        {
            return habilidades.Find(h => h.nombreHabilidad == nombreHabilidad);
        }

        /// <summary>
        /// Obtiene una habilidad por índice.
        /// </summary>
        public HabilidadData ObtenerPorIndice(int indice)
        {
            if (indice < 0 || indice >= habilidades.Count) return null;
            return habilidades[indice];
        }

        /// <summary>
        /// Cantidad de habilidades.
        /// </summary>
        public int Cantidad => habilidades.Count;

        /// <summary>
        /// Límite de habilidades (0 = sin límite).
        /// </summary>
        public int Limite => limiteHabilidades;

        /// <summary>
        /// Espacios disponibles.
        /// </summary>
        public int EspaciosDisponibles => limiteHabilidades > 0 ? limiteHabilidades - habilidades.Count : int.MaxValue;

        #endregion

        #region Uso de Habilidades

        /// <summary>
        /// Verifica si una habilidad puede usarse ahora.
        /// </summary>
        public bool PuedeUsar(HabilidadData habilidad, IEntidadCombate objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
        {
            if (!TieneHabilidad(habilidad)) return false;
            if (!gestorCooldowns.EstaDisponible(habilidad)) return false;
            return habilidad.EsViable(portador, objetivo, aliados, enemigos);
        }

        /// <summary>
        /// Usa una habilidad (consume recursos, inicia cooldown).
        /// Retorna true si se ejecutó correctamente.
        /// </summary>
        public bool UsarHabilidad(HabilidadData habilidad, IEntidadCombate objetivo, List<IEntidadCombate> aliados, List<IEntidadCombate> enemigos)
        {
            if (!PuedeUsar(habilidad, objetivo, aliados, enemigos))
            {
                Debug.LogWarning($"No se puede usar '{habilidad.nombreHabilidad}'");
                return false;
            }

            // Consumir recursos
            habilidad.ConsumirRecursos(portador);
            
            // Iniciar cooldown
            gestorCooldowns.IniciarCooldown(habilidad);
            
            // Ejecutar la habilidad
            habilidad.Ejecutar(portador, objetivo, aliados, enemigos);
            
            // Notificar evento local
            OnHabilidadUsada?.Invoke(habilidad);
            
            // Publicar al EventBus para sistema de evolución
            EventBus.Publicar(new EventoHabilidadUsada
            {
                Invocador = portador,
                Objetivo = objetivo,
                Habilidad = habilidad
            });
            
            return true;
        }

        /// <summary>
        /// Obtiene el cooldown restante de una habilidad.
        /// </summary>
        public int ObtenerCooldown(HabilidadData habilidad)
        {
            return gestorCooldowns.ObtenerCooldown(habilidad);
        }

        /// <summary>
        /// Verifica si una habilidad está en cooldown.
        /// </summary>
        public bool EstaEnCooldown(HabilidadData habilidad)
        {
            return !gestorCooldowns.EstaDisponible(habilidad);
        }

        /// <summary>
        /// Procesa cooldowns al inicio del turno.
        /// </summary>
        public void ProcesarInicioTurno()
        {
            gestorCooldowns.ProcesarInicioTurno();
        }

        /// <summary>
        /// Resetea todos los cooldowns.
        /// </summary>
        public void ResetearCooldowns()
        {
            gestorCooldowns = new GestorCooldowns();
        }

        /// <summary>
        /// Acceso al gestor de cooldowns (para UI, etc.).
        /// </summary>
        public GestorCooldowns Cooldowns => gestorCooldowns;

        #endregion

        #region Serialización (para Save/Load)

        /// <summary>
        /// Obtiene los nombres de las habilidades (para guardar).
        /// </summary>
        public List<string> ObtenerNombresParaGuardar()
        {
            return habilidades.Select(h => h.nombreHabilidad).ToList();
        }

        /// <summary>
        /// Carga habilidades desde nombres (requiere un registry de habilidades).
        /// </summary>
        public void CargarDesdeNombres(List<string> nombres, Func<string, HabilidadData> buscarHabilidad)
        {
            habilidades.Clear();
            foreach (var nombre in nombres)
            {
                var hab = buscarHabilidad(nombre);
                if (hab != null)
                {
                    habilidades.Add(hab);
                }
                else
                {
                    Debug.LogWarning($"No se encontró la habilidad '{nombre}' al cargar");
                }
            }
            OnHabilidadesCambiadas?.Invoke();
        }

        #endregion
    }
}
