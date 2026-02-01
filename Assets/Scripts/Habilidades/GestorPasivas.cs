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
                pasiva.ProcesarTurno(portador);
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
                pasiva.ActualizarEstado(portador);
            }
        }

        /// <summary>
        /// Activa todas las pasivas. Llamar al inicio del combate.
        /// </summary>
        public void ActivarTodas()
        {
            foreach (var pasiva in pasivas)
            {
                if (!pasiva.EstaActiva)
                    pasiva.Activar(portador);
            }
        }

        /// <summary>
        /// Desactiva todas las pasivas. Llamar al fin del combate si es necesario.
        /// </summary>
        public void DesactivarTodas()
        {
            foreach (var pasiva in pasivas)
            {
                if (pasiva.EstaActiva)
                    pasiva.Desactivar(portador);
            }
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
            return pasivas.FindAll(p => p.EstaActiva);
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
