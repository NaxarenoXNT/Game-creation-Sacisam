using System.Collections.Generic;
using UnityEngine;
using Padres;
using Interfaces;
using Managers;

namespace Habilidades
{
    /// <summary>
    /// Gestiona las habilidades pasivas de una entidad.
    /// Cada entidad que pueda tener pasivas debe tener una instancia de este gestor.
    /// </summary>
    [System.Serializable]
    public class GestorPasivas
    {
        // Lista de pasivas que posee la entidad
        private List<PasivaData> pasivas = new List<PasivaData>();
        
        // Tracking de activación por instancia (resuelve estado mutable en SO)
        private HashSet<PasivaData> pasivasActivas = new HashSet<PasivaData>();
        
        // Referencia al portador
        private Entidad portador;

        /// <summary>
        /// Evento disparado cuando se agrega una pasiva.
        /// </summary>
        public event System.Action<PasivaData> OnPasivaAgregada;

        /// <summary>
        /// Evento disparado cuando se remueve una pasiva.
        /// </summary>
        public event System.Action<PasivaData> OnPasivaRemovida;

        public GestorPasivas(Entidad portador)
        {
            this.portador = portador;
        }

        /// <summary>
        /// Agrega y activa una pasiva.
        /// </summary>
        public bool AgregarPasiva(PasivaData pasiva)
        {
            if (pasiva == null) return false;
            if (pasivas.Contains(pasiva))
            {
                Debug.LogWarning($"La entidad ya tiene la pasiva '{pasiva.nombrePasiva}'");
                return false;
            }

            if (!pasiva.PuedeActivarse(portador))
            {
                Debug.LogWarning($"La entidad no puede tener la pasiva '{pasiva.nombrePasiva}'");
                return false;
            }

            pasivas.Add(pasiva);
            pasiva.Activar(portador);
            pasivasActivas.Add(pasiva);
            OnPasivaAgregada?.Invoke(pasiva);
            
            // Publicar al EventBus para sistema de evolución
            EventBus.Publicar(new EventoPasivaDesbloqueada
            {
                Entidad = portador,
                Pasiva = pasiva
            });
            
            return true;
        }

        /// <summary>
        /// Remueve y desactiva una pasiva.
        /// </summary>
        public bool RemoverPasiva(PasivaData pasiva)
        {
            if (pasiva == null) return false;
            if (!pasivas.Contains(pasiva)) return false;

            pasiva.Desactivar(portador);
            pasivasActivas.Remove(pasiva);
            pasivas.Remove(pasiva);
            OnPasivaRemovida?.Invoke(pasiva);
            
            // Publicar al EventBus para sistema de evolución
            EventBus.Publicar(new EventoPasivaRemovida
            {
                Entidad = portador,
                Pasiva = pasiva
            });
            
            return true;
        }

        /// <summary>
        /// Procesa todas las pasivas al inicio del turno.
        /// </summary>
        public void ProcesarInicioTurno()
        {
            foreach (var pasiva in pasivas)
            {
                bool activa = pasivasActivas.Contains(pasiva);
                
                // Re-verificar condiciones cada turno para pasivas condicionales
                if (!pasiva.siempreActiva)
                {
                    bool deberiaEstarActiva = pasiva.DeberiaEstarActiva(portador);
                    if (deberiaEstarActiva && !activa)
                    {
                        pasiva.Activar(portador);
                        pasivasActivas.Add(pasiva);
                        activa = true;
                    }
                    else if (!deberiaEstarActiva && activa)
                    {
                        pasiva.Desactivar(portador);
                        pasivasActivas.Remove(pasiva);
                        activa = false;
                    }
                }
                
                pasiva.ProcesarTurno(portador, activa);
            }
        }

        /// <summary>
        /// Actualiza el estado de todas las pasivas condicionales.
        /// Llamar cuando cambia el estado del portador (HP, etc.).
        /// </summary>
        public void ActualizarEstados()
        {
            foreach (var pasiva in pasivas)
            {
                if (pasiva.siempreActiva) continue;
                
                bool activa = pasivasActivas.Contains(pasiva);
                bool deberiaEstarActiva = pasiva.DeberiaEstarActiva(portador);
                
                if (deberiaEstarActiva && !activa)
                {
                    pasiva.Activar(portador);
                    pasivasActivas.Add(pasiva);
                }
                else if (!deberiaEstarActiva && activa)
                {
                    pasiva.Desactivar(portador);
                    pasivasActivas.Remove(pasiva);
                }
            }
        }

        /// <summary>
        /// Activa todas las pasivas. Llamar al inicio del combate.
        /// </summary>
        public void ActivarTodas()
        {
            foreach (var pasiva in pasivas)
            {
                if (!pasivasActivas.Contains(pasiva))
                {
                    pasiva.Activar(portador);
                    pasivasActivas.Add(pasiva);
                }
            }
        }

        /// <summary>
        /// Desactiva todas las pasivas. Llamar al fin del combate si es necesario.
        /// </summary>
        public void DesactivarTodas()
        {
            foreach (var pasiva in pasivas)
            {
                if (pasivasActivas.Contains(pasiva))
                {
                    pasiva.Desactivar(portador);
                }
            }
            pasivasActivas.Clear();
        }
        
        /// <summary>
        /// Verifica si una pasiva está actualmente activa en este portador.
        /// </summary>
        public bool EstaPasivaActiva(PasivaData pasiva)
        {
            return pasivasActivas.Contains(pasiva);
        }

        /// <summary>
        /// Verifica si la entidad tiene una pasiva específica.
        /// </summary>
        public bool TienePasiva(PasivaData pasiva)
        {
            return pasivas.Contains(pasiva);
        }

        /// <summary>
        /// Verifica si la entidad tiene una pasiva por nombre.
        /// </summary>
        public bool TienePasiva(string nombrePasiva)
        {
            return pasivas.Exists(p => p.nombrePasiva == nombrePasiva);
        }

        /// <summary>
        /// Obtiene todas las pasivas activas.
        /// </summary>
        public List<PasivaData> ObtenerPasivasActivas()
        {
            return pasivas.FindAll(p => pasivasActivas.Contains(p));
        }

        /// <summary>
        /// Obtiene todas las pasivas (activas e inactivas).
        /// </summary>
        public IReadOnlyList<PasivaData> ObtenerTodasLasPasivas()
        {
            return pasivas.AsReadOnly();
        }

        /// <summary>
        /// Cantidad total de pasivas.
        /// </summary>
        public int CantidadPasivas => pasivas.Count;
    }
}
