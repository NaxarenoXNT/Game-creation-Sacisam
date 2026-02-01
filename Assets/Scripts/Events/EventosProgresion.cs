using Interfaces;

namespace Managers
{
    // =================================================================
    // =================== EVENTOS DE PROGRESION =======================
    // =================================================================
    
    public struct EventoNivelSubido : IEvento
    {
        public IEntidadCombate Entidad;
        public int NuevoNivel;
    }
    
    public struct EventoXPGanada : IEvento
    {
        public IEntidadCombate Entidad;
        public float Cantidad;
        public float Total;
        public float Necesaria;
    }
}
