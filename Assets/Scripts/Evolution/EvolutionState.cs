using System;
using System.Collections.Generic;
using Flags;

namespace Evolution
{
    /// <summary>
    /// Estado completo del jugador para evaluación de condiciones.
    /// Persiste entre escenas y se guarda/carga con SaveSystem.
    /// </summary>
    [Serializable]
    public class EvolutionState
    {
        // ========== Básico ==========
        public int nivelJugador;
        public float karma; // -1 a 1
        public int seed;

        // ========== Kills ==========
        public Dictionary<string, int> killsPorTipo = new Dictionary<string, int>();

        // ========== Habilidades ==========
        public Dictionary<string, int> usosHabilidad = new Dictionary<string, int>();
        public HashSet<string> habilidadesDesbloqueadas = new HashSet<string>();

        // ========== Misiones ==========
        public HashSet<string> misionesCompletadas = new HashSet<string>();

        // ========== Items ==========
        public Dictionary<string, int> itemsEnInventario = new Dictionary<string, int>();
        public Dictionary<string, int> itemsUsados = new Dictionary<string, int>();

        // ========== Economía ==========
        public int oroActual;
        public int oroGastado;

        // ========== Facciones ==========
        public Dictionary<string, float> reputaciones = new Dictionary<string, float>();
        public Dictionary<string, int> rangosFaccion = new Dictionary<string, int>();

        // ========== Combate ==========
        public int dañoInfligidoTotal;
        public int dañoRecibidoTotal;
        public int curacionTotal;
        public int combatesSinDaño;
        public int comboMaximo;
        public int sacrificios;
        public int muertes;

        // ========== Estados de Combate ==========
        public HashSet<string> estadosActivos = new HashSet<string>();
        public Dictionary<string, int> estadosAplicados = new Dictionary<string, int>();

        // ========== Exploración ==========
        public HashSet<string> biomasVisitados = new HashSet<string>();

        // ========== Tiempo ==========
        public int tiempoJugadoSegundos;

        // ========== Tags Genéricos ==========
        public HashSet<string> tags = new HashSet<string>();

        // ========== Traits y Evoluciones ==========
        public Dictionary<string, int> traitStacks = new Dictionary<string, int>();
        public HashSet<string> evolucionesAplicadas = new HashSet<string>();

        // ========== Flags Personalizados ==========
        public Dictionary<string, int> customFlags = new Dictionary<string, int>();

        #region Métodos de Registro

        public void RegistrarKill(TipoEntidades tipo)
        {
            string key = tipo.ToString();
            if (!killsPorTipo.ContainsKey(key))
                killsPorTipo[key] = 0;
            killsPorTipo[key]++;
        }

        public void RegistrarKill(string tipoString)
        {
            if (string.IsNullOrEmpty(tipoString)) return;
            if (!killsPorTipo.ContainsKey(tipoString))
                killsPorTipo[tipoString] = 0;
            killsPorTipo[tipoString]++;
        }

        public void RegistrarUsoHabilidad(string habilidadId)
        {
            if (string.IsNullOrEmpty(habilidadId)) return;
            if (!usosHabilidad.ContainsKey(habilidadId))
                usosHabilidad[habilidadId] = 0;
            usosHabilidad[habilidadId]++;
        }

        public void RegistrarMision(string misionId)
        {
            if (!string.IsNullOrEmpty(misionId))
                misionesCompletadas.Add(misionId);
        }

        public void RegistrarDaño(int infligido, int recibido)
        {
            dañoInfligidoTotal += infligido;
            dañoRecibidoTotal += recibido;
        }

        public void RegistrarCuracion(int cantidad)
        {
            curacionTotal += cantidad;
        }

        public void RegistrarUsoItem(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            if (!itemsUsados.ContainsKey(itemId))
                itemsUsados[itemId] = 0;
            itemsUsados[itemId]++;
        }

        public void RegistrarEstadoAplicado(string statusId)
        {
            if (string.IsNullOrEmpty(statusId)) return;
            if (!estadosAplicados.ContainsKey(statusId))
                estadosAplicados[statusId] = 0;
            estadosAplicados[statusId]++;
        }

        public void RegistrarBioma(string biomaId)
        {
            if (!string.IsNullOrEmpty(biomaId))
                biomasVisitados.Add(biomaId);
        }

        public void AñadirTrait(string traitId)
        {
            if (string.IsNullOrEmpty(traitId)) return;
            if (!traitStacks.ContainsKey(traitId))
                traitStacks[traitId] = 0;
            traitStacks[traitId]++;
        }

        public void AñadirEvolucion(string evolucionId)
        {
            if (!string.IsNullOrEmpty(evolucionId))
                evolucionesAplicadas.Add(evolucionId);
        }

        public void ModificarKarma(float delta)
        {
            karma = UnityEngine.Mathf.Clamp(karma + delta, -1f, 1f);
        }

        public void SetCustomFlag(string key, int value)
        {
            customFlags[key] = value;
        }

        public void IncrementCustomFlag(string key, int amount = 1)
        {
            if (!customFlags.ContainsKey(key))
                customFlags[key] = 0;
            customFlags[key] += amount;
        }

        #endregion
    }
}
