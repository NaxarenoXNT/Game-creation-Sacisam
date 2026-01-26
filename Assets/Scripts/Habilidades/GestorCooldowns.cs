using System.Collections.Generic;
using UnityEngine;

namespace Habilidades
{
    /// <summary>
    /// Gestiona los cooldowns de habilidades para una entidad.
    /// </summary>
    [System.Serializable]
    public class GestorCooldowns
    {
        // Diccionario: nombreHabilidad -> turnosRestantes
        private Dictionary<string, int> cooldowns = new Dictionary<string, int>();
        
        /// <summary>
        /// Evento disparado cuando una habilidad sale de cooldown.
        /// </summary>
        public event System.Action<string> OnHabilidadDisponible;
        
        /// <summary>
        /// Verifica si una habilidad está disponible (cooldown = 0).
        /// </summary>
        public bool EstaDisponible(HabilidadData habilidad)
        {
            if (habilidad == null) return false;
            
            if (!cooldowns.ContainsKey(habilidad.nombreHabilidad))
                return true;
                
            return cooldowns[habilidad.nombreHabilidad] <= 0;
        }
        
        /// <summary>
        /// Verifica si una habilidad está disponible por nombre.
        /// </summary>
        public bool EstaDisponible(string nombreHabilidad)
        {
            if (string.IsNullOrEmpty(nombreHabilidad)) return false;
            
            if (!cooldowns.ContainsKey(nombreHabilidad))
                return true;
                
            return cooldowns[nombreHabilidad] <= 0;
        }
        
        /// <summary>
        /// Obtiene los turnos restantes de cooldown para una habilidad.
        /// </summary>
        public int ObtenerCooldown(HabilidadData habilidad)
        {
            if (habilidad == null) return 0;
            
            if (cooldowns.ContainsKey(habilidad.nombreHabilidad))
                return cooldowns[habilidad.nombreHabilidad];
                
            return 0;
        }
        
        /// <summary>
        /// Inicia el cooldown de una habilidad después de usarla.
        /// </summary>
        public void IniciarCooldown(HabilidadData habilidad)
        {
            if (habilidad == null || habilidad.cooldownTurnos <= 0) return;
            
            cooldowns[habilidad.nombreHabilidad] = (int)habilidad.cooldownTurnos;
            Debug.Log($"⏱️ {habilidad.nombreHabilidad} en cooldown por {habilidad.cooldownTurnos} turnos");
        }
        
        /// <summary>
        /// Reduce todos los cooldowns al inicio del turno.
        /// </summary>
        public void ProcesarInicioTurno()
        {
            var habilidadesANotificar = new List<string>();
            var keys = new List<string>(cooldowns.Keys);
            
            foreach (var nombre in keys)
            {
                if (cooldowns[nombre] > 0)
                {
                    cooldowns[nombre]--;
                    
                    if (cooldowns[nombre] <= 0)
                    {
                        habilidadesANotificar.Add(nombre);
                    }
                }
            }
            
            // Notificar habilidades que salieron de cooldown
            foreach (var nombre in habilidadesANotificar)
            {
                OnHabilidadDisponible?.Invoke(nombre);
                Debug.Log($"✅ {nombre} está disponible nuevamente");
            }
        }
        
        /// <summary>
        /// Resetea el cooldown de una habilidad específica.
        /// </summary>
        public void ResetearCooldown(string nombreHabilidad)
        {
            if (cooldowns.ContainsKey(nombreHabilidad))
            {
                cooldowns[nombreHabilidad] = 0;
                OnHabilidadDisponible?.Invoke(nombreHabilidad);
            }
        }
        
        /// <summary>
        /// Resetea todos los cooldowns.
        /// </summary>
        public void ResetearTodos()
        {
            var keys = new List<string>(cooldowns.Keys);
            foreach (var nombre in keys)
            {
                cooldowns[nombre] = 0;
                OnHabilidadDisponible?.Invoke(nombre);
            }
        }
        
        /// <summary>
        /// Reduce el cooldown de una habilidad por una cantidad específica.
        /// </summary>
        public void ReducirCooldown(string nombreHabilidad, int cantidad)
        {
            if (cooldowns.ContainsKey(nombreHabilidad))
            {
                cooldowns[nombreHabilidad] = Mathf.Max(0, cooldowns[nombreHabilidad] - cantidad);
                
                if (cooldowns[nombreHabilidad] <= 0)
                {
                    OnHabilidadDisponible?.Invoke(nombreHabilidad);
                }
            }
        }
        
        /// <summary>
        /// Obtiene una lista de todas las habilidades en cooldown con sus turnos restantes.
        /// </summary>
        public Dictionary<string, int> ObtenerTodosLosCooldowns()
        {
            return new Dictionary<string, int>(cooldowns);
        }
    }
}
