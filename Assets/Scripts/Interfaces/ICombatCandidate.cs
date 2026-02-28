using UnityEngine;

namespace Interfaces
{
    /// <summary>
    /// Contexto pasado a los candidatos para evaluar si pueden entrar en combate.
    /// Contiene información relevante del estado actual del juego.
    /// </summary>
    public class CombatContext
    {
        /// <summary>Posición del jugador/party en el mundo.</summary>
        public Vector3 PlayerPosition { get; set; }
        
        /// <summary>Nivel promedio del party.</summary>
        public int PartyAverageLevel { get; set; }
        
        /// <summary>Cantidad de aliados vivos en el party.</summary>
        public int PartyAliveCount { get; set; }
        
        /// <summary>Cantidad de enemigos ya en el encuentro actual.</summary>
        public int CurrentEnemyCount { get; set; }
        
        /// <summary>ID del bioma actual (opcional).</summary>
        public string CurrentBiome { get; set; }
        
        /// <summary>Hora del día en el juego (0-24, opcional).</summary>
        public float TimeOfDay { get; set; }
        
        /// <summary>Nivel de amenaza/aggro acumulado (opcional).</summary>
        public float ThreatLevel { get; set; }
        
        /// <summary>Si el combate ya está en progreso.</summary>
        public bool CombatInProgress { get; set; }
    }
    
    /// <summary>
    /// Interfaz que deben implementar las entidades que pueden entrar en combate.
    /// Permite al sistema de encuentros consultar si un enemigo debe unirse.
    /// </summary>
    public interface ICombatCandidate
    {
        /// <summary>
        /// Identificador único de este candidato.
        /// </summary>
        string CandidateId { get; }
        
        /// <summary>
        /// Transform del candidato para cálculos de distancia.
        /// </summary>
        Transform CandidateTransform { get; }
        
        /// <summary>
        /// Prioridad para entrar en combate (mayor = más probable que entre primero).
        /// Útil para limitar cantidad de enemigos.
        /// </summary>
        float CombatPriority { get; }
        
        /// <summary>
        /// Evalúa si este candidato puede unirse al combate dado el contexto actual.
        /// </summary>
        /// <param name="context">Contexto con información del estado del juego.</param>
        /// <returns>True si cumple todas las condiciones para entrar en combate.</returns>
        bool CanJoinCombat(CombatContext context);
        
        /// <summary>
        /// Llamado cuando el candidato es seleccionado para entrar en combate.
        /// </summary>
        void OnSelectedForCombat();
        
        /// <summary>
        /// Llamado cuando el candidato es removido del combate (muerte, huida, fin).
        /// </summary>
        void OnRemovedFromCombat();
    }
}
