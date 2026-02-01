using System.Collections.Generic;
using Interfaces;
using UnityEngine;

namespace Managers
{
    // =================================================================
    // ================ EVENTOS DE ENCOUNTER/DETECCIÓN =================
    // =================================================================
    
    /// <summary>
    /// Candidato detectado en el rango de interés del jugador.
    /// </summary>
    public struct EventoCandidatoDetectado : IEvento
    {
        public ICombatCandidate Candidato;
        public bool EnRangoEngagement;
    }
    
    /// <summary>
    /// Candidato salió del rango de detección.
    /// </summary>
    public struct EventoCandidatoFueraDeRango : IEvento
    {
        public ICombatCandidate Candidato;
    }
    
    /// <summary>
    /// Candidato entró en rango de combate (puede iniciar encuentro).
    /// </summary>
    public struct EventoCandidatoEnRangoCombate : IEvento
    {
        public ICombatCandidate Candidato;
    }
    
    /// <summary>
    /// Candidato salió del rango de combate.
    /// </summary>
    public struct EventoCandidatoSalioRangoCombate : IEvento
    {
        public ICombatCandidate Candidato;
    }
    
    /// <summary>
    /// Un encuentro de combate fue iniciado por el EncounterManager.
    /// </summary>
    public struct EventoEncounterIniciado : IEvento
    {
        public List<EntityController> Party;
        public List<EnemyController> Enemigos;
    }
    
    /// <summary>
    /// Nuevos enemigos se agregaron a un combate en progreso.
    /// </summary>
    public struct EventoEnemigosAgregados : IEvento
    {
        public List<EnemyController> NuevosEnemigos;
    }
}
