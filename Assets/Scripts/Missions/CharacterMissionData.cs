using System;
using System.Collections.Generic;
using Missions.Objectives;

namespace Missions
{
    /// <summary>
    /// Datos de misiones específicos de un personaje.
    /// Cada personaje tiene su propio CharacterMissionData que rastrea
    /// sus misiones personales y exclusivas asignadas.
    /// El MissionManager es el dueño de estas instancias.
    /// </summary>
    [Serializable]
    public class CharacterMissionData
    {
        /// <summary>ID del personaje al que pertenecen estos datos.</summary>
        public string characterId;

        /// <summary>Misiones activas de este personaje (personales + exclusivas asignadas).</summary>
        public Dictionary<string, MissionInstance> misionesActivas = new();

        /// <summary>IDs de misiones completadas por este personaje.</summary>
        public HashSet<string> misionesCompletadas = new();

        /// <summary>IDs de misiones fallidas por este personaje.</summary>
        public HashSet<string> misionesFallidas = new();

        /// <summary>IDs de misiones disponibles para este personaje (aún no aceptadas).</summary>
        public HashSet<string> misionesDisponibles = new();

        public CharacterMissionData(string characterId)
        {
            this.characterId = characterId;
        }

        public bool TieneMisionActiva(string misionId)
        {
            return misionesActivas.ContainsKey(misionId);
        }

        public bool CompletoMision(string misionId)
        {
            return misionesCompletadas.Contains(misionId);
        }
    }
}
