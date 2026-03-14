using System;
using System.Collections.Generic;

namespace Evolution
{
    /// <summary>
    /// Estado global del jugador que trasciende personajes individuales.
    /// Almacena datos compartidos: misiones globales, traits bloqueados globalmente,
    /// asignación de misiones exclusivas, flags globales.
    /// Persiste junto con los EvolutionState de cada personaje.
    /// </summary>
    [Serializable]
    public class GlobalPlayerState
    {
        // ========== Misiones Globales ==========
        /// <summary>IDs de misiones globales completadas por cualquier personaje.</summary>
        public HashSet<string> misionesGlobalesCompletadas = new();

        /// <summary>IDs de misiones globales fallidas.</summary>
        public HashSet<string> misionesGlobalesFallidas = new();

        // ========== Traits Globalmente Bloqueados ==========
        /// <summary>
        /// Traits que, una vez obtenidos por cualquier personaje, no pueden ser
        /// obtenidos por otro. Ej: matar un boss único desbloquea un trait irrepetible.
        /// </summary>
        public HashSet<string> traitsGlobalmenteBloqueados = new();

        /// <summary>
        /// Registro de qué personaje obtuvo cada trait global.
        /// traitId → characterId del personaje que lo obtuvo primero.
        /// </summary>
        public Dictionary<string, string> traitGlobalObtenidoPor = new();

        // ========== Misiones Exclusivas ==========
        /// <summary>
        /// Misiones exclusivas asignadas: misionId → characterId del personaje que la posee.
        /// Una vez asignada, ningún otro personaje puede aceptarla.
        /// </summary>
        public Dictionary<string, string> misionesExclusivasAsignadas = new();

        // ========== Flags Globales ==========
        /// <summary>
        /// Flags globales del jugador (estados de mundo, facciones, progresión narrativa).
        /// Separados de los customFlags per-personaje en EvolutionState.
        /// </summary>
        public Dictionary<string, int> flagsGlobales = new();

        #region Métodos de Registro

        public void RegistrarMisionGlobalCompletada(string misionId)
        {
            if (!string.IsNullOrEmpty(misionId))
                misionesGlobalesCompletadas.Add(misionId);
        }

        public void RegistrarMisionGlobalFallida(string misionId)
        {
            if (!string.IsNullOrEmpty(misionId))
                misionesGlobalesFallidas.Add(misionId);
        }

        public bool EsMisionGlobalCompletada(string misionId)
        {
            return !string.IsNullOrEmpty(misionId) && misionesGlobalesCompletadas.Contains(misionId);
        }

        /// <summary>
        /// Bloquea un trait globalmente. Retorna false si ya estaba bloqueado.
        /// </summary>
        public bool BloquearTraitGlobal(string traitId, string characterId)
        {
            if (string.IsNullOrEmpty(traitId)) return false;
            if (traitsGlobalmenteBloqueados.Contains(traitId)) return false;

            traitsGlobalmenteBloqueados.Add(traitId);
            traitGlobalObtenidoPor[traitId] = characterId;
            return true;
        }

        public bool EsTraitGlobalmenteBloqueado(string traitId)
        {
            return !string.IsNullOrEmpty(traitId) && traitsGlobalmenteBloqueados.Contains(traitId);
        }

        /// <summary>
        /// Asigna una misión exclusiva a un personaje. Retorna false si ya está asignada.
        /// </summary>
        public bool AsignarMisionExclusiva(string misionId, string characterId)
        {
            if (string.IsNullOrEmpty(misionId) || string.IsNullOrEmpty(characterId)) return false;
            if (misionesExclusivasAsignadas.ContainsKey(misionId)) return false;

            misionesExclusivasAsignadas[misionId] = characterId;
            return true;
        }

        /// <summary>
        /// Obtiene el characterId dueño de una misión exclusiva, o null si no está asignada.
        /// </summary>
        public string GetDueñoMisionExclusiva(string misionId)
        {
            if (string.IsNullOrEmpty(misionId)) return null;
            return misionesExclusivasAsignadas.TryGetValue(misionId, out var charId) ? charId : null;
        }

        public void SetFlagGlobal(string key, int value)
        {
            if (!string.IsNullOrEmpty(key))
                flagsGlobales[key] = value;
        }

        public int GetFlagGlobal(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;
            return flagsGlobales.TryGetValue(key, out int val) ? val : 0;
        }

        #endregion
    }
}
