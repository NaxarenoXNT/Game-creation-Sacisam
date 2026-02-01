using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    // =================================================================
    // ================ EVENTOS DE PARTY / PERSONAJES ==================
    // =================================================================
    
    /// <summary>
    /// Un personaje fue registrado como propiedad del jugador.
    /// </summary>
    public struct EventoPersonajeRegistrado : IEvento
    {
        public EntityController Personaje;
    }
    
    /// <summary>
    /// El personaje principal (controlado) cambió.
    /// </summary>
    public struct EventoMainCambiado : IEvento
    {
        public EntityController MainAnterior;
        public EntityController NuevoMain;
    }
    
    /// <summary>
    /// Un personaje se unió al party activo.
    /// </summary>
    public struct EventoPersonajeUnidoParty : IEvento
    {
        public EntityController Personaje;
        public int TamanoPartyActual;
    }
    
    /// <summary>
    /// Un personaje salió del party activo.
    /// </summary>
    public struct EventoPersonajeSalioParty : IEvento
    {
        public EntityController Personaje;
        public bool FueEstacionado;
    }
    
    /// <summary>
    /// Un personaje fue estacionado en una ubicación.
    /// </summary>
    public struct EventoPersonajeEstacionado : IEvento
    {
        public EntityController Personaje;
        public Vector3 Ubicacion;
        public string NombreUbicacion;
    }
    
    // =================================================================
    // ================ EVENTOS DE REFUERZOS ===========================
    // =================================================================
    
    /// <summary>
    /// Se solicitaron refuerzos durante un combate.
    /// </summary>
    public struct EventoRefuerzosSolicitados : IEvento
    {
        public List<EntityController> RefuerzosDisponibles;
        public List<EntityController> Refuerzos;
        public int CantidadSolicitada;
        public Vector3 PosicionCombate;
    }
    
    /// <summary>
    /// Un refuerzo fue programado para llegar.
    /// </summary>
    public struct EventoRefuerzoProgramado : IEvento
    {
        public EntityController Refuerzo;
        public EntityController Personaje;
        public int TurnoLlegada;
        public int TurnosRestantes;
        public float Distancia;
    }
    
    /// <summary>
    /// Un refuerzo llegó al combate.
    /// </summary>
    public struct EventoRefuerzoLlegado : IEvento
    {
        public EntityController Refuerzo;
        public EntityController Personaje;
        public int TurnoLlegada;
    }
    
    /// <summary>
    /// Los refuerzos fueron cancelados (combate terminó antes de su llegada).
    /// </summary>
    public struct EventoRefuerzosCancelados : IEvento
    {
        public List<EntityController> RefuerzosCancelados;
    }
}
