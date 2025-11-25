using System;


namespace Interfaces
{
    public interface IJugadorProgresion
    {
        int Nivel_Entidad { get; }
        float Experiencia_Actual { get; }
        float Experiencia_Progreso { get; }
        int Mana_jugador { get; }
        int ManaActual_jugador { get; }
        
        void RecibirXP(float xp);
        
        event Action<int> OnNivelSubido;
        event Action<float, float> OnXPGanada;
        event Action<int, int> OnManaCambiado;
    }
}