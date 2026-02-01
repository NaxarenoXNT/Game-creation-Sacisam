using System.Collections.Generic;
using Interfaces;
using Habilidades;

namespace Managers
{
    // =================================================================
    // =================== EVENTOS DE COMBATE ==========================
    // =================================================================
    
    public struct EventoCombateIniciado : IEvento
    {
        public List<IEntidadCombate> Jugadores;
        public List<IEntidadCombate> Enemigos;
    }
    
    public struct EventoCombateFinalizado : IEvento
    {
        public bool Victoria;
        public int XPGanada;
        public int OroGanado;
    }
    
    public struct EventoTurnoIniciado : IEvento
    {
        public IEntidadCombate Entidad;
        public int NumeroTurno;
        public bool EsJugador;
    }
    
    public struct EventoTurnoFinalizado : IEvento
    {
        public IEntidadCombate Entidad;
    }
    
    // =================== EVENTOS DE UI DE COMBATE ====================
    
    /// <summary>
    /// Cuando el jugador necesita elegir una acción para su personaje.
    /// </summary>
    public struct EventoEsperandoAccionJugador : IEvento
    {
        public EntityController Entidad;
        public List<IEntidadCombate> Aliados;
        public List<IEntidadCombate> Enemigos;
    }
    
    /// <summary>
    /// Cuando el jugador selecciona una acción del menú.
    /// </summary>
    public struct EventoAccionSeleccionada : IEvento
    {
        public EntityController Entidad;
        public CombatActionType TipoAccion;
        public HabilidadData Habilidad;  // Solo si es Atacar
    }
    
    /// <summary>
    /// Cuando el jugador selecciona un objetivo para su acción.
    /// </summary>
    public struct EventoObjetivoSeleccionado : IEvento
    {
        public EntityController Atacante;
        public IEntidadCombate Objetivo;
        public HabilidadData Habilidad;
    }
    
    /// <summary>
    /// Cuando el jugador cancela la selección de acción.
    /// </summary>
    public struct EventoAccionCancelada : IEvento
    {
        public EntityController Entidad;
    }
    
    /// <summary>
    /// Tipos de acciones disponibles en combate.
    /// </summary>
    public enum CombatActionType
    {
        Atacar,         // Seleccionar habilidad → objetivo
        UsarItem,       // Abrir inventario (futuro)
        Defender,       // Aumentar defensa este turno
        CederTurno,     // Saltar turno
        Huir            // Intentar escapar (futuro)
    }
    
    public struct EventoHabilidadUsada : IEvento
    {
        public IEntidadCombate Invocador;
        public IEntidadCombate Objetivo;
        public HabilidadData Habilidad;
    }
    
    public struct EventoHabilidadDesbloqueada : IEvento
    {
        public IEntidadCombate Entidad;
        public HabilidadData Habilidad;
    }
    
    public struct EventoHabilidadRemovida : IEvento
    {
        public IEntidadCombate Entidad;
        public HabilidadData Habilidad;
    }
    
    public struct EventoPasivaDesbloqueada : IEvento
    {
        public IEntidadCombate Entidad;
        public PasivaData Pasiva;
    }
    
    public struct EventoPasivaRemovida : IEvento
    {
        public IEntidadCombate Entidad;
        public PasivaData Pasiva;
    }
}
