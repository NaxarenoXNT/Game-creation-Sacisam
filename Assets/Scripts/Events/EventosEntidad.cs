using Interfaces;
using Flags;

namespace Managers
{
    // =================================================================
    // =================== EVENTOS DE ENTIDAD ==========================
    // =================================================================
    
    public struct EventoDanoRecibido : IEvento
    {
        public IEntidadCombate Entidad;
        public int Cantidad;
        public ElementAttribute TipoDano;
        public IEntidadCombate Atacante;
    }
    
    public struct EventoCuracion : IEvento
    {
        public IEntidadCombate Entidad;
        public int Cantidad;
    }
    
    public struct EventoMuerte : IEvento
    {
        public IEntidadCombate Entidad;
        public IEntidadCombate Asesino;
    }
    
    public struct EventoEstadoAplicado : IEvento
    {
        public IEntidadCombate Entidad;
        public StatusFlag Estado;
        public int Duracion;
    }
    
    public struct EventoEstadoRemovido : IEvento
    {
        public IEntidadCombate Entidad;
        public StatusFlag Estado;
    }
}
