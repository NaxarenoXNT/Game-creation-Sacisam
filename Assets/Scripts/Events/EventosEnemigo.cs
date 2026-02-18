using Interfaces;
using Flags;
using UnityEngine;

namespace Managers
{
    // =================================================================
    // ================= EVENTOS DE ENEMIGOS ===========================
    // =================================================================
    
    /// <summary>
    /// Evento publicado cuando un enemigo es derrotado.
    /// IMPORTANTE: Usa datos copiados, NO referencias a controllers (pueden estar reciclados).
    /// </summary>
    public struct EventoEnemigoDerrotado : IEvento
    {
        /// <summary>ID único de la instancia del enemigo.</summary>
        public string IDInstanciaEnemigo;
        
        /// <summary>Tipo de entidad del enemigo derrotado.</summary>
        public TipoEntidades TipoEnemigo;
        
        /// <summary>Nombre del enemigo derrotado.</summary>
        public string NombreEnemigo;
        
        /// <summary>Nivel del enemigo al momento de morir.</summary>
        public int NivelEnemigo;
        
        /// <summary>XP otorgada al jugador.</summary>
        public float XPOtorgada;
        
        /// <summary>Posición donde murió el enemigo.</summary>
        public Vector3 PosicionMuerte;
        
        /// <summary>Referencia al asesino (si aplica).</summary>
        public IEntidadCombate Asesino;
        
        /// <summary>Timestamp de cuando murió.</summary>
        public float Timestamp;
    }
    
    /// <summary>
    /// Evento publicado cuando un enemigo es spawneado en el mundo.
    /// </summary>
    public struct EventoEnemigoSpawneado : IEvento
    {
        public string IDInstanciaEnemigo;
        public TipoEntidades TipoEnemigo;
        public Vector3 Posicion;
    }
}
