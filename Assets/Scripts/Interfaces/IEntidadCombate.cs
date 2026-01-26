using Flags;
using Habilidades;
using System.Collections.Generic;

namespace Interfaces
{
    // =================================================================
    // =================== INTERFACES GRANULARES =======================
    // =================================================================
    
    /// <summary>
    /// Interfaz para entidades que pueden recibir dano.
    /// </summary>
    public interface IDamageable
    {
        void RecibirDano(int danoBruto, ElementAttribute tipo);
    }
    
    /// <summary>
    /// Interfaz para entidades que pueden ser curadas.
    /// </summary>
    public interface IHealable
    {
        int Curar(int cantidad);
    }
    
    /// <summary>
    /// Interfaz para entidades que pueden recibir estados alterados.
    /// </summary>
    public interface IStatusReceiver
    {
        void AplicarEstado(StatusFlag status, int duracion, int danoPorTurno = 0, float modificador = 0f);
        bool TieneEstado(StatusFlag status);
        void RemoverEstado(StatusFlag status);
    }
    
    /// <summary>
    /// Interfaz para entidades identificables en combate.
    /// </summary>
    public interface IIdentificable
    {
        string Nombre_Entidad { get; }
        int Nivel_Entidad { get; }
        TipoEntidades TipoEntidad { get; }
        bool EsTipoEntidad(TipoEntidades tipo);
    }
    
    // =================================================================
    // =================== INTERFAZ PRINCIPAL ==========================
    // =================================================================
    
    /// <summary>
    /// Interfaz completa para entidades de combate.
    /// Hereda de interfaces granulares para mayor flexibilidad.
    /// </summary>
    public interface IEntidadCombate : IDamageable, IHealable, IStatusReceiver, IIdentificable
    {
        // === Propiedades de vida ===
        int Vida_Entidad { get; }
        int VidaActual_Entidad { get; }
        
        // === Propiedades de combate ===
        int PuntosDeAtaque_Entidad { get; }
        float PuntosDeDefensa_Entidad { get; }
        int Velocidad { get; }
        bool EsDerrotado { get; }
        bool EstaMuerto { get; }

        // === Metodos de estado ===
        bool EstaVivo();
        bool PuedeActuar();
        
        // === Metodos de tipo/estilo ===
        bool UsaEstiloDeCombate(CombatStyle estilo);
        
        // === Combate ===
        int CalcularDanoContra(IEntidadCombate objetivo);
    }
    
    /// <summary>
    /// Interfaz para entidades que pueden seleccionar acciones (jugadores con UI, enemigos con IA).
    /// </summary>
    public interface IEntidadActuable
    {
        (IHabilidadesCommand comando, IEntidadCombate objetivo) ObtenerAccionElegida(
            List<IEntidadCombate> aliados, 
            List<IEntidadCombate> enemigos
        );
    }
    
    /// <summary>
    /// Interfaz para entidades que gestionan cooldowns de habilidades.
    /// </summary>
    public interface IGestorHabilidades
    {
        GestorCooldowns Cooldowns { get; }
        List<HabilidadData> HabilidadesDisponibles { get; }
        bool PuedeUsarHabilidad(HabilidadData habilidad);
        void IniciarCooldown(HabilidadData habilidad);
        void ProcesarInicioTurno();
    }
    
    /// <summary>
    /// Interfaz para entidades que pueden morir y ser derrotadas.
    /// </summary>
    public interface IMortal
    {
        bool EsDerrotado { get; }
        bool EstaMuerto { get; }
        event System.Action OnMuerte;
    }
}