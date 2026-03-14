using System;
using System.Collections.Generic;

namespace Missions
{
    /// <summary>
    /// Datos serializables para guardar/cargar el estado completo del sistema de misiones.
    /// Incluye datos globales y per-personaje.
    /// </summary>
    [Serializable]
    public class MissionSaveData
    {
        // ========== Globales ==========
        public List<string> globalesCompletadas = new List<string>();
        public List<string> globalesFallidas = new List<string>();
        public List<MissionActiveSaveData> globalesActivas = new List<MissionActiveSaveData>();
        public List<MissionExclusiveAssignment> exclusivasAsignadas = new List<MissionExclusiveAssignment>();

        // ========== Per-Personaje ==========
        public List<CharacterMissionSaveData> datosPersonajes = new List<CharacterMissionSaveData>();
    }

    /// <summary>
    /// Datos de misión de un personaje individual.
    /// </summary>
    [Serializable]
    public class CharacterMissionSaveData
    {
        public string characterId;
        public List<string> completadas = new List<string>();
        public List<string> fallidas = new List<string>();
        public List<MissionActiveSaveData> activas = new List<MissionActiveSaveData>();
    }

    /// <summary>
    /// Estado serializable de una misión activa.
    /// </summary>
    [Serializable]
    public class MissionActiveSaveData
    {
        public string misionId;
        public float tiempoAceptada;
        // TODO: Serializar progreso de objetivos cuando se implemente detalle
    }

    /// <summary>
    /// Asignación de misión exclusiva a un personaje.
    /// </summary>
    [Serializable]
    public class MissionExclusiveAssignment
    {
        public string misionId;
        public string characterId;
    }
}
